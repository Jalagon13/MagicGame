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
    
    private BaseState _currentSuperState;
    private BaseState _currentSubState;

    public override void OnNetworkSpawn()
    {
        if (!IsClient)
        {
            return;
        }
        
        _serverCharacter.SuperAIState.OnValueChanged += OnSuperAIStateChanged;
        _serverCharacter.SubAIState.OnValueChanged += OnSubAIStateChanged;

        if(!_serverCharacter.Data.IsNpc && _serverCharacter.IsOwner)
        {
            if(_serverCharacter.TryGetComponent(out Player player))
            {
                player.OnNetworkSpawnInitializations();
            }
        }
    }
    
    public override void OnNetworkDespawn()
    {
        if (!IsClient)
        {
            return;
        }
        
        _serverCharacter.SuperAIState.OnValueChanged -= OnSuperAIStateChanged;
        _serverCharacter.SubAIState.OnValueChanged -= OnSubAIStateChanged;
    }
    
    private void Update()
    {
        if (!IsClient)
        {
            return;
        }
        
        _currentSuperState?.ClientUpdateState();
        _currentSubState?.ClientUpdateState();
    }

    private void OnSuperAIStateChanged(AIState previousValue, AIState newValue)
    {
        // Take the previousValue and run the exit function, take the new value, and run the enter function somehow
        Debug.Log($"{_serverCharacter.StateMachine == null} {OwnerClientId}");
        BaseState previousSuperState = _serverCharacter.StateMachine.GetState(previousValue);
        previousSuperState?.ClientExitState();

        _currentSuperState = _serverCharacter.StateMachine.GetState(newValue);
        _currentSuperState?.ClientEnterState();
    }

    private void OnSubAIStateChanged(AIState previousValue, AIState newValue)
    {
        BaseState previousSubState = _serverCharacter.StateMachine.GetState(previousValue);
        previousSubState?.ClientExitState();

        _currentSubState = _serverCharacter.StateMachine.GetState(newValue);
        _currentSubState?.ClientEnterState();
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void PlayDamageNumbersRpc(int damage)
    {
        GameManager.Instance.PlayDamageNumbers(damage, transform.position, _serverCharacter.NpcVisibility.NpcBiomeType, Color.red);
    }
}
