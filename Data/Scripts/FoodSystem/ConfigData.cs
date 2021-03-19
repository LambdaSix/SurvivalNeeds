namespace DailyNeeds {
    public class ConfigData
    {

        public ulong Steamid;
		public float MaxValue; // = 100f;
		public float MinValue; // = -100f; // if less than zero, a severely starved character will have to consume more
		public float HungryWhen; // = 0.3f; // if need is this much of maxval, consume
		public float ThirstyWhen; // = 0.3f; // if need is this much of maxval, consume
		public float ThirstPerDay; // = 300f; //600f;
		public float HungerPerDay; // = 100f; //300f;
		public float DamageSpeedHunger; // = -0.2f; // 2; // if negative, scale to minvalue for damage. if positive, do this much damage every tick.
		public float DamageSpeedThirst; // = -0.6f; //5; // if negative, scale to use minvalue for damage.  if positive, do this much damage every tick.
		public float DefaultModifier; // = 1f;
		public float FlyingModifier; // = 1f;
		public float RunningModifier; // = 1.5f;
		public float SprintingModifier; // = 2f;
		public float NoModifier; // = 1f;
		public float FecesAmount; // = 0.90f; // if zero, skip creating waste, otherwise, make GreyWater and Organic right after eating, and don't go into details
		public float UrineAmount; // = 0.0f; // does eating/drinking generate any amount of the "other" waste? formula is (1-crapamount)*this
		public float DeathRecovery; // = 2.00f; // if true, "evacuate" before dying, based on current hunger and thirst level. This number is how much is evacuated if player is at 100%
		public bool FatigueEnabled;
		public float FatigueSitting;
		public float FatigueCrouching;
		public float FatigueStanding;
		public float FatigueWalking;
		public float FatigueRunning;
		public float FatigueFlying;
		public float FatigueSprinting;
		public float ExtraThirstFromFatigue; // = -0.6f; //5; // if negative, scale to use minvalue for damage.  if positive, do this much damage every tick.
		public float FatigueLevelNohealing; // at this fraction of MIN_VALUE, prevent autoheal
		public float FatigueLevelForcewalk; // at this fraction of MIN_VALUE, try to force walking
		public float FatigueLevelForcecrouch; // at this fraction of MIN_VALUE, try to force walking
		public float FatigueLevelHelmet; // at this fraction of MIN_VALUE, toggle helmet
		public float FatigueLevelHeartattack; // at this fraction of MIN_VALUE, heart attack
		public float StartingHunger;
		public float StartingThirst;
		public float StartingFatigue;

		public ConfigData() {
		MaxValue = 100f;
		MinValue = -100f; // if less than zero, a severely starved character will have to consume more
		HungryWhen = 0.25f; // if need is this much of maxval, consume
		ThirstyWhen = 0.25f; // if need is this much of maxval, consume
		ThirstPerDay = 300f; //600f;
		HungerPerDay = 100f; //300f;
		DamageSpeedHunger = -0.2f; // 2; // if negative, scale to minvalue for damage. if positive, do this much damage every tick.
		DamageSpeedThirst = -0.3f; //5; // if negative, scale to minvalue for damage.  if positive, do this much damage every tick.
		DefaultModifier = 1f;
		FlyingModifier = 1f;
		RunningModifier = 1.25f;
		SprintingModifier = 1.50f;
		NoModifier = 1f;
		FecesAmount = 0.90f; // if zero, skip creating waste, otherwise, make GreyWater and Organic right after eating, and don't go into details
		UrineAmount = 0.0f; // does eating/drinking generate any amount of the "other" waste? formula is (1-crapamount)*this
		DeathRecovery = 2.00f; // if true, "evacuate" before dying, based on current hunger and thirst level. This number is how much is evacuated if player is at 100%
		FatigueEnabled = true;
		FatigueSitting = 0.1f;
		FatigueCrouching = 0.075f;
		FatigueStanding = 0.05f;
		FatigueWalking = 0f;
		FatigueRunning = -0.05f;
		FatigueFlying  = -0.01f;
		FatigueSprinting = -0.1f;
		ExtraThirstFromFatigue = -2f; // negative: multiply thirst modifier. positive: add to thirst directly.
		FatigueLevelNohealing = 0.01f; // at this fraction of MIN_VALUE, prevent autoheal
		FatigueLevelForcewalk = 0.2f; // at this fraction of MIN_VALUE, try to force walking
		FatigueLevelForcecrouch = 0.5f; // at this fraction of MIN_VALUE, try to force walking
		FatigueLevelHelmet = 0.75f; // at this fraction of MIN_VALUE, toggle helmet
		FatigueLevelHeartattack = 0.999f; // at this fraction of MIN_VALUE, heart attack
		StartingHunger = 100f;
		StartingThirst = 100f;
		StartingFatigue = 100f;

        }
		
    }
}
