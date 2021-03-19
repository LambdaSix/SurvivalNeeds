using System;
using System.Collections.Generic;

namespace DailyNeeds
{
    public class FoodInfo
    {
        // Negative values indicate only filling to MaxValue/2

        public float? Thirst { get; set; }
        public float? Hunger { get; set; }
        public float? Fatigue { get; set; }
    }

    public static class FoodDefaults
    {
        // TODO: Load this from XML Dictionary
        public static void Load()
        {
            throw new NotImplementedException();
        }

        public static Dictionary<string, FoodInfo> Defaults = new Dictionary<string, FoodInfo>()
        {
            ["WaterFood"] = new FoodInfo {Thirst = 60f},
            ["CoffeeFood"] = new FoodInfo {Fatigue = 100f, Thirst = 60f},
            ["SabiroidBouillon"] = new FoodInfo {Thirst = 35f, Hunger = 35f},
            ["WolfBouillon"] = new FoodInfo {Thirst = 35f, Hunger = 35f},
            ["LuxuryMeal"] = new FoodInfo {Hunger = 100f},
            ["SabiroidSteak"] = new FoodInfo {Hunger = 50f},
            ["WolfSteak"] = new FoodInfo { Hunger = 50f},
            ["SabiroidOmelette"] = new FoodInfo{Hunger = 45f},
            ["VeganFood"] = new FoodInfo { Hunger = 35f},
            ["SubFresh"] = new FoodInfo { Hunger = -15f},
            ["ArtificialFood"] = new FoodInfo { Hunger = -10f},
            ["NotBeefBurger"] = new FoodInfo { Hunger = 75f},
            ["ToFurkey"] = new FoodInfo { Hunger = 80f},
            ["SpaceMealBar"] = new FoodInfo { Hunger = 60f},
            ["SpacersBreakfast"] = new FoodInfo { Hunger = 75f},
            ["HotChocolate"] = new FoodInfo { Hunger = 15f, Thirst = 45f, Fatigue = 45f},
            ["ProteinShake"] = new FoodInfo { Hunger = 35f, Thirst = 35f, Fatigue = 35f},
            ["MossMeal"] = new FoodInfo { Hunger = -10f},
            ["WolfBurger"] = new FoodInfo { Hunger = 70f},
            ["MartianSpecial"] = new FoodInfo { Hunger = 35f},
            ["Tomato"] = new FoodInfo { Hunger = -15f},
            ["Carrot"] = new FoodInfo { Hunger = -15f},
            ["Cucumber"] = new FoodInfo {  Hunger = -15f},
            ["Potato"] = new FoodInfo { Hunger = -15f},


        };
    }
}