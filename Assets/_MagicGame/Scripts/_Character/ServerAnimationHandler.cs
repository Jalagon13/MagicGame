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
    
    private CardinalDirection _swingDirection = CardinalDirection.None;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _networkLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
            _serverCharacter.MovementState.OnValueChanged += PlayCurrentMoveState;
            _serverCharacter.CardinalDirection.OnValueChanged += OnCardinalDirectionChanged;
            if(_serverCharacter.TryGetComponent(out Player player))
            {
                player.PlayerHand.SwingDirection.OnValueChanged += OnSwingDirectionChanged;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && _networkLifeState != null)
        {
            _networkLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
            _serverCharacter.MovementState.OnValueChanged -= PlayCurrentMoveState;
            _serverCharacter.CardinalDirection.OnValueChanged -= OnCardinalDirectionChanged;
            if (_serverCharacter.TryGetComponent(out Player player))
            {
                player.PlayerHand.SwingDirection.OnValueChanged -= OnSwingDirectionChanged;
            }
        }
    }

    private void OnSwingDirectionChanged(CardinalDirection previousValue, CardinalDirection newValue)
    {
        _swingDirection = newValue;

        foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
        {
            handler.PlayAnimation(_serverCharacter.MovementState.Value, _swingDirection == CardinalDirection.None ? _serverCharacter.CardinalDirection.Value : _swingDirection);
        }
    }

    private void OnCardinalDirectionChanged(CardinalDirection previousValue, CardinalDirection newValue)
    {
        foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
        {
            handler.PlayAnimation(_serverCharacter.MovementState.Value, _swingDirection == CardinalDirection.None ? newValue : _swingDirection);
        }
    }

    private void PlayCurrentMoveState(MovementState previousMovementState, MovementState newMovementState)
    {
        CardinalDirection direction = _serverCharacter.CardinalDirection.Value;

        foreach (ServerSpriteAnimHandler handler in _spriteAnimHandlers)
        {
            handler.PlayAnimation(newMovementState, _swingDirection == CardinalDirection.None ? direction : _swingDirection);
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
