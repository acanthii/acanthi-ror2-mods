using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RiskOfOptions;
using RiskOfOptions.Options;
using RoR2;
using ShaderSwapper;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ReviveAPI.ReviveAPI;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace NewGenRaces {
    [BepInDependency(LanguageAPI.PluginGUID)]
    //[BepInDependency(ShaderSwapper.ShaderSwapper)]
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(ReviveAPI.ReviveAPI.ModGuid)]
    [BepInDependency(RiskOfOptions.PluginInfo.PLUGIN_GUID)]
    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NewGenRaces : BaseUnityPlugin {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "NewGenRaces";
        public const string PluginVersion = "1.0.0";

        public static ConfigEntry<bool> reviveEnabled;
        public static ConfigEntry<bool> addItem;
        public static ConfigEntry<bool> addToDifficulty;
        public static ConfigEntry<int> baseSecondsToAdd;
        public static ConfigEntry<int> additiveSecondsToAdd;

        public static ConfigEntry<bool> disableNewts;

        public static AssetBundle Assets;

        public static int deaths = 0;

        public static PendingOnRevive[] customPendingOnRevives {
            get {
                PendingOnRevive pendingOnRevive = new PendingOnRevive();
                pendingOnRevive.timer = 2f;
                pendingOnRevive.onReviveDelegate = SimpleRespawn;
                PendingOnRevive pendingOnRevive2 = pendingOnRevive;
                pendingOnRevive = new PendingOnRevive();
                pendingOnRevive.timer = 2f;
                pendingOnRevive.onReviveDelegate = SimpleRespawnSound;
                PendingOnRevive pendingOnRevive3 = pendingOnRevive;
                pendingOnRevive = new PendingOnRevive();
                pendingOnRevive.timer = 2f;
                pendingOnRevive.onReviveDelegate = OnRaceRevive;
                PendingOnRevive pendingOnRevive4 = pendingOnRevive;
                return new PendingOnRevive[3] { pendingOnRevive2, pendingOnRevive3, pendingOnRevive4 };
            }
        }

        private void Awake() {
            InitConfig();

            if (reviveEnabled.Value || addItem.Value) {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NewGenRaces.artifactoftime_assets")) {
                    Assets = AssetBundle.LoadFromStream(stream);
                }

                StartCoroutine(Assets.UpgradeStubbedShadersAsync());
            }

            if (reviveEnabled.Value) {
                Artifact.Init();
            }
            if (addItem.Value) {
                Item.Init();
            }

            ReviveAPI.ReviveAPI.AddCustomRevive(RaceReviveCondition, customPendingOnRevives, -99);
            Run.onRunStartGlobal += RunStart_ResetDeathCounter;
            On.EntityStates.GenericCharacterDeath.OnEnter += GenericCharacterDeath_GlassDeath;
            Stage.onStageStartGlobal += Stage_EraseNewt;

            IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += (il) => {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After,
                        x => x.MatchLdarg(0),
                        x => x.MatchCallOrCallvirt<Run>("GetRunStopwatch")
                    )) {
                    c.EmitDelegate<Func<float, float>>(time => {
                        return addToDifficulty.Value ? time : time - (deaths * baseSecondsToAdd.Value + (additiveSecondsToAdd.Value * deaths));
                    });
                } else {
                    Debug.Log(il.Method.Name + " IL Hook failed!");
                }
            };
        }

        private void Stage_EraseNewt(Stage obj) {
            if (!disableNewts.Value)
                return;

            if (reviveEnabled.Value && !Artifact.ArtifactEnabled)
                return;

            PortalStatueBehavior[] newtArray =
                (from newt in UnityEngine.Object.FindObjectsOfType<PortalStatueBehavior>(includeInactive: true)
                 where newt.portalType == PortalStatueBehavior.PortalType.Shop
                 select newt).ToArray();

            foreach (PortalStatueBehavior newt in newtArray) {
                newt.GetComponent<PurchaseInteraction>().available = false;
            }
        }

        private void GenericCharacterDeath_GlassDeath(On.EntityStates.GenericCharacterDeath.orig_OnEnter orig, EntityStates.GenericCharacterDeath self) {
            var characterMaster = self.characterBody.master;

            if (characterMaster != null && RaceReviveCondition(characterMaster)) {
                self.characterBody.isGlass = true;
            }

            orig(self);
        }

        private void RunStart_ResetDeathCounter(Run obj) {
            deaths = 0;
        }

        private static bool RaceReviveCondition(CharacterMaster characterMaster) {
            // Skip non-player entities.
            if (!(bool)characterMaster.playerCharacterMasterController)
                return false;

            // If the artifact is enabled, return it's state.
            if (reviveEnabled.Value) {
                return Artifact.ArtifactEnabled;
            }

            // Otherwise, return true always.
            return true;
        }

        private static void OnRaceRevive(CharacterMaster characterMaster) {
            AddTime();

            if (addItem.Value) {
                characterMaster.inventory.GiveItemPermanent(Item.ItemDef);
                CharacterMasterNotificationQueue.PushPickupNotification(characterMaster, PickupCatalog.FindPickupIndex(Item.ItemDef.itemIndex));
            }

            deaths++;
        }

        private static void AddTime() {
            float runTime = Run.instance.GetRunStopwatch();
            float addTime = baseSecondsToAdd.Value + (additiveSecondsToAdd.Value * deaths);
            Run.instance.SetRunStopwatch(runTime + addTime);
        }

        private void InitConfig() {
            reviveEnabled = Config.Bind(
                "Main Settings",
                "Artifact",
                true,
                "Allows you to disable/enable the mod as an artifact. Otherwise always enabled. Reload Required!"
            );

            addItem = Config.Bind(
                "Main Settings",
                "Add Item",
                true,
                "ItemDisplay to mock you for dying :3. Reload Required!"
            );

            addToDifficulty = Config.Bind(
                "Main Settings",
                "Add To Difficulty",
                false,
                "Each death adds to the enemy difficulty! WARNING: This compounds quickly."
            );

            baseSecondsToAdd = Config.Bind(
                "Main Settings",
                "Base Seconds To Add",
                120,
                "Base seconds to add to the timer."
            );

            additiveSecondsToAdd = Config.Bind(
                "Main Settings",
                "Additive Seconds To Add",
                60,
                "Seconds to add accumulatively after each death."
            );

            disableNewts = Config.Bind(
                "Run Settings",
                "Disable Newt Altars",
                false,
                "Disables Newt Altars. Only works if reviving is enabled!"
            );

            ModSettingsManager.SetModDescription("A simple races mod that revives you at the cost of time.");
            ModSettingsManager.AddOption(new CheckBoxOption(reviveEnabled, true));
            ModSettingsManager.AddOption(new CheckBoxOption(addItem, true));
            ModSettingsManager.AddOption(new CheckBoxOption(addToDifficulty));
            ModSettingsManager.AddOption(new IntFieldOption(baseSecondsToAdd));
            ModSettingsManager.AddOption(new IntFieldOption(additiveSecondsToAdd));

            ModSettingsManager.AddOption(new CheckBoxOption(disableNewts));
        }
    }
}
