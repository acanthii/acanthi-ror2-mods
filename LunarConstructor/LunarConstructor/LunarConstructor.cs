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
using R2API.ContentManagement;
using System.Reflection;
using ShaderSwapper;
using System.Linq;
using RoR2.Hologram;
using TMPro;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace LunarConstructor
{
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(R2APIContentManager.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LunarConstructor : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "LunarConstructor";
        public const string PluginVersion = "1.0.0";

        private Material constructorMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarCauldron.mat").WaitForCompletion();
        private Material waterMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarRedWhiteLiquid.mat").WaitForCompletion();

        public static GameObject lunarConstructorPrefab;

        public static AssetBundle LunarConstructorAssets;

        public static GameObject ConstructorHologramContent;
        public GameObject ConstructorCostHologramContentPrefab()
        {
            var prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/CostHologramContent.prefab").WaitForCompletion().InstantiateClone("CustomCostHologramContentPrefab", false);

            UnityEngine.Object.DestroyImmediate(prefab.GetComponent<CostHologramContent>());
            var hologramContent = prefab.AddComponent<ConstructorCostHologramContent>();
            hologramContent.targetTextMesh = prefab.transform.Find("Text").GetComponent<TextMeshPro>();
            //hologramContent.targetTextMesh.fontSize = 10f;

            return prefab;
        }

        private void Awake() {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LunarConstructor.lunarconstructor_assets"))
            {
                LunarConstructorAssets = AssetBundle.LoadFromStream(stream);
            }

            base.StartCoroutine(LunarConstructorAssets.UpgradeStubbedShadersAsync());

            CreatePrefab();
            Stage.onStageStartGlobal += Stage_onStageStartGlobal;
            On.RoR2.PurchaseInteraction.GetInteractability += LunarConstructor_GetInteractability;

            ConstructorHologramContent = ConstructorCostHologramContentPrefab();
        }

        private Interactability LunarConstructor_GetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.TryGetComponent<ConstructorManager>(out ConstructorManager constructorManager)) 
            {
                CharacterBody characterBody = activator.GetComponent<CharacterBody>();

                if (characterBody && constructorManager.GetBodyHighestScrap(characterBody) != null) 
                {
                    return orig(self, activator);
                }

                return Interactability.ConditionsNotMet;
            }

            return orig(self, activator);
        }

        public static void Stage_onStageStartGlobal(Stage stage)
        {
            if (NetworkServer.active)
            {
                switch (SceneManager.GetActiveScene().name)
                {
                    case "bazaar":
                        {
                            SpawnLunarConstructor(new Vector3(-117.5476f, -23.2836f, -7.2528f), Quaternion.Euler(0f, 135f, 0f));
                            break;
                        }
                    default:
                        {
                            return;
                        }
                }
            }
        }

        public static void SpawnLunarConstructor(Vector3 pos, Quaternion rot) {
            GameObject constructor = UnityEngine.Object.Instantiate(lunarConstructorPrefab, pos, rot);
            NetworkServer.Spawn(constructor);
        }

        private void CreatePrefab() {
            lunarConstructorPrefab = PrefabAPI.InstantiateClone(LunarConstructor.LunarConstructorAssets.LoadAsset<GameObject>("lunarconstructor.fbx"), "lunarConstructor");

            lunarConstructorPrefab.name = "LunarConstructor";
            lunarConstructorPrefab.AddComponent<NetworkIdentity>();
            lunarConstructorPrefab.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            lunarConstructorPrefab.transform.GetChild(1).GetComponent<MeshRenderer>().sharedMaterial = constructorMat;
            lunarConstructorPrefab.transform.GetChild(1).gameObject.AddComponent<BoxCollider>();

            lunarConstructorPrefab.transform.GetChild(1).transform.rotation = Quaternion.Euler(270f, 90f, 0f);
            lunarConstructorPrefab.transform.GetChild(1).transform.localScale = new Vector3(150f, 150f, 115f);
            lunarConstructorPrefab.transform.GetChild(1).transform.localPosition = new Vector3(0f, -1f, 0f);


            lunarConstructorPrefab.transform.GetChild(0).transform.localScale = new Vector3(150f, 150f, 115f);
            lunarConstructorPrefab.transform.GetChild(0).transform.localPosition = new Vector3(0f, 0f, 0f);
            lunarConstructorPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            lunarConstructorPrefab.transform.GetChild(0).gameObject.AddComponent<Light>().color = Color.white;

            var lunarInfectionLarge = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/Bazaar_LunarInfectionLarge.prefab").WaitForCompletion();

            GameObject shards = PrefabAPI.InstantiateClone(lunarInfectionLarge, "lunarConstructorShards");

            shards.transform.parent = lunarConstructorPrefab.transform;
            shards.transform.localPosition = new Vector3(0f, -1f, 0f);
            shards.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            // PickupDisplay
            GameObject pickupDisplayObject = new GameObject("PickupDisplay", typeof(PickupDisplay), typeof(Highlight));

            pickupDisplayObject.transform.parent = lunarConstructorPrefab.transform;
            pickupDisplayObject.transform.localPosition = new Vector3(0f, 1.65f, -0.75f);

            // Hologram
            GameObject hologramObject = new GameObject("Hologram", typeof(HologramProjector));

            hologramObject.transform.parent = lunarConstructorPrefab.transform;
            hologramObject.transform.localPosition = new Vector3(0f, -0.2f, 0.9f);
            hologramObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Interactable
            ConstructorManager mgr = lunarConstructorPrefab.AddComponent<ConstructorManager>();
            PurchaseInteraction interaction = lunarConstructorPrefab.AddComponent<PurchaseInteraction>();
            interaction.contextToken = "Construct Item (1 Scrap)";
            interaction.NetworkdisplayNameToken = "Lunar Constructor";
            mgr.purchaseInteraction = interaction;
            lunarConstructorPrefab.GetComponent<Highlight>().targetRenderer = lunarConstructorPrefab.transform.GetChild(1).GetComponent<MeshRenderer>();
            GameObject something = new GameObject();
            GameObject trigger = Instantiate(something, lunarConstructorPrefab.transform);
            trigger.AddComponent<BoxCollider>().isTrigger = true;
            trigger.AddComponent<EntityLocator>().entity = lunarConstructorPrefab;

            PrefabAPI.RegisterNetworkPrefab(lunarConstructorPrefab);
        }
    }
}
