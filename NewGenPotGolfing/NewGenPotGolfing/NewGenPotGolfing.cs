using System;
using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace NewGenPotGolfing
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NewGenPotGolfing : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "NewGenPotGolfing";
        public const string PluginVersion = "1.0.0";

        public static BodyIndex potBodyIndex;
        public static float reductionStrength;

        public void Awake()
        {
            Log.Init(Logger);

            reductionStrength = Config.Bind("Reduction", "Reduction Strength", 10f, "Force is divided by this amount.").Value;

            Debug.Log("NewGenPotGolfing :: We're officially skibidi!");

            On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool += HealthComponent_TakeDamageForce_DamageInfo_bool_bool;
            RoR2.BodyCatalog.availability.CallWhenAvailable(() =>
            {
                potBodyIndex = BodyCatalog.FindBodyIndexCaseInsensitive("ExplosivePotDestructibleBody");
            });
        }

        // damageinfo implementation of takedamageforce
        private void HealthComponent_TakeDamageForce_DamageInfo_bool_bool(On.RoR2.HealthComponent.orig_TakeDamageForce_DamageInfo_bool_bool orig, HealthComponent self, DamageInfo damageInfo, bool alwaysApply, bool disableAirControlUntilCollision)
        {
            if (self.body.bodyIndex == potBodyIndex) {
                damageInfo.force = damageInfo.force / reductionStrength;
            }
            orig(self, damageInfo, alwaysApply, disableAirControlUntilCollision);
        }
    }
}
