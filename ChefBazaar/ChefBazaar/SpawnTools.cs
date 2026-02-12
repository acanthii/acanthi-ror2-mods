using System;
using System.Collections.Generic;
using System.Text;
using R2API;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ChefBazaar
{
    public class SpawnTools
    {
        public static GameObject oldTablePrefab;

        #region VanillaTable
        public static void SpawnChefMealPrep(Vector3 location, Quaternion rotation) {
            GameObject chefPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/MealPrep/MealPrep.prefab").WaitForCompletion();
            var chef = UnityEngine.Object.Instantiate(chefPrefab, location, rotation);
            NetworkServer.Spawn(chef);
        }
        
        public static void SpawnChefMealPrepOld(Vector3 location, Quaternion rotation) {
            GameObject chefPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC3/MealPrep/MealPrep.prefab").WaitForCompletion();
            var chef = UnityEngine.Object.Instantiate(chefPrefab, location, rotation);
            NetworkServer.Spawn(chef);

            var table = UnityEngine.Object.Instantiate(oldTablePrefab);
            NetworkServer.Spawn(table);
        }

        public static void SpawnScrapper(Vector3 location, Quaternion rotation) {
            GameObject scrapperPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Scrapper/Scrapper.prefab").WaitForCompletion();
            var scrapper = UnityEngine.Object.Instantiate(scrapperPrefab, location, rotation);
            NetworkServer.Spawn(scrapper);
        }

        /// <summary>
        /// Calls EnableVanillaTable with null as it's newLocation, cleaner code! (kinda,..)
        /// </summary>
        public static void EnableVanillaTable() {
            EnableVanillaTable(null);
        }

        /// <summary>
        /// Finds the Vanilla Table in the scene and enables it. newLocation can be used to set the position of the table after enabling, or can be null.
        /// </summary>
        /// <param name="newLocation"></param>
        public static void EnableVanillaTable(Vector3? newLocation) {
            GameObject chefObject = null;
            GameObject[] sceneGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (GameObject item in sceneGameObjects)
            {
                if (item.name == "HOLDER: Mealprep" && item.activeSelf == false)
                {
                    chefObject = item;
                    break;
                }
            }

            if (!chefObject || chefObject == null) {
                Log.Warning("EnableVanillaTable - CHEF Platform could not be found, or already is enabled...");
                return; 
            } 

            chefObject.gameObject.SetActive(true);
            chefObject.transform.Find("MealPrep").gameObject.SetActive(false);

            if (newLocation != null)
            {
                chefObject.gameObject.transform.position = (Vector3)newLocation;
            }
        }
        #endregion

        #region OldTable
        public static void CreateTablePrefab()
        {
            oldTablePrefab = PrefabAPI.CreateEmptyPrefab("tablePrefab");

            oldTablePrefab.AddComponent<NetworkIdentity>();

            GameObject chefTablePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/Bazaar_LunarTable.prefab").WaitForCompletion();
            GameObject lightPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/Bazaar_HangingLight.prefab").WaitForCompletion();
            GameObject crystalLightPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/bazaar/Bazaar_CrystalLight.prefab").WaitForCompletion();
            GameObject wokDisplayPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/lemuriantemple/Assets/LTWok.prefab").WaitForCompletion();

            GameObject table = UnityEngine.Object.Instantiate(chefTablePrefab, oldTablePrefab.transform);
            table.transform.localPosition = new Vector3(-97.28f, -25.3f, -48.88f);
            table.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);
            table.transform.localScale = Vector3.one;

            GameObject light = UnityEngine.Object.Instantiate(lightPrefab, oldTablePrefab.transform);
            light.transform.localPosition = new Vector3(-97.3327f, -4.507f, -48.562f);
            light.transform.localRotation = Quaternion.identity;

            GameObject light2 = UnityEngine.Object.Instantiate(crystalLightPrefab, oldTablePrefab.transform);
            light2.transform.localPosition = new Vector3(-95.1891f, -24.0915f, -48.829f);
            light2.transform.localRotation = Quaternion.Euler(270f, 0f, 0f);
            light2.transform.localScale = Vector3.one;

            GameObject wok = UnityEngine.Object.Instantiate(wokDisplayPrefab, oldTablePrefab.transform);
            wok.transform.localPosition = new Vector3(-99.0918f, -23.18367f, -47.74821f);
            wok.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            PrefabAPI.RegisterNetworkPrefab(oldTablePrefab);
        }
        #endregion
    }
}
