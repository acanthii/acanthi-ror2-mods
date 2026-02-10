using System.Collections.ObjectModel;
using System.Linq;
using EntityStates;
using EntityStates.Drifter;
using On.EntityStates.Drifter;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace NewGenSkillSwapper
{
    public class DrifterSkills
    {

        public static EntitlementDef alloyedCollectiveEntitlement;
        public static SkillDef tinkerDef;
        public static SkillDef treasureDef;
        private static bool configInitialized = false;

        public static void Init()
        {
            // DO DLC ENTITLEMENT CHECK PLEASE DEAR GOD
            alloyedCollectiveEntitlement = Addressables.LoadAssetAsync<EntitlementDef>("RoR2/DLC3/entitlementDLC3.asset").WaitForCompletion();

            tinkerDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("tinker"));
            treasureDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("PassiveStatsFromScrap"));

            On.RoR2.Skills.DrifterSkillDef.HasRequiredJunk += DrifterSkillDef_HasRequiredJunk;
            On.RoR2.Skills.DrifterTrackingSkillDef.HasRequiredJunk += DrifterTrackingSkillDef_HasRequiredJunk;
            On.RoR2.Skills.DrifterTrackingSkillDef.OnExecute += DrifterTrackingSkillDef_OnExecute;
            On.RoR2.Skills.DrifterSkillDef.OnExecute += DrifterSkillDef_OnExecute;
            On.EntityStates.Drifter.CastTinker.OnEnter += CastTinker_OnEnter;
            On.RoR2.DrifterTracker.Awake += DrifterTracker_Awake;
            On.RoR2.DrifterTrashToTreasureController.OnInventoryChanged += DrifterTrashToTreasureController_OnInventoryChanged;

            // CharacterBody.onBodyStartGlobal += CharacterBody_onBodyStartGlobal;
        }

        private static void DrifterTrashToTreasureController_OnInventoryChanged(On.RoR2.DrifterTrashToTreasureController.orig_OnInventoryChanged orig, DrifterTrashToTreasureController self)
        {
            if (!self.TryGetComponent<CharacterBody>(out CharacterBody characterBody)) {
                return;
            }

            if (characterBody.master.TryGetComponent<PlayerCharacterMasterController>(out PlayerCharacterMasterController pcmc)) {
                if (pcmc.TryGetComponent<PlayerCharacterMasterControllerEntitlementTracker>(out PlayerCharacterMasterControllerEntitlementTracker pcmcEntitlement)) {
                    if (pcmcEntitlement == null)
                    {
                        Debug.Log("RailgunnerTweaks - Exiting TrashToTreasure because entitlement couldn't be verified.");
                        self.enabled = false;
                        return;
                    }
                    else if (!pcmcEntitlement.HasEntitlement(alloyedCollectiveEntitlement))
                    {
                        Debug.Log("RailgunnerTweaks - Exiting TrashToTreasure because player did not pass entitlement.");
                        self.enabled = false;
                        return;
                    }
                }
            }

            treasureDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("PassiveStatsFromScrap"));
            foreach (GenericSkill skill in characterBody.GetComponentsInChildren<GenericSkill>())
            {
                if ((skill._skillFamily as ScriptableObject).name.Contains("Passive"))
                {
                    SkillFamily family = skill._skillFamily;
                    if (skill.skillDef != treasureDef)
                    {
                        self.enabled = false;
                        return;
                    }
                }
            }

            orig(self);
        }

        private static void DrifterTracker_Awake(On.RoR2.DrifterTracker.orig_Awake orig, DrifterTracker self)
        {
            // Why are the prefabs null to begin with??? Gearbox???
            self.rerollTrackingPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drifter/TinkerIndicatorReroll.prefab").WaitForCompletion();
            self.breakTrackingPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drifter/TinkerIndicatorBreak.prefab").WaitForCompletion();
            self.invalidTrackingPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/Drifter/TinkerIndicatorBad.prefab").WaitForCompletion();
            orig(self);
        }

        //private static void CharacterBody_onBodyStartGlobal(CharacterBody body)
        //{
        //    if (!body.isPlayerControlled) { return; }
        //    if (!body.TryGetComponent<SkillLocator>(out var skillLocator)) { return; }
        //    if (body.GetComponent<DrifterTracker>()) { return; } // Already a Drifter probably.

        //    GenericSkill[] skills = [skillLocator.primary, skillLocator.secondary, skillLocator.utility, skillLocator.special];
        //    GenericSkill trackerSkill = null;

        //    SkillDef tinkerDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("tinker"));

        //    foreach (GenericSkill skill in skills) {
        //        if (skill.skillDef == tinkerDef) {
        //            trackerSkill = skill;
        //            break;
        //        }
        //    }

        //    DrifterTracker tracker = body.gameObject.AddComponent<DrifterTracker>();
        //    tracker.indicatorsActive = true;
        //    tracker.skillInstance = trackerSkill;
        //}

        public static void MatchConfig_DrifterExtension(GameObject body)
        {
            //if (configInitialized) { return; }
            if (!body.TryGetComponent<SkillLocator>(out var skillLocator)) { return; }
            GenericSkill[] skills = [skillLocator.primary, skillLocator.secondary, skillLocator.utility, skillLocator.special];
            GenericSkill trackerSkill = null;

            foreach (GenericSkill skill in skills)
            {
                if (skill.skillDef == tinkerDef)
                {
                    trackerSkill = skill;
                    break;
                }
            }

            if (trackerSkill != null) {
                DrifterTracker tracker;
                if (!body.gameObject.GetComponent<DrifterTracker>())
                {
                    tracker = body.gameObject.AddComponent<DrifterTracker>();
                }
                else {
                    tracker = body.gameObject.GetComponent<DrifterTracker>();
                }

                tracker.skillInstance = trackerSkill;
            }

            // Passive

            GenericSkill treasureSkill = null;
            foreach (GenericSkill skill in body.GetComponentsInChildren<GenericSkill>())
            {
                if ((skill._skillFamily as ScriptableObject).name.Contains("Passive"))
                {
                    SkillFamily family = skill._skillFamily;
                    if (skill.skillDef == treasureDef) {
                        treasureSkill = skill;
                    }
                }
            }

            if (treasureSkill != null)
            {
                DrifterTrashToTreasureController treasureController;
                if (!body.gameObject.GetComponent<DrifterTrashToTreasureController>())
                {
                    treasureController = body.gameObject.AddComponent<DrifterTrashToTreasureController>();
                }
                else
                {
                    treasureController = body.gameObject.GetComponent<DrifterTrashToTreasureController>();
                }
            }
            //configInitialized = true;
        }

        private static void CastTinker_OnEnter(On.EntityStates.Drifter.CastTinker.orig_OnEnter orig, EntityStates.Drifter.CastTinker self)
        {
            orig(self);

            if (!((Component)(object)((EntityState)self).characterBody).gameObject.name.Contains("DrifterBody"))
            {
                if (self.characterBody.TryGetComponent<PlayerCharacterMasterController>(out PlayerCharacterMasterController pcmc))
                {
                    if (pcmc.TryGetComponent<PlayerCharacterMasterControllerEntitlementTracker>(out PlayerCharacterMasterControllerEntitlementTracker pcmcEntitlement))
                    {
                        if (pcmcEntitlement == null)
                        {
                            Debug.Log("RailgunnerTweaks - Exiting CastTinker_OnEnter because entitlement couldn't be verified.");
                            return;
                        }
                        else if (!pcmcEntitlement.HasEntitlement(alloyedCollectiveEntitlement))
                        {
                            Debug.Log("RailgunnerTweaks - Exiting CastTinker_OnEnter because player did not pass entitlement.");
                            return;
                        }
                    }
                }
                ((GenericProjectileBaseState)self).FireProjectile();
                ((GenericProjectileBaseState)self).DoFireEffects();
            }
        }

        private static void DrifterTrackingSkillDef_OnExecute(On.RoR2.Skills.DrifterTrackingSkillDef.orig_OnExecute orig, RoR2.Skills.DrifterTrackingSkillDef self, GenericSkill skillSlot)
        {
            RoR2.Skills.DrifterTrackingSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterTrackingSkillDef.InstanceData)skillSlot.skillInstanceData;
            skillSlot.stateMachine.SetInterruptState(((RoR2.Skills.SkillDef)self).InstantiateNextState(skillSlot), self.interruptPriority);
            if (!(Object)(object)instanceData.junkController)
            {
                skillSlot.stock -= self.stockToConsume;
                if (self.cancelSprintingOnActivation)
                {
                    skillSlot.characterBody.isSprinting = false;
                }
                if (self.resetCooldownTimerOnUse)
                {
                    skillSlot.rechargeStopwatch = 0f;
                }
                if ((bool)(Object)(object)skillSlot.characterBody)
                {
                    skillSlot.characterBody.OnSkillActivated(skillSlot);
                }
            }
            else
            {
                orig(self, skillSlot);
            }
        }

        private static void DrifterSkillDef_OnExecute(On.RoR2.Skills.DrifterSkillDef.orig_OnExecute orig, RoR2.Skills.DrifterSkillDef self, GenericSkill skillSlot)
        {
            RoR2.Skills.DrifterSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterSkillDef.InstanceData)skillSlot.skillInstanceData;
            skillSlot.stateMachine.SetInterruptState(((RoR2.Skills.SkillDef)self).InstantiateNextState(skillSlot), self.interruptPriority);
            if (!(Object)(object)instanceData.junkController)
            {
                skillSlot.stock -= self.stockToConsume;
                if (self.cancelSprintingOnActivation)
                {
                    skillSlot.characterBody.isSprinting = false;
                }
                if (self.resetCooldownTimerOnUse)
                {
                    skillSlot.rechargeStopwatch = 0f;
                }
                if ((bool)(Object)(object)skillSlot.characterBody)
                {
                    skillSlot.characterBody.OnSkillActivated(skillSlot);
                }
            }
            else
            {
                orig(self, skillSlot);
            }
        }

        private static bool DrifterSkillDef_HasRequiredJunk(On.RoR2.Skills.DrifterSkillDef.orig_HasRequiredJunk orig, RoR2.Skills.DrifterSkillDef self, GenericSkill skillSlot)
        {
            RoR2.Skills.DrifterSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterSkillDef.InstanceData)skillSlot.skillInstanceData;
            if (!(Object)(object)instanceData.junkController)
            {
                return true;
            }
            return orig(self, skillSlot);
        }

        private static bool DrifterTrackingSkillDef_HasRequiredJunk(On.RoR2.Skills.DrifterTrackingSkillDef.orig_HasRequiredJunk orig, RoR2.Skills.DrifterTrackingSkillDef self, GenericSkill skillSlot)
        {
            if (!((Component)(object)skillSlot.characterBody).gameObject.GetComponent<DrifterTracker>())
            {
                ((Component)(object)skillSlot.characterBody).gameObject.AddComponent<DrifterTracker>();
            }
            RoR2.Skills.DrifterTrackingSkillDef.InstanceData instanceData = (RoR2.Skills.DrifterTrackingSkillDef.InstanceData)skillSlot.skillInstanceData;
            if (!(Object)(object)instanceData.junkController)
            {
                return true;
            }
            return orig(self, skillSlot);
        }
    }
}