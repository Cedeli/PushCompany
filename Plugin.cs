using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PushCompany.Assets.Scripts;
using System.Reflection;
using UnityEngine;

namespace PushCompany
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class PushCompanyBase : BaseUnityPlugin
    {
        public static PushCompanyBase Instance;
        public static GameObject pushPrefab;
        public ManualLogSource mls;

        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            AssetBundle pushBundle = AssetBundle.LoadFromMemory(Properties.Resources.pushcompany);
            if (pushBundle == null)
            {
                mls.LogError("Failed to load Push Bundle!");
                return;
            }
            pushPrefab = pushBundle.LoadAsset<GameObject>("Assets/Push.prefab");
            if (pushPrefab == null)
            {
                mls.LogError("Failed to load Push Prefab!");
                return;
            }
            pushPrefab.AddComponent<PushComponent>();

            harmony.PatchAll(typeof(PushCompanyBase));
            harmony.PatchAll(typeof(PlayerControllerB_Patches));
            harmony.PatchAll(typeof(NetworkHandler));

            // Unity Netcode Weaver
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }

            mls.LogInfo($"PushCompany has initialized!");
        }
    }
}