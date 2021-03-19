using System.Xml.Serialization;
using VRage.ModAPI;

namespace DailyNeeds {
    public class PlayerData
    {
        public ulong steamid;
        public float hunger;
        public float thirst;
        public float fatigue;

        [XmlIgnore]
        public VRage.Game.MyCharacterMovementEnum LastMovement;
        
        [XmlIgnore]
        public IMyEntity Entity;
        
        [XmlIgnore]
        public bool loaded;

        public PlayerData(ulong id)
        {
            thirst = 100;
            hunger = 100;
            fatigue = 100;
            LastMovement = 0;
            Entity = null;
            steamid = id;
            loaded = false;
        }

        public PlayerData() {
            thirst = 100;
            hunger = 100;
            fatigue = 100;
            LastMovement = 0;
            Entity = null;
            loaded = false;
        }
    }
}
