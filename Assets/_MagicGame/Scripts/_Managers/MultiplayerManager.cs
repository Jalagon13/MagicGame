using Unity.Netcode;
using UnityEngine;

namespace ProjectWizard
{
    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public Player GetClosestPlayer(Vector2 position, BiomeType biome)
        {
            Player closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    var player = client.PlayerObject?.GetComponent<Player>();
                    if (player != null)
                    {
                        if (player.CurrentBiome.Value != biome) continue;

                        float distance = Vector3.Distance(position, player.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestPlayer = player;
                        }
                    }
                }
            }

            return closestPlayer;
        }
    }
}
