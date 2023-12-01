using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace PushCompany.Assets.Scripts
{
    public class PushComponent : NetworkBehaviour
    {
        private Dictionary<ulong, float> lastPushTimes = new Dictionary<ulong, float>();

        private NetworkVariable<float> PushCooldown = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> PushRange = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> PushForce = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> PushCost = new NetworkVariable<float>(0.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                PushCooldown.Value = PushCompanyBase.config_PushCooldown.Value;
                PushRange.Value = PushCompanyBase.config_PushRange.Value;
                PushForce.Value = PushCompanyBase.config_PushForce.Value;
                PushCost.Value = PushCompanyBase.config_PushCost.Value;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PushServerRpc(ulong playerId)
        {
            if (lastPushTimes.TryGetValue(playerId, out float lastPushTime))
            {
                if (Time.time - lastPushTime < PushCooldown.Value) return;
            }
            else
            {
                lastPushTimes.Add(playerId, 0.0f);
            }

            GameObject playerObject = GetPlayerById(playerId);
            PlayerControllerB player = playerObject.GetComponent<PlayerControllerB>();
            Camera playerCamera = player.gameplayCamera;

            if (!CanPushPlayer(player)) return;

            int playerLayerMask = 1 << playerObject.layer;

            Vector3 pushDirection = playerCamera.transform.forward.normalized;
            RaycastHit[] pushRay = Physics.RaycastAll(playerCamera.transform.position, pushDirection, PushRange.Value, playerLayerMask);

            foreach (RaycastHit hit in pushRay)
            {
                if (hit.transform.gameObject != playerObject)
                {
                    PlayerControllerB hitPlayer = hit.transform.GetComponent<PlayerControllerB>();
                    if (hitPlayer.inSpecialInteractAnimation) return;

                    PushClientRpc(player.NetworkObjectId, hitPlayer.NetworkObjectId, pushDirection * PushForce.Value * Time.fixedDeltaTime);
                    lastPushTimes[playerId] = Time.time;
                    break;
                }
            }
        }

        [ClientRpc]
        void PushClientRpc(ulong pusherId, ulong playerId, Vector3 push)
        {
            GameObject playerObject = GetPlayerById(playerId);
            PlayerControllerB player = playerObject.GetComponent<PlayerControllerB>();

            StartCoroutine(SmoothMove(player.thisController, push));

            player.movementAudio.PlayOneShot(StartOfRound.Instance.playerJumpSFX);

            GameObject pusherObject = GetPlayerById(pusherId);
            PlayerControllerB pusher = pusherObject.GetComponent<PlayerControllerB>();
            pusher.sprintMeter = Mathf.Clamp(pusher.sprintMeter - PushCost.Value, 0.0f, 1.0f);
        }

        public IEnumerator SmoothMove(CharacterController controller, Vector3 push)
        {
            float force = PushForce.Value / 12.5f;
            float smoothTime = push.magnitude / force;

            Vector3 targetPosition = controller.transform.position + push;
            Vector3 direction = (targetPosition - controller.transform.position).normalized;
            float distance = Vector3.Distance(controller.transform.position, targetPosition);

            for (float currentTime = 0; currentTime < smoothTime; currentTime += Time.fixedDeltaTime)
            {
                float currentDistance = distance * Mathf.Min(currentTime, smoothTime) / smoothTime;

                controller.Move(direction * currentDistance);

                yield return null;
            }
        }

        private bool CanPushPlayer(PlayerControllerB player)
        {
            return !player.quickMenuManager.isMenuOpen &&
                   !player.inSpecialInteractAnimation &&
                   !player.isTypingChat &&
                   !player.isExhausted;
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
