using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using PushCompany;
using PushCompany.Assets.Scripts;
using System.Collections;

[HarmonyPatch(typeof(PlayerControllerB))]
public static class PlayerControllerB_Patches
{
    private static PushComponent pushComponent;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    static void Update(PlayerControllerB __instance)
    {
        if (__instance.IsOwner)
        {
            if (pushComponent == null)
            {
                pushComponent = GameObject.FindObjectOfType<PushComponent>();
            }

            if (__instance.playerActions.Movement.Interact.WasPressedThisFrame())
            {
                if (pushComponent != null)
                {
                    pushComponent.PushServerRpc(__instance.NetworkObjectId);
                }
            }
        }
    }
}
