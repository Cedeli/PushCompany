using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace PushCompany.Assets.Scripts
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public static class PlayerControllerB_Patches
    {
        private static PushComponent pushComponent;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        static void Update(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner) return;
            if (!__instance.playerActions.Movement.Interact.WasPressedThisFrame()) return;

            if (pushComponent == null)
            {
                pushComponent = GameObject.FindObjectOfType<PushComponent>();
            }

            if (pushComponent != null)
            {
                pushComponent.PushServerRpc(__instance.NetworkObjectId);
            }
        }
    }
}