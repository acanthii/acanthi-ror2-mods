using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace NewGenSkillSwapper {
    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NewGenSkillSwapper : BaseUnityPlugin {
        public class SkillSwapConfig {
            public string skillName;

            public string survivorName;

            public string family;
        }

        public class AKBankConfig {
            public string survivorOrigin;

            public string survivorTarget;
        }

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "RailgunnerTinker";
        public const string PluginVersion = "1.0.0";

        public static List<SkillSwapConfig> skillSwapConfig = new List<SkillSwapConfig>();
        public static List<AKBankConfig> akbankConfig = new List<AKBankConfig>();

        public static ConfigEntry<string> skillsToSwap;
        public static ConfigEntry<string> akBankToTransfer;

        public static ConfigEntry<bool> debugMode;


        public void ParseConfig() {
            skillsToSwap = Config.Bind<string>("RailgunnerTinker", "skillsToSwap", "(Tinker,Railgunner,special),(PassiveStatsFromScrap,Railgunner,passive),(Tinker,Swarmling,special)", "This should NOT be touched if you don't know what you're doing. Theoretically you could use this to add Tinker to more survivors but it's not a supported feature. Here be Dragons! A list of skills to swap. Should be formatted like this: (skillDefName,survivorDefName,family),(otherSkillDefName,otherSurvivorDefName,otherFamily)");
            akBankToTransfer = Config.Bind<string>("RailgunnerTinker", "akBankToTransfer", "", "This should NOT be touched if you don't know what you're doing. Should be formatted like this (origin -> target): (survivorDefName,survivorDefName)");
            debugMode = Config.Bind<bool>("RailgunnerTinker", "debugMode", false, "Prints all names for skills and survivors.");
            Log.Debug("Parsing Config...");
            string[] array = skillsToSwap.Value.Trim().Split("),(");
            string[] array2 = array;
            foreach (string item in array2) {
                if (string.IsNullOrEmpty(item)) continue;
                string itemTrimmed = item.Trim('(', ')');
                string[] pieces = itemTrimmed.Split(',');
                skillSwapConfig.Add(new SkillSwapConfig {
                    skillName = pieces[0],
                    survivorName = pieces[1],
                    family = pieces[2]
                });
            }

            string[] array3 = akBankToTransfer.Value.Trim().Split("),(");
            string[] array4 = array3;
            foreach (string item2 in array4) {
                if (string.IsNullOrEmpty(item2)) continue;
                string itemTrimmed = item2.Trim('(', ')');
                string[] pieces = itemTrimmed.Split(',');
                akbankConfig.Add(new AKBankConfig {
                    survivorOrigin = pieces[0],
                    survivorTarget = pieces[1]
                });
            }
        }

        public static void PrintDebug() {
            if (debugMode.Value) {
                Log.Debug("Printing skill names... Disable debugMode in config to disable this!");
                foreach (SkillDef item in SkillCatalog.allSkillDefs) {
                    Log.Debug(item.skillName + " || Token (for reference, dont use!): " + item.skillNameToken);
                }
                Log.Debug("Printing survivor names... Disable debugMode in config to disable this!");
                {
                    foreach (SurvivorDef item2 in SurvivorCatalog.allSurvivorDefs) {
                        Log.Debug(item2.cachedName);
                    }
                    return;
                }
            }
            Log.Debug("Skipping logs...");
        }

        public static void MatchConfig() {
            foreach (AKBankConfig akbankItem in akbankConfig) {
                SurvivorDef survivorOriginDef = SurvivorCatalog.FindSurvivorDef(akbankItem.survivorOrigin);
                SurvivorDef survivorTargetDef = SurvivorCatalog.FindSurvivorDef(akbankItem.survivorTarget);
                if (!survivorOriginDef || !survivorTargetDef) {
                    Log.Debug("Couldn't transfer AKBank. Skipping...");
                    continue;
                }
                AddSoundbank(survivorOriginDef, survivorTargetDef);
            }
            foreach (SkillSwapConfig item in skillSwapConfig) {
                int skillIdx = SkillCatalog.FindSkillIndexByName(item.skillName);
                if (skillIdx == -1) {
                    foreach (SkillDef skillDefIterate in SkillCatalog.allSkillDefs) {
                        if (item.skillName == skillDefIterate.skillNameToken) {
                            skillIdx = skillDefIterate.skillIndex;
                        }
                    }
                }
                if (skillIdx == -1) {
                    Log.Debug("Couldn't find skill. Skipping...");
                    continue;
                }
                SkillDef skillDef = SkillCatalog.GetSkillDef(skillIdx);
                SurvivorDef survivorDef = SurvivorCatalog.FindSurvivorDef(item.survivorName);
                if (!skillDef || !survivorDef) {
                    Log.Debug("Couldn't add skill. Skipping...");
                    continue;
                }
                SkillLocator skillLocator = survivorDef.bodyPrefab.GetComponent<SkillLocator>();
                if (!(UnityEngine.Object)(object)skillLocator) {
                    Log.Debug("No SkillLocator on " + survivorDef.cachedName + "!");
                    continue;
                }
                SkillFamily skillFamily;
                switch (item.family.ToLower()) {
                    case "primary":
                        skillFamily = skillLocator.primary.skillFamily;
                        ApplySkillFamilyResize(skillFamily, skillDef);
                        break;
                    case "secondary":
                        skillFamily = skillLocator.secondary.skillFamily;
                        ApplySkillFamilyResize(skillFamily, skillDef);
                        break;
                    case "utility":
                        skillFamily = skillLocator.utility.skillFamily;
                        ApplySkillFamilyResize(skillFamily, skillDef);
                        break;
                    case "special":
                        skillFamily = skillLocator.special.skillFamily;
                        ApplySkillFamilyResize(skillFamily, skillDef);
                        break;
                    case "passive":
                        foreach (GenericSkill skill in survivorDef.bodyPrefab.GetComponentsInChildren<GenericSkill>()) {
                            if ((skill._skillFamily as ScriptableObject).name.Contains(survivorDef.cachedName + "Passive") ||
                                (skill._skillFamily as ScriptableObject).name.Contains(survivorDef.cachedName + "BodyPassive")) {
                                SkillFamily family = skill._skillFamily;
                                ApplySkillFamilyResize(family, skillDef);
                            }
                        }
                        break;
                    case "extrafirst":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Log.Debug("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 0);
                        break;
                    case "extrasecond":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Log.Debug("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 1);
                        break;
                    case "extrathird":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Log.Debug("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 2);
                        break;
                    case "extrafourth":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Log.Debug("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 3);
                        break;
                    default:
                        Log.Error("Family is not set for " + item.skillName + ", " + item.survivorName + "!");
                        break;
                }


                DrifterSkills.MatchConfig_DrifterExtension(survivorDef.bodyPrefab, skillIdx);
            }
        }

        private static void ApplySkillFamilyResize(SkillFamily skillFamily, SkillDef skillDef) {
            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, isFolder: false)
            };
        }

        private void Awake() {
            Log.Init(Logger);
            DrifterSkills.Awake();
            ParseConfig();
        }

        [SystemInitializer(new Type[] { typeof(SkillCatalog), typeof(SurvivorCatalog) })]
        public static void Init() {
            DrifterSkills.Init();
            MatchConfig();
            PrintDebug();
        }

        public static void AddSoundbank(SurvivorDef survivorOrigin, SurvivorDef survivorTarget) {
            try {
                GameObject origin = survivorOrigin.bodyPrefab;
                GameObject target = survivorTarget.bodyPrefab;

                AkBank fsBank = origin.GetComponent<AkBank>();
                CopyAkBank(fsBank, target);
            } catch {
                Log.Debug($"NewGenSkillSwapper - Failed to copy AKBank from {survivorOrigin.cachedName} to {survivorTarget.cachedName}");
            }
        }

        public static void CopyAkBank(AkBank original, GameObject destination) {
            var type = original.GetType();
            var copy = destination.AddComponent(type);
            var fields = type.GetFields();
            foreach (var field in fields) field.SetValue(copy, field.GetValue(original));
        }
    }
}
