using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using UnityEngine;

public class ResourceObject : WorldObject
{
    public event EventHandler OnBrokenByPlayer;

    [SerializeField] private WandAttribute _harvestType;
    [SerializeField] private int _maxHitPoints = 100;
    [SerializeField] private LootTable _lootTable;
	
    [FoldoutGroup("Feedbacks")]
    [SerializeField] private MMF_Player _clickFeedback;
    [FoldoutGroup("Feedbacks")]
    [SerializeField] private MMF_Player _destroyFeedback;
    [FoldoutGroup("Feedbacks")]
    [SerializeField] private MMF_Player _spawnFeedback;
	
    private Vector2 _dropPosOffset;
	
    public void Start()
    {
        _dropPosOffset = Vector2.one * 0.5f;
        _spawnFeedback?.PlayFeedbacks();
    }
	
    public void OverrideLootTable(LootTable lootTable)
    {
        _lootTable = lootTable;
    }
	
    public void DestroyResourceAsset()
    {
        OnBrokenByPlayer?.Invoke(this, EventArgs.Empty);
		
        _lootTable.SpawnLoot((Vector2)transform.position + _dropPosOffset);
		
        if (_destroyFeedback != null)
        {
            _destroyFeedback.transform.SetParent(null);
            _destroyFeedback?.PlayFeedbacks(_clickFeedback.transform.position, _maxHitPoints);
        }
		
        Destroy(gameObject);
    }
	
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
	
    public int GetMaxHitPoints()
    {
        return _maxHitPoints;
    }
	
    public WandAttribute GetHarvestType()
    {
        return _harvestType;
    }
}
