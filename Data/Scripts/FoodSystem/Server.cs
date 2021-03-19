using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Contracts;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.Utils;

namespace DailyNeeds
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Server : MySessionComponentBase
	{
        private ConfigData Config { get; set; }

		private int food_logic_skip = 0;
		// internal counter, init at 0
		private const int FOOD_LOGIC_SKIP_TICKS = 60 * 5;
		// Updating every 5s
		private static float Config.MaxValue = 100f;
		private static float MIN_VALUE = -100f;
		// if less than zero, a severely starved character will have to consume more
		private static float HUNGRY_WHEN = 0.3f;
		// if need is this much of maxval, consume
		private static float THIRSTY_WHEN = 0.3f;
		// if need is this much of maxval, consume
		private static float THIRST_PER_DAY = 300f;
		//600f;
		private static float HUNGER_PER_DAY = 100f;
		//300f;
		private static float DAMAGE_SPEED_HUNGER = -0.2f;
		// 2; // if negative, scale to minvalue for damage. if positive, do this much damage every tick.
		private static float DAMAGE_SPEED_THIRST = -0.6f;
		//5; // if negative, scale to use minvalue for damage.  if positive, do this much damage every tick.
		private static float DEFAULT_MODIFIER = 1f;
		private static float FLYING_MODIFIER = 1f;
		private static float RUNNING_MODIFIER = 1.5f;
		private static float SPRINTING_MODIFIER = 2f;
		private static float NO_MODIFIER = 1f;

		private static float CRAP_AMOUNT = 0.90f;
		// if zero, skip creating waste, otherwise, make GreyWater and Organic right after eating, and don't go into details
		private static float CROSS_CRAP_AMOUNT = 0.0f;
		// does eating/drinking generate any amount of the "other" waste? formula is (1-crapamount)*this
		private static float DEATH_RECOVERY = 2.00f;
		// if true, "evacuate" before dying, based on current hunger and thirst level. This number is how much is evacuated if player is at 100%

		private static bool FATIGUE_ENABLED = true;
		private static float FATIGUE_SITTING = 0.1f;
		private static float FATIGUE_CROUCHING = 0.075f;
		private static float FATIGUE_STANDING = 0.05f;
		private static float FATIGUE_FLYING = -0.05f;
		private static float FATIGUE_WALKING = -0.1f;
		private static float FATIGUE_RUNNING = -0.1f;
		private static float FATIGUE_SPRINTING = -0.1f;
		private static float EXTRA_THIRST_FROM_FATIGUE = -0.1f;
				
		private static float FATIGUE_LEVEL_NOHEALING = 0.01f; // at this fraction of MIN_VALUE, prevent autoheal
		private static float FATIGUE_LEVEL_FORCEWALK = 0.25f; // at this fraction of MIN_VALUE, try to force walking
		private static float FATIGUE_LEVEL_FORCECROUCH = 0.5f; // at this fraction of MIN_VALUE, try to force walking
		private static float FATIGUE_LEVEL_HELMET = 0.75f; // at this fraction of MIN_VALUE, toggle helmet
		private static float FATIGUE_LEVEL_HEARTATTACK = 0.999f; // at this fraction of MIN_VALUE, heart attack
		
		
		// = -0.6f; //5; // if negative, scale to use minvalue for damage.  if positive, do this much damage every tick.
		private static float FOOD_BONUS = 1.25f; // multipliers
		private static float DRINK_BONUS = 1.25f;
		private static float REST_BONUS = 1.25f;
		
		private static float STARTING_HUNGER = 100f;
		private static float STARTING_THIRST = 100f;
		private static float STARTING_FATIGUE = 100f;
		
		private float mHungerPerMinute;
		private float mThirstPerMinute;
		private bool IsAutohealingOn = false;
		private float dayLen = 120f;
		private bool config_get = false;

		//private static Config mConfig = Config.Load("hatm.cfg");
		private static PlayerDataStore _playerDataStorage = new PlayerDataStore();
		private static ConfigDataStore mConfigDataStore = new ConfigDataStore();
		private static List<IMyPlayer> _players = new List<IMyPlayer>();
		private static Dictionary<string, float> mFoodTypes = new Dictionary<string, float>();
		private static Dictionary<string, float> mBeverageTypes = new Dictionary<string, float>();
		private const string OBJECT_BUILDER_PREFIX = "ObjectBuilder_";
		private static bool mStarted = false;

		private MyGameTimer mTimer;

		/*public static void RegisterFood(string szItemName, float hungerValue)
        {
            mFoodTypes.Add(szItemName, hungerValue);
        }

        public static void RegisterBeverage(string szItemName, float thirstValue)
        {
            mBeverageTypes.Add(szItemName, thirstValue);
        }
        
		 */

		private static bool playerEatSomething(IMyEntity entity, PlayerData playerData, float maxval_cap, float crapbonus)
		{
			MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
			var items = inventory.GetItems();

			foreach (IMyInventoryItem item in items) {
				float result;

				// Getting the item type

				string szItemContent = item.Content.ToString();
				string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX) + OBJECT_BUILDER_PREFIX.Length);

				// Type verification

				if (!szTypeName.Equals("Ingot"))
					continue;
				
				if (mFoodTypes.TryGetValue(item.Content.SubtypeName, out result)) {
					float canConsumeNum = 0f;
					
					// if a food is registered as negative, reduce the maximum value. Useful for low nutrition meals.
					if (result < 0)
					{
						result = Math.Abs(result);
						canConsumeNum = Math.Min((((maxval_cap/2f) - playerData.hunger) / result), (float)item.Amount);
					}
					else
						canConsumeNum = Math.Min(((maxval_cap - playerData.hunger) / result), (float)item.Amount);

					//MyAPIGateway.Utilities.ShowMessage("DEBUG", "canEat: " + canConsumeNum);

					if (canConsumeNum > 0) {
						inventory.Remove(item, (MyFixedPoint)canConsumeNum);
						playerData.hunger += result * (float)canConsumeNum;
						if (item.Content.SubtypeName.Contains("ouillon")) // TODO parametrize this
							playerData.thirst += Math.Max(0f,Math.Min(result * (float)canConsumeNum, maxval_cap-playerData.thirst)); // TODO parametrize this
						// waste management line
						if (CRAP_AMOUNT > 0.0) {
							inventory.AddItems((MyFixedPoint)(canConsumeNum * CRAP_AMOUNT * crapbonus), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
							if (CROSS_CRAP_AMOUNT > 0.0)
								inventory.AddItems((MyFixedPoint)(canConsumeNum * (1 - CRAP_AMOUNT) * CROSS_CRAP_AMOUNT), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater" });
						}
						return true;
					}
				}
			}

			return false;
		}

		private static bool playerDrinkSomething(IMyEntity entity, PlayerData playerData, float maxval_cap, float crapbonus)
		{
			MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
			var items = inventory.GetItems();

			foreach (IMyInventoryItem item in items) {
				float result;

				// Getting the item type

				string szItemContent = item.Content.ToString();

				//MyAPIGateway.Utilities.ShowMessage("DEBUG", "szItemContent: " + item.Content.SubtypeName);

				string szTypeName = szItemContent.Substring(szItemContent.IndexOf(OBJECT_BUILDER_PREFIX) + OBJECT_BUILDER_PREFIX.Length);

				// Type verification

				if (!szTypeName.Equals("Ingot"))
					continue;
				
				if (mBeverageTypes.TryGetValue(item.Content.SubtypeName, out result)) {
					float canConsumeNum = Math.Min(((maxval_cap - playerData.thirst) / result), (float)item.Amount);

					//MyAPIGateway.Utilities.ShowMessage("DEBUG", "canDrink: " + canConsumeNum);

					if (canConsumeNum > 0) {
						inventory.Remove(item, (MyFixedPoint)canConsumeNum);
						playerData.thirst += result * (float)canConsumeNum;
						if (item.Content.SubtypeName.Contains("offee")) // TODO parametrize this
							playerData.fatigue = Config.MaxValue; // TODO parametrize this
						if (item.Content.SubtypeName.Contains("ouillon")) // TODO parametrize this
							playerData.hunger += Math.Max(0f,Math.Min(result * (float)canConsumeNum, maxval_cap-playerData.hunger)); // TODO parametrize this
						
						// waste management line
						if (CRAP_AMOUNT > 0.0) {
							inventory.AddItems((MyFixedPoint)(canConsumeNum * CRAP_AMOUNT * crapbonus), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater" });
							if (CROSS_CRAP_AMOUNT > 0.0)
								inventory.AddItems((MyFixedPoint)(canConsumeNum * (1 - CRAP_AMOUNT) * CROSS_CRAP_AMOUNT), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
						}
						return true;
					}
				}
			}

			return false;
		}

		private void init()
		{
			_playerDataStorage.Load();
			mConfigDataStore.Load();

            Config = mConfigDataStore.ConfigData;

            // Minimum of 2h
            dayLen = Math.Max(MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes, 120f);
            mHungerPerMinute = Config.HungerPerDay / dayLen;
            mThirstPerMinute = Config.ThirstPerDay / dayLen;

			mConfigDataStore.Save();


			if (Utils.isDev())
				MyAPIGateway.Utilities.ShowMessage("SERVER", "INIT");

			MyAPIGateway.Multiplayer.RegisterMessageHandler(1338, AdminCommandHandler);
			MyAPIGateway.Utilities.RegisterMessageHandler(1339, NeedsApiHandler);

			IsAutohealingOn = MyAPIGateway.Session.SessionSettings.AutoHealing;
            NeedsApi api = new NeedsApi();
			
			// Register all the known foods.
            foreach (var food in FoodDefaults.Defaults)
            {
                api.RegisterItem(food.Key, food.Value);
            }
			
			
			// this lets other people do stuff with this, just add the appropriate tag to the food name. Use whole numbers. For example, SpacePasta!fih40 or SpaceCola!di60 or SpaceTofu!fil20
            // TODO: Refactor this to handle tags more gracefully..
			for (int i=1;i<=100;i++)
			{
				api.RegisterEdibleItem("!fil"+(i),(float)(-i));
				api.RegisterEdibleItem("!fih"+(i),(float)(i));
				api.RegisterDrinkableItem("!di"+(i),(float)(i));
			}
			
			// Initiate the timer

			mTimer = new MyGameTimer();
		}

		// Update the player list

		private void updatePlayerList()
		{
			_players.Clear();
			MyAPIGateway.Players.GetPlayers(_players);
		}

		private IMyEntity GetCharacterEntity(IMyEntity entity)
		{
			if (entity is MyCockpit)
				return (entity as MyCockpit).Pilot as IMyEntity;

			if (entity is MyRemoteControl)
				return (entity as MyRemoteControl).Pilot as IMyEntity;

			//TODO: Add more pilotable entities
			return entity;
		}

		private void updateFoodLogic()
		{
			float elapsedMinutes = (float)(mTimer.Elapsed.Seconds / 60);
			//float CurPlayerHealth = -1f;
			bool ChangedStance = false;
			MyObjectBuilder_Character character;
			MyCharacterMovementEnum curmove = MyCharacterMovementEnum.Sitting;

            foreach (var player in _players) {
				
				
				if (String.IsNullOrWhiteSpace(player.Controller?.ControlledEntity?.Entity?.DisplayName)) {

					PlayerData playerData = _playerDataStorage.Retrieve(player);
				    Logging.Instance.WriteLine(playerData.ToString() + "Loaded to Server");

                    IMyEntity entity = player.Controller.ControlledEntity.Entity;
					entity = GetCharacterEntity(entity);
					//					//MyAPIGateway.Utilities.ShowMessage("DEBUG", "Character: " + entity.DisplayName); // gets me player name

					float currentModifier = 1f;
					float fatigueRate = 0f;
					bool dead = false;
					bool forceEating = false;
					float recycleBonus = 1f;
					bool fatigueBonus = false;
					bool hungerBonus = false;
					bool thirstBonus = false;
					
					// if we were under the effects of a bonus, keep it until we no longer are
                    fatigueBonus = playerData.fatigue > Config.MaxValue;
					thirstBonus = playerData.thirst > Config.MaxValue;
                    hungerBonus = playerData.hunger > Config.MaxValue;


                    if (entity is IMyCharacter) {
						character = entity.GetObjectBuilder() as MyObjectBuilder_Character;
						//MyAPIGateway.Utilities.ShowMessage("DEBUG", "State: " + character.MovementState);
						
						if (playerData.Entity == null || playerData.Entity.Closed || playerData.Entity.EntityId != entity.EntityId) {
							bool bReset = false;

							if (!playerData.loaded) {
								bReset = true;
								playerData.loaded = true;
							} else if ((playerData.Entity != null) && (playerData.Entity != entity))
								bReset = true;

							if (bReset) {
								playerData.hunger = Config.StartingHunger;
								playerData.thirst = Config.StartingThirst;
								playerData.fatigue = Config.StartingFatigue;
							}
							
							playerData.Entity = entity;
						}

						//MyAPIGateway.Utilities.ShowMessage("DEBUG", "State: " + character.MovementState + ":" + playerData.LastMovement);
						ChangedStance = playerData.LastMovement != character.MovementState;
						
						curmove = character.MovementState;

						switch (character.MovementState) { // this should be all of them....

							case MyCharacterMovementEnum.Sitting:
								IMyCubeBlock cb = player.Controller.ControlledEntity.Entity as IMyCubeBlock;
								//cb.DisplayNameText is name of individual block, cb.DefinitionDisplayNameText is name of block type
								currentModifier = Config.DefaultModifier;
                                fatigueRate = Config.FatigueSitting;
								
								// special case: we may be interacting with a bed, a lunchroom seat or treadmill, so let's
								String seatmodel = cb.DefinitionDisplayNameText.ToLower();
								String seatname = cb.DisplayNameText.ToLower();

                                if (seatmodel.Contains("cryo")) // you're in a cryopd not an oxygen bed
								{
									currentModifier = 0.0000125f;
									fatigueRate = 0.0000125f;
								} else if (seatmodel.Contains("treadmill")) {
									currentModifier = RUNNING_MODIFIER; // jog...
									fatigueRate = FATIGUE_RUNNING/2.5f; // but pace yourself
								} else if (seatmodel.Contains("bed") || seatmodel.Contains("bunk")) {
									currentModifier = DEFAULT_MODIFIER / 2f; // nap time! Needs are reduced.
									fatigueRate = FATIGUE_SITTING * 3f; //  nap time! Rest is greatly sped up.
									fatigueBonus |= !ChangedStance; // longer nap? OK, allow for extra resting
								} else if (seatmodel.Contains("toilet") && ChangedStance) {
									forceEating = true; // also forces defecation, so this makes sense. but use changedstance to do it only once.
									recycleBonus = 1.5f;
								} else if (seatname.Contains("noms")) {
									forceEating = true; // also forces crapping, fortunately the suit takes care of it. Eat continuously while sitting.
									hungerBonus |= playerData.hunger > Config.MaxValue * 0.99; // get to 100% first, then apply bonus.
									thirstBonus |= playerData.thirst > Config.MaxValue * 0.99; // get to 100% first, then apply bonus.
								}
								break;

							case MyCharacterMovementEnum.Flying:
								currentModifier = FLYING_MODIFIER;
								fatigueRate = FATIGUE_FLYING; // operating a jetpack is surprisingly hard
								break;

							case MyCharacterMovementEnum.Falling:
								currentModifier = FLYING_MODIFIER;
								fatigueRate = FATIGUE_WALKING; // change nothing for the first iteration (prevents jump exploit)
								if (!ChangedStance)
									fatigueRate = FATIGUE_STANDING; // freefall is actually relaxing when you are used to it. A professional space engineer would be.
								break;
								
							case MyCharacterMovementEnum.Crouching:
							case MyCharacterMovementEnum.CrouchRotatingLeft:
							case MyCharacterMovementEnum.CrouchRotatingRight:
								currentModifier = DEFAULT_MODIFIER;
								fatigueRate = FATIGUE_CROUCHING;
								break;

							case MyCharacterMovementEnum.Standing:
							case MyCharacterMovementEnum.RotatingLeft:
							case MyCharacterMovementEnum.RotatingRight:
								currentModifier = DEFAULT_MODIFIER;
								fatigueRate = FATIGUE_STANDING;
								break;

							case MyCharacterMovementEnum.CrouchWalking:
							case MyCharacterMovementEnum.CrouchBackWalking:
							case MyCharacterMovementEnum.CrouchStrafingLeft:
							case MyCharacterMovementEnum.CrouchStrafingRight:
							case MyCharacterMovementEnum.CrouchWalkingRightFront:
							case MyCharacterMovementEnum.CrouchWalkingRightBack:
							case MyCharacterMovementEnum.CrouchWalkingLeftFront:
							case MyCharacterMovementEnum.CrouchWalkingLeftBack:
								currentModifier = RUNNING_MODIFIER;
								fatigueRate = FATIGUE_RUNNING; // doing the duckwalk is more tiring than walking: try it if you don't believe me
								break;
								
								
								
							case MyCharacterMovementEnum.Walking:
							case MyCharacterMovementEnum.BackWalking:
							case MyCharacterMovementEnum.WalkStrafingLeft:
							case MyCharacterMovementEnum.WalkStrafingRight:
							case MyCharacterMovementEnum.WalkingRightFront:
							case MyCharacterMovementEnum.WalkingRightBack:
							case MyCharacterMovementEnum.WalkingLeftFront:
							case MyCharacterMovementEnum.WalkingLeftBack:
								currentModifier = DEFAULT_MODIFIER;
								fatigueRate = FATIGUE_WALKING;
								break;

							case MyCharacterMovementEnum.LadderUp:
								currentModifier = RUNNING_MODIFIER;
								fatigueRate = FATIGUE_RUNNING;
								break;
							case MyCharacterMovementEnum.LadderDown:
								currentModifier = DEFAULT_MODIFIER;
								fatigueRate = FATIGUE_WALKING;
								break;

							case MyCharacterMovementEnum.Running:
							case MyCharacterMovementEnum.Backrunning:
							case MyCharacterMovementEnum.RunStrafingLeft:
							case MyCharacterMovementEnum.RunStrafingRight:
							case MyCharacterMovementEnum.RunningRightFront:
							case MyCharacterMovementEnum.RunningRightBack:
							case MyCharacterMovementEnum.RunningLeftBack:
							case MyCharacterMovementEnum.RunningLeftFront:
								currentModifier = RUNNING_MODIFIER;
								fatigueRate = FATIGUE_RUNNING;
								break;

							case MyCharacterMovementEnum.Sprinting:
							case MyCharacterMovementEnum.Jump:
								currentModifier = SPRINTING_MODIFIER;
								fatigueRate = FATIGUE_SPRINTING;
								break;

							case MyCharacterMovementEnum.Died:
								currentModifier = DEFAULT_MODIFIER; // unused, but let's have them
								fatigueRate = FATIGUE_STANDING; // unused, but let's have them
								dead = true; // for death recovery logic
								break;

						}
						playerData.LastMovement = character.MovementState; // track delta

					} else if (playerData.Entity != null || !playerData.Entity.Closed)
						entity = playerData.Entity;

					// Sanity checks
					if (hungerBonus) {
						if (playerData.hunger > Config.MaxValue*FOOD_BONUS)
							playerData.hunger = Config.MaxValue*FOOD_BONUS;
					} else {
						if (playerData.hunger > Config.MaxValue)
							playerData.hunger = Config.MaxValue;
					}
					
					if (thirstBonus) {
						if (playerData.thirst > Config.MaxValue*DRINK_BONUS)
							playerData.thirst = Config.MaxValue*DRINK_BONUS;
					} else {
						if (playerData.thirst > Config.MaxValue)
							playerData.thirst = Config.MaxValue;
					}

					// Cause needs
					if (FATIGUE_ENABLED) {
						playerData.fatigue += (fatigueRate * FOOD_LOGIC_SKIP_TICKS / 60 * 20);// / 15);
						playerData.fatigue = Math.Max(playerData.fatigue, MIN_VALUE);
						if (fatigueBonus)
							playerData.fatigue = Math.Min(playerData.fatigue, Config.MaxValue*REST_BONUS);
						else
							playerData.fatigue = Math.Min(playerData.fatigue, Config.MaxValue);
						
					} else
						playerData.fatigue = 9001f;
					
					if (playerData.fatigue <= 0) {
						
						// fatigue consequences
						// at 0, start causing extra thirst
						// at specified, force walk instead of run (unless overriding by sprinting)
						// at specified, force crouch, and do damage flashes
						// at specified, breathing reflex / mess with helmet, and do a bit of actual damage (just in case thirst isn't already causing it)
						// at specified, cause heart attack

						if (playerData.fatigue <= (0.00f * MIN_VALUE))
						{
							if (EXTRA_THIRST_FROM_FATIGUE > 0)
							{
								// positive: pile on to thirst, per second
								playerData.thirst -= (EXTRA_THIRST_FROM_FATIGUE * FOOD_LOGIC_SKIP_TICKS / 60);
							} else { // negative: multiply modifier
								currentModifier *= -EXTRA_THIRST_FROM_FATIGUE;
							}
						}

						if (playerData.fatigue <= (FATIGUE_LEVEL_FORCEWALK * MIN_VALUE))
						{ // force player to walk if they were running
							switch (curmove)
							{
								case MyCharacterMovementEnum.Sprinting:
								case MyCharacterMovementEnum.Running:
								case MyCharacterMovementEnum.Backrunning:
								case MyCharacterMovementEnum.RunStrafingLeft:
								case MyCharacterMovementEnum.RunStrafingRight:
								case MyCharacterMovementEnum.RunningRightFront:
								case MyCharacterMovementEnum.RunningRightBack:
								case MyCharacterMovementEnum.RunningLeftBack:
								case MyCharacterMovementEnum.RunningLeftFront:
									VRage.Game.ModAPI.Interfaces.IMyControllableEntity ce = player.Controller.ControlledEntity.Entity as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
									ce.SwitchWalk();
									break;
							}
						}

						if (playerData.fatigue <= (FATIGUE_LEVEL_FORCECROUCH * MIN_VALUE))
						{
							bool iscrouching=false;
							switch(curmove)
							{
								case MyCharacterMovementEnum.Crouching:
								case MyCharacterMovementEnum.CrouchWalking:
								case MyCharacterMovementEnum.CrouchBackWalking:
								case MyCharacterMovementEnum.CrouchStrafingLeft:
								case MyCharacterMovementEnum.CrouchStrafingRight:
								case MyCharacterMovementEnum.CrouchWalkingRightFront:
								case MyCharacterMovementEnum.CrouchWalkingRightBack:
								case MyCharacterMovementEnum.CrouchWalkingLeftFront:
								case MyCharacterMovementEnum.CrouchWalkingLeftBack:
									iscrouching=true;
									break;
							}
							if (!iscrouching)
							{
								VRage.Game.ModAPI.Interfaces.IMyControllableEntity ce = player.Controller.ControlledEntity.Entity as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
								ce.Crouch(); // force player to crouch
							}
						}
						
						if (playerData.fatigue <= (FATIGUE_LEVEL_HELMET * MIN_VALUE))
						{
							VRage.Game.ModAPI.Interfaces.IMyControllableEntity ce = player.Controller.ControlledEntity.Entity as VRage.Game.ModAPI.Interfaces.IMyControllableEntity;
							ce.SwitchHelmet(); // force player to switch helmet, panic reaction from trying to catch breath
							
							var destroyable = entity as IMyDestroyableObject;
							destroyable.DoDamage(0.001f, MyStringHash.GetOrCompute("Fatigue"), true); // starting to hurt
						}

						if (playerData.fatigue <= (FATIGUE_LEVEL_NOHEALING * MIN_VALUE))
						{
							var destroyable = entity as IMyDestroyableObject;
							destroyable.DoDamage(0.001f, MyStringHash.GetOrCompute("Fatigue"), true); // starting to hurt
							if (IsAutohealingOn) // fatigued? no autohealing, either.
							{
								const float HealthTick = 100f / 240f * FOOD_LOGIC_SKIP_TICKS / 60f;
								destroyable.DoDamage(HealthTick, MyStringHash.GetOrCompute("Testing"), false);
							}

						}

						if (playerData.fatigue <= (FATIGUE_LEVEL_HEARTATTACK * MIN_VALUE))
						{
							var destroyable = entity as IMyDestroyableObject;
							destroyable.DoDamage(1000f, MyStringHash.GetOrCompute("Fatigue"), true); // sudden, but very avoidable, heart attack ;)
						}
					}
					
					if (playerData.thirst > MIN_VALUE) {
						playerData.thirst -= elapsedMinutes * mThirstPerMinute * currentModifier;
						playerData.thirst = Math.Max(playerData.thirst, MIN_VALUE);
					}

					if (playerData.hunger > MIN_VALUE) {
						playerData.hunger -= elapsedMinutes * mHungerPerMinute * currentModifier;
						playerData.hunger = Math.Max(playerData.hunger, MIN_VALUE);
					}


					// Try to meet needs
					if (playerData.hunger < (Config.MaxValue * HUNGRY_WHEN) || forceEating)
						playerEatSomething(entity, playerData, hungerBonus?Config.MaxValue*1.25f:Config.MaxValue,recycleBonus);
					
					if (playerData.thirst < (Config.MaxValue * THIRSTY_WHEN) || forceEating)
						playerDrinkSomething(entity, playerData, thirstBonus?Config.MaxValue*1.25f:Config.MaxValue,recycleBonus);

					// Cause damage if needs are unmet
					if (playerData.thirst <= 0) {
						var destroyable = entity as IMyDestroyableObject;
						if (DAMAGE_SPEED_THIRST > 0)
							destroyable.DoDamage((IsAutohealingOn ? (DAMAGE_SPEED_THIRST + 1f) : DAMAGE_SPEED_THIRST), MyStringHash.GetOrCompute("Thirst"), true);
						else
							destroyable.DoDamage(((IsAutohealingOn ? (-DAMAGE_SPEED_THIRST + 1f) : -DAMAGE_SPEED_THIRST) + DAMAGE_SPEED_THIRST * playerData.thirst), MyStringHash.GetOrCompute("Thirst"), true);
					}

					if (playerData.hunger <= 0) {
						var destroyable = entity as IMyDestroyableObject;
						if (DAMAGE_SPEED_HUNGER > 0)
							destroyable.DoDamage((IsAutohealingOn ? (DAMAGE_SPEED_HUNGER + 1f) : DAMAGE_SPEED_HUNGER), MyStringHash.GetOrCompute("Hunger"), true);
						else
							destroyable.DoDamage(((IsAutohealingOn ? (-DAMAGE_SPEED_HUNGER + 1f) : -DAMAGE_SPEED_HUNGER) + DAMAGE_SPEED_HUNGER * playerData.hunger), MyStringHash.GetOrCompute("Hunger"), true);
					}



					/*


					character = entity.GetObjectBuilder(false) as MyObjectBuilder_Character;
					if (character.Health == null) // ok, so the variable exists, but it's always null for some reason?
						CurPlayerHealth = 101f;
					else
						CurPlayerHealth = (float) (character.Health);

					if (IsAutohealingOn && CurPlayerHealth < 70f)
					{
						const float HealthTick = 100f / 240f * FOOD_LOGIC_SKIP_TICKS / 60f;
						var destroyable = entity as IMyDestroyableObject;
						destroyable.DoDamage(HealthTick, MyStringHash.GetOrCompute("Testing"), false);
					}
					 */


					if (dead && DEATH_RECOVERY > 0.0) {
						MyInventoryBase inventory = ((MyEntity)entity).GetInventoryBase();
						if (playerData.hunger > 0)
							inventory.AddItems((MyFixedPoint)((1f / Config.MaxValue) * DEATH_RECOVERY * (playerData.hunger)), new MyObjectBuilder_Ore() { SubtypeName = "Organic" });
						if (playerData.thirst > 0)
							inventory.AddItems((MyFixedPoint)((1f / Config.MaxValue) * DEATH_RECOVERY * (playerData.thirst)), new MyObjectBuilder_Ingot() { SubtypeName = "GreyWater" });
					}

				    //Sends data from Server.cs to Client.cs
				    string message = MyAPIGateway.Utilities.SerializeToXML<PlayerData>(playerData);
                    Logging.Instance.WriteLine(("Message sent from Server.cs to Client.cs: " + message));
                    MyAPIGateway.Multiplayer.SendMessageTo(
						1337,
						Encoding.Unicode.GetBytes(message),
						player.SteamUserId
					);
                }
			}

			// Reinitialize the timer

			mTimer = new MyGameTimer();
		}

		public void AdminCommandHandler(byte[] data)
		{
			//Keen why do you not pass the steamId? :/
			Command command = MyAPIGateway.Utilities.SerializeFromXML<Command>(Encoding.Unicode.GetString(data));

			/*if (Utils.isAdmin(command.sender)) {
                var words = command.content.Trim().ToLower().Replace("/", "").Split(' ');
                if (words.Length > 0 && words[0] == "hatm") {
                    switch (words[1])
                    {
                        case "blacklist":
                            IMyPlayer player = _players.Find(p => words[2] == p.DisplayName);
                            mConfig.BlacklistAdd(player.SteamUserId);
                            break;
                    }
                }
            }*/
		}

		public void NeedsApiHandler(object data)
		{
			//mFoodTypes.Add(szItemName, hungerValue);
			//mBeverageTypes.Add(szItemName, thirstValue);

			NeedsApi.Event e = (NeedsApi.Event)data;

			if (e.type == NeedsApi.Event.Type.RegisterEdibleItem) {
				NeedsApi.RegisterEdibleItemEvent edibleItemEvent = (NeedsApi.RegisterEdibleItemEvent)e.payload;
				//MyAPIGateway.Utilities.ShowMessage("DEBUG", "EdibleItem " + edibleItemEvent.item + "(" +  edibleItemEvent.value + ") registered");
				mFoodTypes.Add(edibleItemEvent.item, edibleItemEvent.value);
			} else if (e.type == NeedsApi.Event.Type.RegisterDrinkableItem) {
				NeedsApi.RegisterDrinkableItemEvent drinkableItemEvent = (NeedsApi.RegisterDrinkableItemEvent)e.payload;
				//MyAPIGateway.Utilities.ShowMessage("DEBUG", "DrinkableItem " + drinkableItemEvent.item + "(" +  drinkableItemEvent.value + ") registered");
				mBeverageTypes.Add(drinkableItemEvent.item, drinkableItemEvent.value);
			}
		}

		public override void UpdateAfterSimulation()
		{
			if (MyAPIGateway.Session == null)
				return;

			// Food logic is desactivated in creative mode

			if (MyAPIGateway.Session.SessionSettings.GameMode == MyGameModeEnum.Creative)
				return;

            // Don't process food ticks when the game is paused.
            if (Sandbox.MySandboxGame.IsPaused)
                return;

            try {
				if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer) {
					if (!mStarted) {
						mStarted = true;
						init();

						food_logic_skip = FOOD_LOGIC_SKIP_TICKS;
					}

					if (++food_logic_skip >= FOOD_LOGIC_SKIP_TICKS) {
						food_logic_skip = 0;

						updatePlayerList();
						updateFoodLogic();
					}
				}
			}
			catch (Exception e)
			{
                //MyApiGateway.Utilities.ShowMessage("ERROR", "Logger error: " + e.Message + "\n" + e.StackTrace);
                
                Logging.Instance.WriteLine(("(FoodSystem) Server UpdateSimulation Error: " + e.Message + "\n" + e.StackTrace));
            }
		}

		// Saving datas when requested

		public override void SaveData()
		{
			_playerDataStorage.Save();
			mConfigDataStore.Save();
		}

		protected override void UnloadData()
		{
			mStarted = false;
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(1338, AdminCommandHandler);
			MyAPIGateway.Utilities.UnregisterMessageHandler(1339, NeedsApiHandler);
			_players.Clear();
			mFoodTypes.Clear();
			mBeverageTypes.Clear();
			_playerDataStorage.clear();
			mConfigDataStore.Clear();
		}
	}

    public class CustomContract : IMyContractHauling
    {
        public long StartBlockId { get; }
        public int MoneyReward { get; }
        public int Collateral { get; }
        public int Duration { get; }
        public Action<long> OnContractAcquired { get; set; }
        public Action OnContractSucceeded { get; set; }
        public Action OnContractFailed { get; set; }
        public long EndBlockId { get; }
    }
}