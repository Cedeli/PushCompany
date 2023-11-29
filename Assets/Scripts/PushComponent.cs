using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace PushCompany.Assets.Scripts
{
    public class PushComponent : NetworkBehaviour
    {
        [ServerRpc(RequireOwnership = false)]
        public void PushServerRpc(ulong playerId)
        {
            const float PushRange = 3.0f;
            const float PushDistance = 75.0f;
            const string PlayerTag = "Player";

            GameObject playerObject = GetPlayerById(playerId);
            PlayerControllerB player = playerObject.GetComponent<PlayerControllerB>();
            Camera playerCamera = player.gameplayCamera;

            if (player.quickMenuManager.isMenuOpen)
            {
                return;
            }
            if (player.inSpecialInteractAnimation)
            {
                return;
            }
            if (player.isTypingChat)
            {
                return;
            }
            if (player.isMovementHindered > 0 && !player.isUnderwater)
            {
                return;
            }
            if (player.isExhausted)
            {
                return;
            }

            int playerLayerMask = 1 << playerObject.layer;

            Vector3 pushDirection = playerCamera.transform.forward.normalized;
            RaycastHit[] pushRay = Physics.RaycastAll(playerCamera.transform.position, pushDirection, PushRange, playerLayerMask);

            foreach (RaycastHit hit in pushRay)
            {
                if (hit.transform.CompareTag(PlayerTag) && hit.transform.gameObject != playerObject)
                {
                    PlayerControllerB hitPlayer = hit.transform.GetComponent<PlayerControllerB>();
                    PushClientRpc(player.NetworkObjectId, hitPlayer.NetworkObjectId, pushDirection * PushDistance * Time.deltaTime);

                    break;
                }
            }
        }

        [ClientRpc]
        void PushClientRpc(ulong pusherId, ulong playerId, Vector3 push)
        {
            GameObject playerObject = GetPlayerById(playerId);
            PlayerControllerB player = playerObject.GetComponent<PlayerControllerB>();

            player.thisController.Move(push);

            player.movementAudio.pitch = Random.Range(0.5f, 0.75f);
            player.movementAudio.PlayOneShot(StartOfRound.Instance.playerJumpSFX);
            player.movementAudio.pitch = Random.Range(0.93f, 1.07f);

            GameObject pusherObject = GetPlayerById(pusherId);
            PlayerControllerB pusher = pusherObject.GetComponent<PlayerControllerB>();
            pusher.sprintMeter = Mathf.Clamp(pusher.sprintMeter - 0.08f, 0f, 1f);
        }

        private static GameObject GetPlayerById(ulong playerId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject networkObject))
            {
                return networkObject.gameObject;
            }

            return null;
        }
    }
}
