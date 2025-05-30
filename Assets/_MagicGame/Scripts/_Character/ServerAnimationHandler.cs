using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ServerAnimationHandler : NetworkBehaviour
{
    [SerializeField] 
    private AnimationConfigSO _animConfig;
    public AnimationConfigSO AnimationConfig => _animConfig;

    [SerializeField] 
    private NetworkAnimator _netcodeAnimator;

    [SerializeField]
    NetworkLifeState _networkLifeState;

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
    
    public void PlayAnimation(AnimationClip clip)
    {
        AnimStateManager.ChangeAnimationState(_netcodeAnimator.Animator, clip);
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
