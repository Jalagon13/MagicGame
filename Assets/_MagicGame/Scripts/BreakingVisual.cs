using System;
using Unity.Netcode;
using UnityEngine;


namespace ProjectWizard
{
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
	    private NetworkVariable<ushort> _rscBreakingId = new(GameDataRegistry.INVALID_ID, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	    private Animator _breakingAnimator;
	    private SpriteRenderer _breakingSr;
	    private int _sortingOrder;
	    private ParticleSystem _hitParticles;
    
	    private void Awake()
	    {
	        _breakingAnimator = transform.GetChild(0).GetComponent<Animator>();
	        _breakingSr = transform.GetChild(0).GetComponent<SpriteRenderer>();
	        _hitParticles = transform.GetChild(1).GetComponent<ParticleSystem>();
	        _hitParticles.gameObject.SetActive(false);
	        _breakingSr.enabled = false;
	    }

	    public override void OnNetworkSpawn()
	    {
	        if(IsOwner)
	        {
	            MiningManager.Instance.OnMiningStarted += MiningStarted;
	            MiningManager.Instance.OnMiningStopped += MiningStopped;
	        }

	        _isVisible.OnValueChanged += OnVisibleChanged;
	        _tileBreakingId.OnValueChanged += OnTileBreakingIdChanged;
	        _rscBreakingId.OnValueChanged += OnRscBreakingIdChanged;
	    }

	    public override void OnNetworkDespawn()
	    {
	        if (IsOwner)
	        {
	            MiningManager.Instance.OnMiningStarted -= MiningStarted;
	            MiningManager.Instance.OnMiningStopped -= MiningStopped;
	        }

	        _isVisible.OnValueChanged -= OnVisibleChanged;
	        _tileBreakingId.OnValueChanged -= OnTileBreakingIdChanged;
	        _rscBreakingId.OnValueChanged -= OnRscBreakingIdChanged;
	    }

	    private void MiningStopped(object sender, EventArgs e)
	    {
	        _isVisible.Value = false;
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

	                _hitParticles.Play();
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

	    private void MiningStarted(object sender, MiningManager.MiningStartedEventArgs e)
	    {
	        _sortingOrder = 0;
        
	        if(e.TileData != null)
	        {
	            _tileBreakingId.Value = GameDataRegistry.Instance.GetTileIdFromTileData(e.TileData);
	        }
	        else if(e.ResourceData != null)
	        {
	            _rscBreakingId.Value = GameDataRegistry.Instance.GetResourceIdFromResourceData(e.ResourceData);
	        }

	        if (e.TileData != null)
	        {
	            if(TileManager.Instance.HasTile(e.BreakTargetPosition + Vector3Int.down, TileType.Wall, out TileDataSO belowWallTile))
	            {
	                // There is a tile below the tile we are breaking
	                TileDataSO tileData = TileManager.Instance.WallTm.GetTile<TileDataSO>(e.BreakTargetPosition);
	                ushort tileIdOfTileBeingBroken = tileData != null ? GameDataRegistry.Instance.GetTileIdFromTileData(tileData) : ushort.MaxValue;
	                ushort tileIdOfTileBelow = belowWallTile != null ? GameDataRegistry.Instance.GetTileIdFromTileData(belowWallTile) : ushort.MaxValue;

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

	    // NTFS: This is really weird I should probably refactor this to make tile and rsc the same data type or something but it works for now
	    private void OnTileBreakingIdChanged(ushort previousValue, ushort newValue)
	    {
	        TileDataSO tileData = GameDataRegistry.Instance.GetTileDataFromTileId(newValue);
	        if (tileData != null)
	        {
	            Debug.Log($"Changing tile breaking ID from {previousValue} to {newValue}");
	            var tsa = _hitParticles.textureSheetAnimation;
	            tsa.enabled = true;
	            tsa.mode = ParticleSystemAnimationMode.Sprites;
	            for (int i = 0; i < tsa.spriteCount; i++)
	            {
	                tsa.SetSprite(i, tileData.GetRandomMiningParticleSprite());
	            }
            
	            _hitParticles.gameObject.SetActive(true);
	        }
	        else
	        {
	            _hitParticles.Stop();
	            _hitParticles.gameObject.SetActive(false);
	        }
	    }

	    private void OnRscBreakingIdChanged(ushort previousValue, ushort newValue)
	    {
	        ResourceDataSO resourceData = GameDataRegistry.Instance.GetResourceDataFromResourceId(newValue);
	        if (resourceData != null)
	        {
	            Debug.Log($"Changing resource breaking ID from {previousValue} to {newValue}");
	            var tsa = _hitParticles.textureSheetAnimation;
	            tsa.enabled = true;
	            tsa.mode = ParticleSystemAnimationMode.Sprites;
	            for (int i = 0; i < tsa.spriteCount; i++)
	            {
	                tsa.SetSprite(i, resourceData.GetRandomMiningParticleSprite());
	            }

	            _hitParticles.gameObject.SetActive(true);
	        }
	        else
	        {
	            _hitParticles.Stop();
	            _hitParticles.gameObject.SetActive(false);
	        }
	    }
	}

    
}