using BepInEx;
using BepInEx.Configuration;
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

        public static ConfigEntry<float>
            config_PushCooldown,
            config_PushForce,
            config_PushRange,
            config_PushCost;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);

            ConfigSetup();
            LoadBundle();

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

        private void ConfigSetup()
        {
            config_PushCooldown = Config.Bind("Push Cooldown", "Value", 0.025f, "How long until the player can push again");
            config_PushForce = Config.Bind("Push Force", "Value", 12.5f, "How strong the player pushes.");
            config_PushRange = Config.Bind("Push Range", "Value", 3.0f, "The distance the player is able to push.");
            config_PushCost = Config.Bind("Push Cost", "Value", 0.08f, "The energy cost of each push.");
        }

        private void LoadBundle()
        {
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
        }
    }
}