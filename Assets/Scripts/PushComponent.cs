using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace PushCompany.Assets.Scripts
{
    public class PushComponent : NetworkBehaviour
    {
        private NetworkVariable<float> PushRange = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> PushDistance = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> PushCost = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                PushRange.Value = PushCompanyBase.config_PushRange.Value;
                PushDistance.Value = PushCompanyBase.config_PushDistance.Value;
                PushCost.Value = PushCompanyBase.config_PushCost.Value;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PushServerRpc(ulong playerId)
        {
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
            RaycastHit[] pushRay = Physics.RaycastAll(playerCamera.transform.position, pushDirection, PushRange.Value, playerLayerMask);

            foreach (RaycastHit hit in pushRay)
            {
                if (hit.transform.gameObject != playerObject)
                {
                    PlayerControllerB hitPlayer = hit.transform.GetComponent<PlayerControllerB>();
                    PushClientRpc(player.NetworkObjectId, hitPlayer.NetworkObjectId, pushDirection * PushDistance.Value * Time.deltaTime);

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
            pusher.sprintMeter = Mathf.Clamp(pusher.sprintMeter - PushCost.Value, 0f, 1f);
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
