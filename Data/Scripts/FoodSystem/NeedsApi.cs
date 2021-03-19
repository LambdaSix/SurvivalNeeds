using System;
using Sandbox.ModAPI;

namespace DailyNeeds
{
    public class NeedsApi {
        public class Event {
            public enum Type {
                RegisterEdibleItem,
                RegisterDrinkableItem
            };
    
            public Type type;
            public object payload;
        }

        // TODO: Refactor to single "RegisterFoodItem"

        public class RegisterEdibleItemEvent {
            public RegisterEdibleItemEvent(string szItemName,  float value) {
                this.item = szItemName;
                this.value = value;
            }
        
            public string item;
            public float value;
        }
        
        public class RegisterDrinkableItemEvent {
            public RegisterDrinkableItemEvent(string szItemName,  float value) {
                this.item = szItemName;
                this.value = value;
            }
        
            public string item;
            public float value;
        }
    
        public NeedsApi() {
        
        }
        
        public void RegisterItem(string itemName, FoodInfo info)
        {
            if (info.Hunger.HasValue)
            {
                RegisterEdible(itemName, info.Hunger.Value);
            }

            if (info.Thirst != null)
            {
                RegisterDrinkable(itemName, info.Thirst.Value);
            }
        }

        [Obsolete("Use RegisterItem(string, FoodInfo) instead")]
        public void RegisterEdibleItem(string szItemName, float value) => RegisterEdible(szItemName, value);

        [Obsolete("Use RegisterItem(string, FoodInfo) instead")]
        public void RegisterDrinkableItem(string szItemName, float value) => RegisterDrinkable(szItemName, value);

        private void RegisterEdible(string itemName, float value)
        {
            Event message = new Event();
            message.type = Event.Type.RegisterEdibleItem;
            message.payload = new RegisterEdibleItemEvent(itemName, value);

            MyAPIGateway.Utilities.SendModMessage(1339, message);
        }

        private void RegisterDrinkable(string itemName, float value)
        {
            Event message = new Event();
            message.type = Event.Type.RegisterDrinkableItem;
            message.payload = new RegisterDrinkableItemEvent(itemName, value);

            MyAPIGateway.Utilities.SendModMessage(1339, message);
        }

        public void SetPlayerHunger(ulong player, float value) {
        
        }
        
        public void SetPlayerThirst(ulong player, float value) {
        
        }
        
        public void AddPlayerHunger(ulong player, float value) {
        
        }
        
        public void AddPlayerThirst(ulong player, float value) {
        
        }  
    }

}