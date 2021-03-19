using System;
using Sandbox.ModAPI;

namespace DailyNeeds
{
    public class ConfigDataStore
    {
        public ConfigData ConfigData;
        public string Filename;

        public ConfigDataStore()
        {
            Filename = "ConfigData.xml";
            ConfigData = new ConfigData();
        }

        public float MaxValue => ConfigData.MaxValue;
        public float MinValue => ConfigData.MinValue;
        public float HungryWhen => ConfigData.HungryWhen;
        public float ThirstyWhen => ConfigData.ThirstyWhen;
        public float ThirstPerDay => ConfigData.ThirstPerDay;
        public float HungerPerDay => ConfigData.HungerPerDay;
        public float DamageSpeedHunger => ConfigData.DamageSpeedHunger;
        public float DamageSpeedThirst => ConfigData.DamageSpeedThirst;
        public float DefaultModifier => ConfigData.DefaultModifier;
        public float FlyingModifier => ConfigData.FlyingModifier;
        public float RunningModifier => ConfigData.RunningModifier;
        public float SprintingModifier => ConfigData.SprintingModifier;
        public float NoModifier => ConfigData.NoModifier;
        public float FecesAmount => ConfigData.FecesAmount;
        public float UrineAmount => ConfigData.UrineAmount;
        public float DeathRecovery => ConfigData.DeathRecovery;

        public bool FatigueEnabled => ConfigData.FatigueEnabled;
        public float FatigueSitting => ConfigData.FatigueSitting;
        public float FatigueCrouching => ConfigData.FatigueCrouching;
        public float FatigueStanding => ConfigData.FatigueStanding;
        public float FatigueWalking => ConfigData.FatigueWalking;
        public float FatigueRunning => ConfigData.FatigueRunning;
        public float FatigueFlying => ConfigData.FatigueFlying;
        public float FatigueSprinting => ConfigData.FatigueSprinting;
        public float ExtraThirstFromFatigue => ConfigData.ExtraThirstFromFatigue;
        public float FatigueLevelNohealing => ConfigData.FatigueLevelNohealing;
        public float FatigueLevelForcewalk => ConfigData.FatigueLevelForcewalk;
        public float FatigueLevelForcecrouch => ConfigData.FatigueLevelForcecrouch;
        public float FatigueLevelHelmet => ConfigData.FatigueLevelHelmet;
        public float FatigueLevelHeartattack => ConfigData.FatigueLevelHeartattack;
        public float StartingHunger => ConfigData.StartingHunger;
        public float StartingThirst => ConfigData.StartingThirst;
        public float StartingFatigue => ConfigData.StartingFatigue;


        public void Save()
        {
            try
            {
                var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(ConfigData));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(ConfigData));
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                Logging.Instance.WriteLine("(FoodSystem) Config Save Data Error: " + e.Message + "\n" + e.StackTrace);
            }
        }

        public bool Load()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(ConfigData)))
                {
                    ConfigData = new ConfigData();
                    return false;
                }

                var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(ConfigData));
                var xmlText = reader.ReadToEnd();
                reader.Close();

                ConfigData = MyAPIGateway.Utilities.SerializeFromXML<ConfigData>(xmlText);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("ERROR", "Error: " + e.Message + "\n" + e.StackTrace);
                ConfigData = new ConfigData();
                return false;
            }

            return true;
        }

        public void Clear()
        {
            ConfigData = new ConfigData();
        }
    }
}