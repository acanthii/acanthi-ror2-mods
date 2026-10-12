using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.ContentManagement;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Hologram;
using ShaderSwapper;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace LunarConstructor {
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(R2APIContentManager.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LunarConstructor : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "LunarConstructor";
        public const string PluginVersion = "1.0.0";

        private Material constructorMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarCauldron.mat").WaitForCompletion();
        private Material waterMat = Addressables.LoadAssetAsync<Material>("RoR2/Base/bazaar/matBazaarRedWhiteLiquid.mat").WaitForCompletion();

        public static ExpansionDef voidDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
        public static ExpansionDef alloyedDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC3/DLC3.asset").WaitForCompletion();
        public static ExpansionDef hallowedDLC = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC4/DLC4.asset").WaitForCompletion();

        public static GameObject lunarConstructorPrefab;

        public static AssetBundle LunarConstructorAssets;

        public static GameObject ConstructorHologramContent;

        public static ConfigEntry<float> chanceForAccident;
        public static ConfigEntry<float> tempWeight;
        public static ConfigEntry<float> lunarWeight;
        public static ConfigEntry<float> hexWeight;
        public static ConfigEntry<float> voidWeight;
        public static ConfigEntry<float> nothingWeight;

        public GameObject ConstructorCostHologramContentPrefab() {
            var prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/CostHologramContent.prefab").WaitForCompletion().InstantiateClone("CustomCostHologramContentPrefab", false);

            UnityEngine.Object.DestroyImmediate(prefab.GetComponent<CostHologramContent>());
            var hologramContent = prefab.AddComponent<ConstructorCostHologramContent>();
            hologramContent.targetTextMesh = prefab.transform.Find("Text").GetComponent<TextMeshPro>();
            //hologramContent.targetTextMesh.fontSize = 10f;

            return prefab;
        }

        private void Awake() {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LunarConstructor.lunarconstructor_assets")) {
                LunarConstructorAssets = AssetBundle.LoadFromStream(stream);
            }

            base.StartCoroutine(LunarConstructorAssets.UpgradeStubbedShadersAsync());

            InitConfig();

            CreatePrefab();
            Stage.onStageStartGlobal += Stage_onStageStartGlobal;
            On.RoR2.PurchaseInteraction.GetInteractability += LunarConstructor_GetInteractability;
            On.RoR2.Projectile.TinkerProjectile.TransmuteTargetObject += TinkerProjectile_TransmuteTargetObject;

            ConstructorHologramContent = ConstructorCostHologramContentPrefab();
        }

        private void InitConfig() {
            chanceForAccident = Config.Bind(
                "Unstable Creation",
                "Unstable Creation Chance",
                10f,
                new ConfigDescription(
                    "Chance for an Unstable Creation to occur. (10 = 10%)",
                   new AcceptableValueRange<float>(0, 100)
                )
            );
            tempWeight = Config.Bind(
                "Unstable Creation",
                "Temporary Creation Weight",
                50f,
                new ConfigDescription(
                    "Chance for an Temporary Creation to occur. (10 = 10%)",
                   new AcceptableValueRange<float>(0, 100)
                )
            );
            lunarWeight = Config.Bind(
                "Unstable Creation",
                "Lunar Creation Weight",
                25f,
                new ConfigDescription(
                    "Chance for an Lunar Creation to occur. (10 = 10%)",
                   new AcceptableValueRange<float>(0, 100)
                )
            );
            hexWeight = Config.Bind(
                "Unstable Creation",
                "Hex Creation Weight",
                20f,
                new ConfigDescription(
                    "Chance for an Hex Creation to occur. (10 = 10%)",
                   new AcceptableValueRange<float>(0, 100)
                )
            );
            voidWeight = Config.Bind(
                "Unstable Creation",
                "Void Creation Weight",
                5f,
                new ConfigDescription(
                    "Chance for a Void Creation to occur. (10 = 10%)",
                   new AcceptableValueRange<float>(0, 100)
                )
            );
            nothingWeight = Config.Bind(
                "Unstable Creation",
                "No Creation Weight",
                0.5f,
                new ConfigDescription(
                    "Chance for No Creation to occur. (10 = 10%)",
                   new AcceptableValueRange<float>(0, 100)
                )
            );
        }

        private void TinkerProjectile_TransmuteTargetObject(On.RoR2.Projectile.TinkerProjectile.orig_TransmuteTargetObject orig, RoR2.Projectile.TinkerProjectile self, GameObject targetObject) {
            if (!targetObject.TryGetComponent<TinkerableObjectAttributes>(out var component1)) {
                orig(self, targetObject);
                return;
            }

            if (!component1.IsTinkerable) {
                orig(self, targetObject);
                return;
            }

            if (targetObject.TryGetComponent<ConstructorManager>(out var component2)) {
                //Debug.Log("TinkerProjectile_TransmuteTargetObject :: Rerolling...");
                component2.RollItemsServer();
                orig(self, targetObject);
                return;
            }

            orig(self, targetObject);
        }

        private Interactability LunarConstructor_GetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator) {
            if (self.TryGetComponent<ConstructorManager>(out ConstructorManager constructorManager)) {
                CharacterBody characterBody = activator.GetComponent<CharacterBody>();

                if (characterBody && constructorManager.GetBodyHighestScrap(characterBody) != null) {
                    return orig(self, activator);
                }

                return Interactability.ConditionsNotMet;
            }

            return orig(self, activator);
        }

        public static void Stage_onStageStartGlobal(Stage stage) {
            if (NetworkServer.active) {
                switch (SceneManager.GetActiveScene().name) {
                    case "bazaar": {
                            SpawnLunarConstructor(new Vector3(-117.5476f, -23.2836f, -7.2528f), Quaternion.Euler(0f, 135f, 0f));
                            break;
                        }
                    case "moon2": {
                            SpawnLunarConstructor(new Vector3(-232.915f, -186.8033f, -323.1316f), Quaternion.Euler(0f, 64f, 0f));
                            break;
                        }
                    default: {
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
            TinkerableObjectAttributes tinker = lunarConstructorPrefab.AddComponent<TinkerableObjectAttributes>();
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
