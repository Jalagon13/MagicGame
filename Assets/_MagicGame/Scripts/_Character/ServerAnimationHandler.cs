using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ServerAnimationHandler : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;

    [SerializeField]
    private NetworkLifeState _networkLifeState;
    [SerializeField] 
    private List<ServerSpriteAnimHandler> _spriteAnimHandlers = new List<ServerSpriteAnimHandler>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _networkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && _networkLifeState != null)
        {
            _networkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
        }
    }
    
    public void PlayCurrentMoveState()
    {
        MovementState moveState = _serverCharacter.MovementState.Value;
        CardinalDirection direction = _serverCharacter.CardinalDirection.Value;
        // Debug.Log($"Playing {moveState} {direction}");

        foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
        {
            handler.PlayAnimation(moveState, direction);
        }
    }

    private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        // TODO: Later
        switch (newValue)
        {
            case LifeState.Alive:
            
                break;
            case LifeState.IFrame:
            
                break;
            case LifeState.Dead:
            
                break;
        }
    }
}
