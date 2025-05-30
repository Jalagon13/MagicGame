using Unity.Netcode;
using UnityEngine;

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
        if (!IsClient || transform.parent == null)
        {
            return;
        }

        _clientActionPlayer = new ClientActionPlayer(this);
        
        if(!_serverCharacter.Data.IsNpc && _serverCharacter.IsOwner)
        {
            // local player start up code here, maybe input
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ClientPlayActionRpc(ActionSO action)
    {
        // TODO: Data unpacking
        _clientActionPlayer.PlayAction(action);
    }
}
