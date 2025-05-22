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
    private int _sortingOrder;
    
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
                _breakingSr.sortingOrder = _sortingOrder;
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
        _sortingOrder = 0;

        if (e.DestructableType == DestructableType.Tile)
        {
            if(TileManager.Instance.HasTile(e.BreakTargetPosition + Vector3Int.down, TileType.Wall, out TileSO belowWallTile))
            {
                // There is a tile below the tile we are breaking
                int tileIdOfTileBeingBroken = GameManager.Instance.GetTileIdFromTileSO(TileManager.Instance.WallTm.GetTile<TileSO>(e.BreakTargetPosition));
                int tileIdOfTileBelow = GameManager.Instance.GetTileIdFromTileSO(belowWallTile);
                
                if(tileIdOfTileBeingBroken == tileIdOfTileBelow) // Same tile
                {
                    transform.position = new Vector3(e.BreakTargetPosition.x, e.BreakTargetPosition.y + 0.5f, 0f);
                    _sortingOrder = 1;
                    // Animation 1 tall
                }
                else // Different tile
                {
                    transform.position = new Vector3(e.BreakTargetPosition.x, e.BreakTargetPosition.y, 0f);
                    // Animation 1.5 tall
                }
            }
            else
            {
                // There is no tile below the tile we are breaking
                transform.position = new Vector3(e.BreakTargetPosition.x, e.BreakTargetPosition.y, 0f);
                // Animation 1.5 tall
            }
        }
        else
        {
            transform.position = new Vector3(e.BreakTargetPosition.x, e.BreakTargetPosition.y, 0f);
            // Animation 1 tall
        }

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
