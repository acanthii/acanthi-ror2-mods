using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EntityStates.BrotherMonster.Weapon;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using static RoR2.SkillLocator;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace NewGenSkillSwapper
{
    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class NewGenSkillSwapper : BaseUnityPlugin
    {
        public class SkillSwapConfig
        {
            public string skillName;

            public string survivorName;

            public string family;
        }

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "RailgunnerTinker";
        public const string PluginVersion = "1.0.0";

        public static List<SkillSwapConfig> skillSwapConfig = new List<SkillSwapConfig>();

        public static ConfigEntry<string> skillsToSwap;

        public static ConfigEntry<bool> debugMode;

        public void ParseConfig()
        {
            skillsToSwap = Config.Bind<string>("RailgunnerTinker", "skillsToSwap", "(Tinker,Railgunner,special)", "This should NOT be touched if you don't know what you're doing. Theoretically you could use this to add Tinker to more survivors but it's not a supported feature. Here be Dragons! A list of skills to swap. Should be formatted like this: (skillDefName,survivorDefName,family),(otherSkillDefName,otherSurvivorDefName,otherFamily)");
            debugMode = Config.Bind<bool>("RailgunnerTinker", "debugMode", false, "Prints all names for skills and survivors.");
            Debug.Log("Parsing Config...");
            string[] array = skillsToSwap.Value.Trim().Split("),(");
            string[] array2 = array;
            foreach (string item in array2)
            {
                Debug.Log(item);
                string itemTrimmed = item.Trim('(', ')');
                string[] pieces = itemTrimmed.Split(',');
                skillSwapConfig.Add(new SkillSwapConfig
                {
                    skillName = pieces[0],
                    survivorName = pieces[1],
                    family = pieces[2]
                });
            }
        }

        public static void PrintDebug()
        {
            if (debugMode.Value)
            {
                Debug.Log("Printing skill names... Disable debugMode in config to disable this!");
                foreach (SkillDef item in SkillCatalog.allSkillDefs)
                {
                    Debug.Log(item.skillName + " || Token (for reference, dont use!): " + item.skillNameToken);
                }
                Debug.Log("Printing survivor names... Disable debugMode in config to disable this!");
                {
                    foreach (SurvivorDef item2 in SurvivorCatalog.allSurvivorDefs)
                    {
                        Debug.Log(item2.cachedName);
                    }
                    return;
                }
            }
            Debug.Log("Skipping logs...");
        }

        public static void MatchConfig()
        {
            foreach (SkillSwapConfig item in skillSwapConfig)
            {
                int skillIdx = SkillCatalog.FindSkillIndexByName(item.skillName);
                if (skillIdx == -1)
                {
                    Debug.Log("Couldn't find skill. Skipping...");
                    continue;
                }
                SkillDef skillDef = SkillCatalog.GetSkillDef(skillIdx);
                SurvivorDef survivorDef = SurvivorCatalog.FindSurvivorDef(item.survivorName);
                if (!skillDef || !survivorDef)
                {
                    Debug.Log("Couldn't add skill. Skipping...");
                    continue;
                }
                SkillLocator skillLocator = survivorDef.bodyPrefab.GetComponent<SkillLocator>();
                if (!(UnityEngine.Object)(object)skillLocator)
                {
                    Debug.Log("No SkillLocator on " + survivorDef.cachedName + "!");
                    continue;
                }
                SkillFamily skillFamily;
                switch (item.family.ToLower())
                {
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
                        foreach (GenericSkill skill in survivorDef.bodyPrefab.GetComponentsInChildren<GenericSkill>())
                        {
                            if ((skill._skillFamily as ScriptableObject).name.Contains("Passive"))
                            {
                                SkillFamily family = skill._skillFamily;
                                ApplySkillFamilyResize(family, skillDef);
                            }
                        }
                        break;
                    case "extrafirst":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Debug.Log("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 0);
                        break;
                    case "extrasecond":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Debug.Log("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 1);
                        break;
                    case "extrathird":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Debug.Log("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 2);
                        break;
                    case "extrafourth":
                        if (!ExtraSkillSlotsCompat.IsEnabled()) {
                            Debug.Log("ExtraSkillSlots entry detected, but EntrySkillSlots not installed or unsupported?");
                            break;
                        }
                        //ExtraSkillSlotsCompat.MatchConfig_ESSCompat(survivorDef.bodyPrefab, skillDef, 3);
                        break;
                    default:
                        Debug.LogError("Family is not set for " + item.skillName + ", " + item.survivorName + "!");
                        break;
                }
                

                DrifterSkills.MatchConfig_DrifterExtension(survivorDef.bodyPrefab);
            }
        }

        private static void ApplySkillFamilyResize(SkillFamily skillFamily, SkillDef skillDef) {
            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, isFolder: false)
            };
        }

        private void Awake()
        {
            ParseConfig();
            DrifterSkills.Init();
        }

        [SystemInitializer(new Type[]{typeof(SkillCatalog), typeof(SurvivorCatalog)})]
        public static void Init()
        {
            PrintDebug();
            MatchConfig();
        }
    }
}
