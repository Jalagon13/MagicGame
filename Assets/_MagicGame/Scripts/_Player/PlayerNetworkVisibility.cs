using System;
using Unity.Netcode;
using UnityEngine;

namespace ProjectTinker
{
    public class PlayerNetworkVisibility : NetworkBehaviour
    {
        private GameObject _playerGameObject;
        private Collider2D _playerCollider;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _playerGameObject = transform.GetChild(0).gameObject;
                _playerCollider = GetComponent<Collider2D>();

                NetworkObject.CheckObjectVisibility += CheckVisibility;
                NetworkManager.NetworkTickSystem.Tick += HandleOtherPlayerVisibility;
            }
        }

        private bool CheckVisibility(ulong clientId)
        {
            if (!IsSpawned)
            {
                return false;
            }

            if (clientId == OwnerClientId) return true;

            var nonClientIdPlayerObject = NetworkManager.ConnectedClients[clientId].PlayerObject;
            var nonClientIdPlayerBiome = nonClientIdPlayerObject.GetComponent<Player>().CurrentBiome.Value;
            var ownerClientBiome = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<Player>().CurrentBiome.Value;

            if (NetworkManager.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<Player>().ServerCharacter.LifeState == LifeState.Dead)
            {
                return false;
            }

            return ownerClientBiome == nonClientIdPlayerBiome;
        }

        private void HandleOtherPlayerVisibility()
        {
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
            {
                if (clientId == OwnerClientId) continue;

                // Now testing non owner client's ids
                var shouldBeVisible = CheckVisibility(clientId);
                var isVisibile = NetworkObjectVisibleTo(clientId);

                if (shouldBeVisible && !isVisibile)
                {
                    ShowPlayer(clientId);
                }
                else if (!shouldBeVisible && isVisibile)
                {
                    HidePlayer(clientId);
                }
            }
        }

        private void ShowPlayer(ulong playerId)
        {
            if (playerId == NetworkManager.ServerClientId)
            {
                _playerGameObject.SetActive(true);
                _playerCollider.enabled = true;
                _playerCollider.isTrigger = true;
            }
            else
            {
                NetworkObject.NetworkShow(playerId);
            }
        }

        private void HidePlayer(ulong playerId)
        {
            if (playerId == NetworkManager.ServerClientId)
            {
                _playerGameObject.SetActive(false);
                _playerCollider.enabled = false;
                _playerCollider.isTrigger = false;
            }
            else
            {
                NetworkObject.NetworkHide(playerId);
            }
        }

        private bool NetworkObjectVisibleTo(ulong clientId)
        {
            return clientId == NetworkManager.ServerClientId ? _playerGameObject.activeInHierarchy : NetworkObject.IsNetworkVisibleTo(clientId);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkObject.CheckObjectVisibility -= CheckVisibility;
                NetworkManager.NetworkTickSystem.Tick -= HandleOtherPlayerVisibility;
            }

            base.OnNetworkDespawn();
        }
    }
}
