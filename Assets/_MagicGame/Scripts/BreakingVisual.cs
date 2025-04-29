using System;
using Unity.Netcode;
using UnityEngine;

public class BreakingVisual : NetworkBehaviour
{
    [field: SerializeField] public AnimationClip BreakingClip { get; private set; }
    [field: SerializeField] public AnimationClip NotBreakingClip { get; private set; }

    private NetworkVariable<bool> _isVisible = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> _totalMiningTime = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<BiomeType> _ownerBiome = new(BiomeType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private Animator _breakingAnimator;
    private SpriteRenderer _breakingSr;
    
    private void Awake()
    {
        _breakingAnimator = transform.GetChild(0).GetComponent<Animator>();
        _breakingSr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _breakingSr.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            MiningHandler.Instance.OnMiningStarted += MiningStarted;
            MiningHandler.Instance.OnMiningStopped += MiningStopped;
        }

        _isVisible.OnValueChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(bool previousValue, bool newValue)
    {
        if(Player.LocalClientInstance.CurrentPlayerBiome.Value == _ownerBiome.Value)
        {
            if (newValue)
            {
                AnimStateManager.ChangeAnimationState(_breakingAnimator, BreakingClip, _totalMiningTime.Value);
                _breakingSr.enabled = true;
            }
            else
            {
                AnimStateManager.ChangeAnimationState(_breakingAnimator, NotBreakingClip);
                _breakingSr.enabled = false;
            }
        }
        else
        {
            AnimStateManager.ChangeAnimationState(_breakingAnimator, NotBreakingClip);
            _breakingSr.enabled = false;
        }
    }

    private void MiningStarted(object sender, MiningHandler.MiningStartedEventArgs e)
    {
        transform.position = new Vector3(e.BreakTargetPosition.x, e.BreakTargetPosition.y, 0f);
        _totalMiningTime.Value = e.TotalMiningTime;
        _ownerBiome.Value = e.Biome;
        _isVisible.Value = true;
    }

    private void MiningStopped(object sender, EventArgs e)
    {
        _isVisible.Value = false;
    }

    public override void OnNetworkDespawn()
    {
        if(IsOwner)
        {
            MiningHandler.Instance.OnMiningStarted -= MiningStarted;
            MiningHandler.Instance.OnMiningStopped -= MiningStopped;
        }
    }
}
