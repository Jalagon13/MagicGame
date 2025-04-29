using System;
using Unity.Netcode;
using UnityEngine;

public class BreakingVisualsHandler : NetworkBehaviour
{
    [field: SerializeField] public BreakingVisual BreakingVisualPrefab { get; private set; }

    private void Awake()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback += SpawnBreakingVisuals;
        }
    }

    private void SpawnBreakingVisuals(ulong clientId)
    {
        if (NetworkManager.LocalClientId != clientId) return;
        
         SpawnBreakingVisualsServerRpc(clientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SpawnBreakingVisualsServerRpc(ulong clientId)
    {
        BreakingVisual breakingVisual = Instantiate(BreakingVisualPrefab, transform.position, Quaternion.identity);
        NetworkObject no = breakingVisual.GetComponent<NetworkObject>();
        no.SpawnWithOwnership(clientId, true);
    }

    public override void OnDestroy()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= SpawnBreakingVisuals;
        }

        base.OnDestroy();
    }
}
