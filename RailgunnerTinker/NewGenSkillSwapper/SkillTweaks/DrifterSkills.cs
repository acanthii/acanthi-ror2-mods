using RoR2;
using RoR2.EntitlementManagement;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NewGenSkillSwapper {
    public class DrifterSkills {

        public static EntitlementDef dlc3EntitlementDef;
        public static SkillDef tinkerSkillDef;
        public static SkillDef passiveStatsFromScrapSkillDef;
        public static GameObject rerollTrackingPrefab;
        public static GameObject breakTrackingPrefab;
        public static GameObject invalidTrackingPrefab;

        public static Dictionary<PlayerCharacterMasterController, bool> cachedDlc3EntitlementResult = [];

        private static bool configInitialized = false;

        public static void Awake() {
            On.RoR2.Skills.DrifterSkillDef.HasRequiredJunk += DrifterSkillDef_HasRequiredJunk;
            On.RoR2.Skills.DrifterTrackingSkillDef.HasRequiredJunk += DrifterTrackingSkillDef_HasRequiredJunk;
            On.RoR2.Skills.DrifterTrackingSkillDef.OnExecute += DrifterTrackingSkillDef_OnExecute;
            On.RoR2.Skills.DrifterSkillDef.OnExecute += DrifterSkillDef_OnExecute;
            On.EntityStates.Drifter.CastTinker.OnEnter += CastTinker_OnEnter;
            On.RoR2.DrifterTracker.Awake += DrifterTracker_Awake;
            On.RoR2.DrifterTrashToTreasureController.Start += DrifterTrashToTreasureController_Start;
        }

        public static void Init() {
            Log.Debug("Initializing!");
            dlc3EntitlementDef = Addressables.LoadAssetAsync<EntitlementDef>("RoR2/DLC3/entitlementDLC3.asset").WaitForCompletion();
            tinkerSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("tinker"));
            passiveStatsFromScrapSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("PassiveStatsFromScrap"));
            rerollTrackingPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drifter/TinkerIndicatorReroll.prefab").WaitForCompletion();
            breakTrackingPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drifter/TinkerIndicatorBreak.prefab").WaitForCompletion();
            invalidTrackingPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drifter/TinkerIndicatorBad.prefab").WaitForCompletion();
        }

        public static bool IsDLC3Entitled(CharacterMaster master) {
            if (master.TryGetComponent<PlayerCharacterMasterController>(out PlayerCharacterMasterController pcmc)) {
                return IsDLC3Entitled(pcmc);
            }
            return false;
        }

        public static bool IsDLC3Entitled(PlayerCharacterMasterController pcmc) {
            if (cachedDlc3EntitlementResult.ContainsKey(pcmc))
                return cachedDlc3EntitlementResult[pcmc];

            var entitlementFoundFlag = false;

            if (pcmc.TryGetComponent<PlayerCharacterMasterControllerEntitlementTracker>(out PlayerCharacterMasterControllerEntitlementTracker playerEntitlement)) {
                if (playerEntitlement != null && playerEntitlement.HasEntitlement(dlc3EntitlementDef)) {
                    entitlementFoundFlag = true;
                }
            }

            cachedDlc3EntitlementResult[pcmc] = entitlementFoundFlag;

            return entitlementFoundFlag;
        }

        private static void DrifterTrashToTreasureController_Start(On.RoR2.DrifterTrashToTreasureController.orig_Start orig, DrifterTrashToTreasureController self) {
            Log.Debug($"DrifterTrashToTreasureController_Start Starting...");
            if (!self.TryGetComponent<CharacterBody>(out CharacterBody characterBody)) {
                Log.Debug($"There's no body! DrifterTrashToTreasureController_Start returning.");
                return;
            }

            var skillFoundFlag = false;

            foreach (GenericSkill skill in characterBody.GetComponents<GenericSkill>()) {
                if (passiveStatsFromScrapSkillDef == null)
                    passiveStatsFromScrapSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("PassiveStatsFromScrap"));
                if (skill.skillDef == passiveStatsFromScrapSkillDef) {
                    skillFoundFlag = true;
                }
            }

            if (!skillFoundFlag || !IsDLC3Entitled(characterBody.master)) {
                Log.Debug($"SkillDef either doesn't match or didn't pass entitlement! DrifterTrashToTreasureController_Start disabling and returning.");
                self.enabled = false;
                return;
            }

            Log.Debug($"DrifterTrashToTreasureController_Start Running orig(self)...");

            orig(self);

            Log.Debug($"DrifterTrashToTreasureController_Start is enabled? {self.enabled}");
        }

        private static void DrifterTracker_Awake(On.RoR2.DrifterTracker.orig_Awake orig, DrifterTracker self) {
            self.rerollTrackingPrefab ??= rerollTrackingPrefab;
            self.breakTrackingPrefab ??= breakTrackingPrefab;
            self.invalidTrackingPrefab ??= invalidTrackingPrefab;
            orig(self);
        }

        public static void MatchConfig_DrifterExtension(GameObject body) {
            Log.Debug("MatchConfig_DrifterExtension Initializing!");
            if (!body.TryGetComponent<SkillLocator>(out var skillLocator)) { return; }
            GenericSkill[] skills = [skillLocator.primary, skillLocator.secondary, skillLocator.utility, skillLocator.special];
            GenericSkill trackerSkill = null;

            foreach (GenericSkill skill in skills) {
                if (skill.skillDef == tinkerSkillDef) {
                    trackerSkill = skill;
                    break;
                }
            }

            if (trackerSkill != null) {
                DrifterTracker tracker;
                if (!body.gameObject.GetComponent<DrifterTracker>()) {
                    tracker = body.gameObject.AddComponent<DrifterTracker>();
                } else {
                    tracker = body.gameObject.GetComponent<DrifterTracker>();
                }

                tracker.skillInstance = trackerSkill;
            }

            // Passive

            Log.Debug($"Body: {body.name}; Passive: {passiveStatsFromScrapSkillDef.name}"); //.name is temporary, dw.

            foreach (var skill in body.GetComponents<GenericSkill>()) {
                Log.Debug($"Skill: {skill.skillDef.name}");
            }

            var skillFoundFlag = false;

            foreach (GenericSkill skill in body.GetComponents<GenericSkill>()) {
                if (passiveStatsFromScrapSkillDef == null)
                    passiveStatsFromScrapSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("PassiveStatsFromScrap"));
                if (skill.skillDef == passiveStatsFromScrapSkillDef) {
                    skillFoundFlag = true;
                }
            }

            if (skillFoundFlag && !body.GetComponent<DrifterTrashToTreasureController>()) {
                Log.Debug($"DrifterTrashToTreasureController_Start added!");
                body.AddComponent<DrifterTrashToTreasureController>();
            }
        }

        private static void CastTinker_OnEnter(On.EntityStates.Drifter.CastTinker.orig_OnEnter orig, EntityStates.Drifter.CastTinker self) {
            orig(self);

            if (self.characterBody != DLC3Content.Survivors.Drifter.bodyPrefab && IsDLC3Entitled(self.characterBody.master)) {
                self.FireProjectile();
                self.DoFireEffects();
            }
        }

        private static void DrifterTrackingSkillDef_OnExecute(On.RoR2.Skills.DrifterTrackingSkillDef.orig_OnExecute orig, RoR2.Skills.DrifterTrackingSkillDef self, GenericSkill skillSlot) {
            RoR2.Skills.DrifterTrackingSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterTrackingSkillDef.InstanceData)skillSlot.skillInstanceData;
            skillSlot.stateMachine.SetInterruptState(((RoR2.Skills.SkillDef)self).InstantiateNextState(skillSlot), self.interruptPriority);
            if (!(Object)(object)instanceData.junkController) {
                skillSlot.stock -= self.stockToConsume;
                if (self.cancelSprintingOnActivation) {
                    skillSlot.characterBody.isSprinting = false;
                }
                if (self.resetCooldownTimerOnUse) {
                    skillSlot.rechargeStopwatch = 0f;
                }
                if ((bool)(Object)(object)skillSlot.characterBody) {
                    skillSlot.characterBody.OnSkillActivated(skillSlot);
                }
            } else {
                orig(self, skillSlot);
            }
        }

        private static void DrifterSkillDef_OnExecute(On.RoR2.Skills.DrifterSkillDef.orig_OnExecute orig, RoR2.Skills.DrifterSkillDef self, GenericSkill skillSlot) {
            RoR2.Skills.DrifterSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterSkillDef.InstanceData)skillSlot.skillInstanceData;
            skillSlot.stateMachine.SetInterruptState(((RoR2.Skills.SkillDef)self).InstantiateNextState(skillSlot), self.interruptPriority);
            if (!(Object)(object)instanceData.junkController) {
                skillSlot.stock -= self.stockToConsume;
                if (self.cancelSprintingOnActivation) {
                    skillSlot.characterBody.isSprinting = false;
                }
                if (self.resetCooldownTimerOnUse) {
                    skillSlot.rechargeStopwatch = 0f;
                }
                if ((bool)(Object)(object)skillSlot.characterBody) {
                    skillSlot.characterBody.OnSkillActivated(skillSlot);
                }
            } else {
                orig(self, skillSlot);
            }
        }

        private static bool DrifterSkillDef_HasRequiredJunk(On.RoR2.Skills.DrifterSkillDef.orig_HasRequiredJunk orig, RoR2.Skills.DrifterSkillDef self, GenericSkill skillSlot) {
            RoR2.Skills.DrifterSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterSkillDef.InstanceData)skillSlot.skillInstanceData;
            if (!(Object)(object)instanceData.junkController) {
                return true;
            }
            return orig(self, skillSlot);
        }

        private static bool DrifterTrackingSkillDef_HasRequiredJunk(On.RoR2.Skills.DrifterTrackingSkillDef.orig_HasRequiredJunk orig, RoR2.Skills.DrifterTrackingSkillDef self, GenericSkill skillSlot) {
            if (!((Component)(object)skillSlot.characterBody).gameObject.GetComponent<DrifterTracker>()) {
                ((Component)(object)skillSlot.characterBody).gameObject.AddComponent<DrifterTracker>();
            }
            RoR2.Skills.DrifterTrackingSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterTrackingSkillDef.InstanceData)skillSlot.skillInstanceData;
            if (!(Object)(object)instanceData.junkController) {
                return true;
            }
            return orig(self, skillSlot);
        }
    }
}