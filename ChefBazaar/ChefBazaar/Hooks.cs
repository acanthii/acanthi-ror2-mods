using System;
using System.Collections.Generic;
using System.Text;
using R2API.Networking;
using RoR2;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using R2API.Networking.Interfaces;

namespace ChefBazaar
{
    public class Hooks
    {
        #region ChefHooks
        /// <summary>
        /// When the CHEF begins cooking, increment that player's used counter. (Useful for limiting crafting later.)
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="activator"></param>
        /// <param name="itemsToTake"></param>
        /// <param name="reward"></param>
        /// <param name="count"></param>
        public static void MealPrepController_BeginCookingServer(On.RoR2.MealPrepController.orig_BeginCookingServer orig, MealPrepController self, Interactor activator, PickupIndex[] itemsToTake, PickupIndex reward, int count)
        {
            var user = activator.GetComponent<CharacterBody>().master.GetComponent<PlayerCharacterMasterController>();
            ChefBazaar.usedTimes[user] = ChefBazaar.usedTimes.GetValueOrDefault(user) + 1;
            orig(self, activator, itemsToTake, reward, count);
        }

        /// <summary>
        /// Denies interaction for the user if either the moon is detonating, or you've used your allotted maximum crafting amount. See MealPrepController_BeginCookingServer for more info on maximum crafting amount.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        /// <param name="activator"></param>
        public static void PickupPickerController_OnInteractionBegin(On.RoR2.PickupPickerController.orig_OnInteractionBegin orig, PickupPickerController self, Interactor activator)
        {
            if (!self.GetComponent<MealPrepController>()) { orig(self, activator); return; } // Runs regularly if this isn't a Wandering CHEF (or at least a MealPrepController...)

            // Case where the moon is detonating...
            if (ChefBazaar.isMoonFuckening)
            {
                Chat.SendBroadcastChat(new Chat.NpcChatMessage
                {
                    formatStringToken = "MEALPREP_DIALOGUE_FORMAT",
                    baseToken = ChefBazaar.chefEscapePhrases[UnityEngine.Random.Range(0, ChefBazaar.chefEscapePhrases.Count)],
                    sender = null
                });
                return;
            }
            //

            if (SceneManager.GetActiveScene().name != "bazaar") { orig(self, activator); return; } // Runs regularly if this isn't the Bazaar...

            // Case where CHEF has had ENOUGH...
            var user = activator.GetComponent<CharacterBody>().master.GetComponent<PlayerCharacterMasterController>();
            if (user && ChefBazaar.usedTimes.GetValueOrDefault(user) >= ChefBazaar.maxUsedTimes.Value && ChefBazaar.maxUsedTimes.Value > 0)
            {
                Chat.SendBroadcastChat(new Chat.NpcChatMessage
                {
                    formatStringToken = "MEALPREP_DIALOGUE_FORMAT",
                    baseToken = ChefBazaar.chefEscapePhrases[UnityEngine.Random.Range(0, ChefBazaar.chefEscapePhrases.Count)],
                    sender = null
                });
                return;
            }

            orig(self, activator);
        }
        #endregion

        #region WorldSetupHooks
        public static void Stage_onStageStartGlobal(Stage stage) {
            // Runs server-side CHEF-spawning logic.
            if (NetworkServer.active)
            {
                Log.Warning("HERE! 1");
                ChefBazaar.isChefInBazaar = false;

                if (!Run.instance.IsExpansionEnabled(ChefBazaar.expansionNeeded)) { return; }
                if (!Run.instance.GetEventFlag("SolusHeartBeaten") && ChefBazaar.allowedAfterSolusEvent.Value) { return; }

                switch (SceneManager.GetActiveScene().name)
                {
                    case "moon2":
                    {
                        MoonHooksInit();
                        SpawnChefMoonServer();
                        break;
                    }
                    case "bazaar":
                    {
                        SpawnChefBazaarServer();
                        Log.Warning("HERE! 2");
                        break;
                    }
                    default:
                    {
                        return;
                    }
                }
            }
            // Polls the NetworkServer for CHEF status.
            else
            {
                Log.Debug("Client :: " + DateTime.Now.ToString() + " " + DateTime.Now.Millisecond.ToString());
                NetworkUser user = LocalUserManager.GetFirstLocalUser()?.currentNetworkUser;
                new NetMessages.RequestChefMessage(user.netIdentity).Send(NetworkDestination.Server);
            }
        }

        public static void MoonHooksInit()
        {
            if (!ChefBazaar.moonHooks.Value) { return; }
            var escapeController = GameObject.Find("EscapeSequenceController").GetComponent<EscapeSequenceController>();
            if (escapeController != null)
            {
                escapeController.onEnterMainEscapeSequence.AddListener(delegate
                {
                    ChefBazaar.isMoonFuckening = true;
                });
                escapeController.onFailEscapeSequenceServer.AddListener(delegate
                {
                    ChefBazaar.isMoonFuckening = false;
                });
                escapeController.onCompleteEscapeSequenceServer.AddListener(delegate
                {
                    ChefBazaar.isMoonFuckening = false;
                });
            }
        }

        public static void SpawnChefBazaarServer() {
            if (UnityEngine.Random.Range(0f, 100.0f) >= ChefBazaar.chefChance.Value && !(Run.instance.GetEventFlag("SolusHeartBeaten") && ChefBazaar.guaranteedAfterSolusEvent.Value)) { return; }

            ChefBazaar.isChefInBazaar = true;


            if (!ChefBazaar.classicChef.Value)
            {
                SpawnTools.SpawnChefMealPrep(new Vector3(-95.7066f, -24.9729f, 20.9845f), Quaternion.Euler(0f, 350.4501f, 0f));
                if (ChefBazaar.spawnScrapper.Value)
                {
                    SpawnTools.SpawnScrapper(new Vector3(-94.3094f, -25.7118f, 25.2005f), Quaternion.Euler(2.21f, 1.35f, 0f));
                }
            }
            else {
                SpawnTools.SpawnChefMealPrepOld(new Vector3(-97.3184f, -24.2f, -49.48f), Quaternion.Euler(0f, 270f, 0f));
                if (ChefBazaar.spawnScrapper.Value)
                {
                    SpawnTools.SpawnScrapper(new Vector3(-93.5723f, -25.8374f, -47.0965f), Quaternion.Euler(2.21f, 1.35f, 345.91f));
                }
            }


            Chat.SendBroadcastChat(new Chat.NpcChatMessage
            {
                formatStringToken = "MEALPREP_DIALOGUE_FORMAT",
                baseToken = ChefBazaar.chefPhrases[UnityEngine.Random.Range(0, ChefBazaar.chefPhrases.Count)],
                sender = null
            });

            ChefBazaar.usedTimes.Clear();

            if (!ChefBazaar.classicChef.Value)
            {
                SpawnTools.EnableVanillaTable(new Vector3(-82.2492f, -47.2163f, 14.0186f));
            }
            //new NetMessages.SpawnChefTableMessage().Send(NetworkDestination.Clients);
        }

        public static void SpawnChefMoonServer()
        {
            if (!ChefBazaar.moonChef.Value) { return; }

            SpawnTools.SpawnChefMealPrep(new Vector3(-259.18f, -220.2f, -402.91f), Quaternion.Euler(0f, 120.9532f, 0f));
            if (ChefBazaar.spawnScrapper.Value)
            {
                SpawnTools.SpawnScrapper(new Vector3(-257.9419f, -221.2121f, -407.045f), Quaternion.Euler(359.7326f, 122.0352f, 346.4449f));
            }

            SpawnTools.EnableVanillaTable();
            //new NetMessages.SpawnChefTableMessage().Send(NetworkDestination.Clients);
        }
        #endregion
    }
}
