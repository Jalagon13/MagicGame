using System;
using Unity.Netcode;
using UnityEngine;

public class BreakingVisual : NetworkBehaviour
{
    [SerializeField]
    private AnimationClip _breakingClip;

    [SerializeField]
    private AnimationClip _notBreakingClip;

    private NetworkVariable<bool> _isVisible = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<float> _totalMiningTime = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<BiomeType> _ownerBiome = new(BiomeType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<ushort> _tileBreakingId = new(GameDataRegistry.INVALID_ID, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private Animator _breakingAnimator;
    private SpriteRenderer _breakingSr;
    private int _sortingOrder;
    private ParticleSystem _hitParticles;
    
    private void Awake()
    {
        _breakingAnimator = transform.GetChild(0).GetComponent<Animator>();
        _breakingSr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        _hitParticles = transform.GetChild(1).GetComponent<ParticleSystem>();
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

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            MiningHandler.Instance.OnMiningStarted -= MiningStarted;
            MiningHandler.Instance.OnMiningStopped -= MiningStopped;
        }

        _isVisible.OnValueChanged -= OnVisibleChanged;
    }

    private void OnVisibleChanged(bool previousValue, bool newValue)
    {
        if(Player.Instance.CurrentBiome.Value == _ownerBiome.Value)
        {
            if (newValue)
            {
                AnimStateManager.ChangeAnimationState(_breakingAnimator, _breakingClip, _totalMiningTime.Value); 
                _breakingSr.sortingOrder = _sortingOrder;
                _breakingSr.enabled = true;

                TileDataSO tileData = GameDataRegistry.Instance.GetTileDataFromTileId(_tileBreakingId.Value);
                if(tileData != null)
                {
                    var tsa = _hitParticles.textureSheetAnimation;
                    tsa.enabled = true;
                    tsa.mode = ParticleSystemAnimationMode.Sprites;
                    for (int i = 0; i < tsa.spriteCount; i++)
                    {
                        tsa.SetSprite(i, tileData.GetRandomMiningParticleSprite());
                    }

                    _hitParticles.Play();
                }
            }
            else
            {
                AnimStateManager.ChangeAnimationState(_breakingAnimator, _notBreakingClip);
                _breakingSr.enabled = false;
                _hitParticles.Stop();
            }
        }
        else
        {
            AnimStateManager.ChangeAnimationState(_breakingAnimator, _notBreakingClip);
            _breakingSr.enabled = false;
            _hitParticles.Stop();
        }
    }

    private void MiningStarted(object sender, MiningHandler.MiningStartedEventArgs e)
    {
        _sortingOrder = 0;
        
        if(e.TileData != null)
        {
            _tileBreakingId.Value = GameDataRegistry.Instance.GetTileIdFromTileData(e.TileData);
        }

        if (e.DestructableType == DestructableType.Tile)
        {
            if(TileManager.Instance.HasTile(e.BreakTargetPosition + Vector3Int.down, TileType.Wall, out TileDataSO belowWallTile))
            {
                // There is a tile below the tile we are breaking
                int tileIdOfTileBeingBroken = GameDataRegistry.Instance.GetTileIdFromTileData(TileManager.Instance.WallTm.GetTile<TileDataSO>(e.BreakTargetPosition));
                int tileIdOfTileBelow = GameDataRegistry.Instance.GetTileIdFromTileData(belowWallTile);

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
                transform.position = new Vector3(e.BreakTargetPosition.x, e.BreakTargetPosition.y + 0.25f, 0f);
                _sortingOrder = 1;
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
}
