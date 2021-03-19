using System;
using DailyNeeds.AnimationsAndLighting;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace DailyNeeds
{
    public static class GameLogic
    {
        public static bool SkipProcessing()
        {
            if (MyAPIGateway.Session == null)
                return true;

            var isHost = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE ||
                         MyAPIGateway.Multiplayer.IsServer;

            var isDedicatedHost = isHost && MyAPIGateway.Utilities.IsDedicated;

            if (isDedicatedHost)
                return true;
            return false;
        }
    }

    public static class LightTools
    {
        public static void ToggleLight(MyLight light)
        {
            if (light != null)
            {
                light.LightOn = !light.LightOn;
                light.UpdateLight();
            }
        }

        /// <summary>
        /// Updates the working state of an entity with regards to it's lights, calling optional actions.
        /// </summary>
        /// <param name="block">The block to manipulate emissiveness on</param>
        /// <param name="light">The light to modify</param>
        /// <param name="lightOrigin">The origin of the light</param>
        /// <param name="lightColor">Color of the light</param>
        /// <param name="emissive">Emissiveness of the main model part</param>
        /// <param name="subpartEmissive">Emissiveness of the model's subparts</param>
        /// <param name="emV">Emissiveness colour components</param>
        /// <param name="updateLogic">Methods to call</param>
        public static void UpdateWorkingState(IMyCubeBlock block, 
            MyLight light, Vector3D? lightOrigin, Color lightColor, float emissive,
            float subpartEmissive, Vector4 emV, Action updateLogic)
        {
            if (GameLogic.SkipProcessing()) return;

            var entity = block as MyEntity;
            var subparts = entity?.Subparts;

            if (block.IsWorking)
            {
                CreateOrUpdateLight(entity, light, lightColor, lightOrigin);
                MyCubeBlockEmissive.SetEmissiveParts(entity,
                    emissive,
                    Color.FromNonPremultiplied(emV),
                    Color.White);

                ToggleLight(light);

                if (subparts != null)
                {
                    foreach (var subPart in subparts)
                    {
                        MyCubeBlockEmissive.SetEmissiveParts(subPart.Value, subpartEmissive,
                            Color.FromNonPremultiplied(emV),
                            Color.White);
                    }
                }

                updateLogic?.Invoke();
            }
            else
            {
                ToggleLight(light);

                if (subparts == null) return;

                foreach (var subPart in subparts)
                {
                    subPart.Value.SetEmissiveParts("Emissive", Color.Red, 1.0f);
                }
            }
        }

        public static void CreateOrUpdateLight(MyEntity entity, MyLight light, Color color, Vector3D? originPoint = null)
        {
            var lightPosition = originPoint ?? entity.WorldMatrix.Translation;
            var lightRange = 1.5f; //Range of light
            var lightIntensity = 5.0f; //Light intensity
            var lightFalloff = 1.5f; //Light falloff
            var lightOffset = 0.5f; //Light offset

            if (light == null) //Ignore - checks if there is a light and if not makes it.
            {
                light = MyLights.AddLight(); //Ignore - adds the light to the games lighting system
                light.Start(lightPosition, color.ToVector4(), lightRange,
                    ""); // Ignore- Determines the lights position, initial color and initial range.
                light.Intensity = lightIntensity; //Ignore - sets light intensity from above values.
                light.Falloff = lightFalloff; //Ignore - sets light fall off from above values.
                light.PointLightOffset = lightOffset; //Ignore - sets light offset from above values.
                light.LightOn = true; //Ignore - turns light on
            }
            else
            {
                // Updates the lights position constantly. You'll need help if you want it somewhere else.
                light.Position = lightPosition; 
                light.UpdateLight(); //Ignore - tells the game to update the light.
            }
        }
    }
}
