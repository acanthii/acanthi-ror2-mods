using System;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using RoR2.ExpansionManagement;
using System.Linq;
using UnityEngine.Networking;
using RiskOfOptions;
using RiskOfOptions.Options;
using RiskOfOptions.OptionConfigs;
using R2API.Networking;
using R2API.Networking.Interfaces;
using BepInEx.Logging;

namespace ChefBazaar
{
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(NetworkingAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions")]
    // This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class ChefBazaar : BaseUnityPlugin
    {

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "ChefBazaar";
        public const string PluginVersion = "1.4.1";

        public static List<String> chefPhrases;
        public static List<String> chefEscapePhrases;

        public static ConfigEntry<float> chefChance;
        public static ConfigEntry<bool> spawnScrapper;
        public static ConfigEntry<int> maxUsedTimes;
        public static ConfigEntry<bool> moonChef;
        public static ConfigEntry<bool> moonHooks;
        public static ConfigEntry<bool> classicChef;
        public static ConfigEntry<bool> guaranteedAfterSolusEvent;
        public static ConfigEntry<bool> allowedAfterSolusEvent;

        public static ExpansionDef expansionNeeded;
        public static bool isMoonFuckening = false;
        public static bool isChefInBazaar = false;

        public static Dictionary<PlayerCharacterMasterController, int> usedTimes = new Dictionary<PlayerCharacterMasterController, int>();

        public void Awake()
        {
            Log.Init(Logger);

            InitConfig();
            SpawnTools.CreateTablePrefab();

            NetworkingAPI.RegisterMessageType<NetMessages.RequestChefMessage>();
            NetworkingAPI.RegisterMessageType<NetMessages.SpawnChefTableMessage>();

            LanguageAPI.Add("CHEFBAZAAR_IAMHERE_1", "I come and I go, but today I am here.");
            LanguageAPI.Add("CHEFBAZAAR_IAMHERE_2", "Come, mon ami, we shall cook!");
            LanguageAPI.Add("CHEFBAZAAR_IAMHERE_3", "Tired, mon ami? Surely a meal will help.");
            LanguageAPI.Add("CHEFBAZAAR_IAMHERE_4", "It is okay to take a break every once in a while. Come now.");
            LanguageAPI.Add("CHEFBAZAAR_IAMHERE_5", "Mon Chéri! How wonderful it is to see you here.");

            chefPhrases = ["CHEFBAZAAR_IAMHERE_1", "CHEFBAZAAR_IAMHERE_2", "CHEFBAZAAR_IAMHERE_3", "CHEFBAZAAR_IAMHERE_4", "CHEFBAZAAR_IAMHERE_5"];
            chefEscapePhrases = ["MEALPREP_DIALOGUE_MOONDETONATION_1", "MEALPREP_DIALOGUE_MOONDETONATION_2", "MEALPREP_DIALOGUE_MOONDETONATION_3"];

            if (spawnScrapper.Value) 
            {
                LanguageAPI.Add("CHEFBAZAAR_IAMHERE_SCRAPPER", "Please, do not use the scrapper as a garbage bin.");
                chefPhrases.Add("CHEFBAZAAR_IAMHERE_SCRAPPER");
            }

            expansionNeeded = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC3/DLC3.asset").WaitForCompletion();

            On.RoR2.PickupPickerController.OnInteractionBegin += Hooks.PickupPickerController_OnInteractionBegin;
            On.RoR2.MealPrepController.BeginCookingServer += Hooks.MealPrepController_BeginCookingServer;
            Stage.onStageStartGlobal += Hooks.Stage_onStageStartGlobal;
        }

        private void InitConfig()
        {
            chefChance = Config.Bind(
                "ChefBazaar",
                "Wandering CHEF Chance",
                35f,
                new ConfigDescription(
                    "How likely is the Wandering CHEF to appear in the Bazaar?",
                    new AcceptableValueRange<float>(0, 100)
                )
            );

            guaranteedAfterSolusEvent = Config.Bind(
                "ChefBazaar",
                "Guaranteed After Solus Event",
                false,
                "Guarantees Wandering CHEF spawns in the Bazaar after the Solus Event."
            );

            allowedAfterSolusEvent = Config.Bind(
                "ChefBazaar",
                "Only Allowed After Solus Event",
                false,
                "Only allows Wandering CHEF spawns in the Bazaar or moon after the Solus Event."
            );

            spawnScrapper = Config.Bind(
                "ChefBazaar",
                "Spawn Scrapper",
                false,
                "Spawn a Scrapper next to the Wandering CHEF?"
            );

            maxUsedTimes = Config.Bind(
                "ChefBazaar",
                "Crafting Limit",
                0,
                "How many times can the Wandering CHEF cook for you in the Bazaar? (0 = Infinite)"
            );

            moonChef = Config.Bind(
                "Extras",
                "Moon CHEF",
                false,
                "Re-enables the unused Wandering CHEF on Commencement. Guaranteed."
            );

            moonHooks = Config.Bind(
                "Extras",
                "Moon Hooks",
                false,
                "Enable the Wandering CHEF shooing you away during moon detonation for mods that add the Wandering CHEF to the moon."
            );

            classicChef = Config.Bind(
                "Extras",
                "Classic Location",
                false,
                "Re-enables the pre-rework location for the Wandering CHEF! (Next to the lunar item table.)"
            );

            ModSettingsManager.SetModDescription("Chance for the Wandering CHEF to appear in the Bazaar!");
            ModSettingsManager.AddOption(new StepSliderOption(chefChance, new StepSliderConfig() { min = 0, max = 100, increment = 0.5f }));
            ModSettingsManager.AddOption(new CheckBoxOption(guaranteedAfterSolusEvent));
            ModSettingsManager.AddOption(new CheckBoxOption(allowedAfterSolusEvent));
            ModSettingsManager.AddOption(new CheckBoxOption(spawnScrapper));
            ModSettingsManager.AddOption(new IntSliderOption(maxUsedTimes, new IntSliderConfig() { min = 0, max = 100}));
            ModSettingsManager.AddOption(new CheckBoxOption(moonChef));
            ModSettingsManager.AddOption(new CheckBoxOption(moonHooks));
            ModSettingsManager.AddOption(new CheckBoxOption(classicChef));
        }
    }
}
