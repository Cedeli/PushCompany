using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace PushCompany.Assets.Scripts
{
    [HarmonyPatch]
    public class NetworkHandler
    {
        private static GameObject pushObject;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameNetworkManager), "Start")]
        static void Init()
        {
            NetworkManager.Singleton.AddNetworkPrefab(PushCompanyBase.pushPrefab);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void SpawnNetworkPrefab()
        {
            try
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    pushObject = Object.Instantiate(PushCompanyBase.pushPrefab);
                    pushObject.GetComponent<NetworkObject>().Spawn(true);
                }
            }
            catch
            {
                PushCompanyBase.Instance.Value.mls.LogError("Failed to instantiate network prefab!");
            }
        }
    }
}