using System.Reflection;
using BepInEx;
using R2API;
using RoR2;
using ShaderSwapper;
using UnityEngine;
using UnityEngine.AddressableAssets;
#if DEBUG
using UnityHotReloadNS;
#endif

namespace DegenerateItems
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class DegenerateItems : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "acanthi";
        public const string PluginName = "DegenerateItems";
        public const string PluginVersion = "1.0.0";

        public static AssetBundle DegenerateItemsAssets;

        public void Awake()
        {
            Log.Init(Logger);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DegenerateItems.degenerateitems_assets"))
            {
                DegenerateItemsAssets = AssetBundle.LoadFromStream(stream);
            }

            base.StartCoroutine(DegenerateItemsAssets.UpgradeStubbedShadersAsync());

            BunnyEars.Init();
            HeavenTranmitter.Init();
        }
#if DEBUG
        void Update()
        {
            if (Input.GetKeyUp(KeyCode.F2))
            {
                UnityHotReload.LoadNewAssemblyVersion(
                    typeof(DegenerateItems).Assembly, // The currently loaded assembly to replace.
                    "D:\\GitRepos\\acanthi-ror2-mods\\DegenerateItems\\DegenerateItems\\bin\\Debug\\netstandard2.1\\DegenerateItems.dll"  // The path to the newly compiled DLL.
                );
            }
        }
#endif
    }
}
