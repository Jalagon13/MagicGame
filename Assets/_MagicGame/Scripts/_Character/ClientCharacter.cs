using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// <see cref="ClientCharacter"/> is responsible for displaying a character on the client's screen based on state information sent by the server.
/// </summary>
public class ClientCharacter : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;

    private ClientActionPlayer _clientActionPlayer;

    public override void OnNetworkSpawn()
    {
        if (!IsClient)
        {
            return;
        }

        _clientActionPlayer = new ClientActionPlayer(this);
        
        if(!_serverCharacter.Data.IsNpc && _serverCharacter.IsOwner)
        {
            if(_serverCharacter.TryGetComponent(out Player player))
            {
                player.OnNetworkSpawnInitializations();
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void PlayDamageNumbersRpc(int damage)
    {
        GameManager.Instance.PlayDamageNumbers(damage, transform.position, _serverCharacter.NpcVisibility.NpcBiomeType, Color.red);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ClientPlayActionRpc(ActionSO action)
    {
        // TODO: Data unpacking
        _clientActionPlayer.PlayAction(action);
    }
}
