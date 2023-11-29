using HarmonyLib;
using PushCompany;
using Unity.Netcode;
using UnityEngine;

[HarmonyPatch]
public class NetworkHandler
{
    private static GameObject pushObject;

    #region Host
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MenuManager), "StartHosting")]
    static void CacheHostNetworkHandler(ref GameNetworkManager __instance)
    {
        NetworkManager.Singleton.AddNetworkPrefab(PushCompanyBase.pushPrefab);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MenuManager), "StartHosting")]
    static void SpawnNetworkHandler(ref GameNetworkManager __instance)
    {
        pushObject = Object.Instantiate(PushCompanyBase.pushPrefab);

        try
        {
            pushObject.GetComponent<NetworkObject>().Spawn();
        }
        catch
        {
            PushCompanyBase.Instance.mls.LogError("Failed to instantiate network prefab!");
        }
    }
    #endregion Host

    #region Client
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), "StartClient")]
    static void CacheClientNetworkHandler(ref GameNetworkManager __instance)
    {
        NetworkManager.Singleton.AddNetworkPrefab(PushCompanyBase.pushPrefab);
    }
    #endregion Client
}
