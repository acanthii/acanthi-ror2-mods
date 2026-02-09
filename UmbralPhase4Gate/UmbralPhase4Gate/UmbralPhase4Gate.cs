using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using R2API;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EntityStates.BrotherMonster.Weapon;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace UmbralPhase4Gate
{
    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class UmbralPhase4Gate : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "UmbralPhase4Gate";
        public const string PluginVersion = "1.0.0";

        public static bool skipStage4;

        private void Awake() {
            SceneDirector.onPostPopulateSceneServer += SceneDirector_onPostPopulateSceneServer;
            CreateLang();
            skipStage4 = true;
        }

        private void CreateLang() {
            LanguageAPI.Add("UMBRAL_PHASE_4_GATE_CONTEXT", "Open the Umbral Gate... (Enables Phase 4.)");
        }

        private void SceneDirector_onPostPopulateSceneServer(SceneDirector director)
        {
            skipStage4 = true;
            if (SceneManager.GetActiveScene().name == "moon2")
            {
                On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
                On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
                SpawnPhase4Gate();
            }
            else {
                On.RoR2.CharacterMaster.OnBodyStart -= CharacterMaster_OnBodyStart;
                On.RoR2.PurchaseInteraction.OnInteractionBegin -= PurchaseInteraction_OnInteractionBegin;
            }
        }

        private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);

            if (!PhaseCounter.instance) { return; }

            if (body.name == "BrotherHurtBody(Clone)" && PhaseCounter.instance.phase == 4 && skipStage4) {
                body.healthComponent.Suicide();
            }
        }

        private void PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.name == "Phase4Shrine") {
                skipStage4 = false;
            }

            orig(self, activator);
        }

        private void SpawnPhase4Gate() {
            GameObject gateObject = Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/YoungTeleporter.prefab").WaitForCompletion(), new Vector3(1062.23f, -283.1f, 1166.086f), Quaternion.Euler(0, 80f, 0));
            GameObject gateFireObject = Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/Bazaar_Light.prefab").WaitForCompletion().transform.Find("FireLODLevel").gameObject, new Vector3(1062.23f, -283.1f, 1166.086f), Quaternion.Euler(0, 80f, 0));
            gateObject.GetComponent<PurchaseInteraction>().NetworkcontextToken = "UMBRAL_PHASE_4_GATE_CONTEXT";
            gateObject.name = "Phase4Shrine";
            gateFireObject.transform.parent = gateObject.transform;
            gateFireObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            gateFireObject.transform.GetChild(0).gameObject.GetComponent<ParticleSystemRenderer>().material.SetColor("_TintColor", Color.magenta);
            NetworkServer.Spawn(gateObject);
            NetworkServer.Spawn(gateFireObject); //guh???
        }
    }
}
