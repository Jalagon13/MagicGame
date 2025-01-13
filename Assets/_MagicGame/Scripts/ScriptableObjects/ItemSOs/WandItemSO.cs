using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Tilemaps;
using System.Linq;

public enum WandAttribute 
{
	Mining, 
	WoodCutting,
	Construction,
	Range,
}

// Wand upgrades and upgrade data need to live in here and injected into WandInventoryItem somehow
// Level system must stay in WandInventoryItem and Wand upgrade data in here
[CreateAssetMenu(fileName = "New Wand", menuName = "Create Item/New Wand")]
public class WandItemSO : ItemSO
{
	[Space(10)]
	[Title("Mining Upgrades", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	public List<AttributeData> MiningUpgrades = new();
	
	[Space(10)]
	[Title("WoodCutting Upgrades", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	public List<AttributeData> WoodCuttingUpgrades = new();
	
	[Space(10)]
	[Title("Construction Upgrades", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	public List<AttributeData> ConstructionUpgrades = new();
	
	[Space(10)]
	[Title("Range Upgrades", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	public List<RangeData> RangeUpgrades = new();
	
	private ResourceObject _resourceObjectSelected;
	private WandInventoryItem _wandInventoryItem;
	
	public override float ExecuteItemAction(InventoryItem inventoryItem)
	{
		if(inventoryItem is not WandInventoryItem || !Player.LocalClientInstance.gameObject.GetComponent<Player>().IsHoldingAWand()) return _baseActionCooldown;
		
		_wandInventoryItem = inventoryItem as WandInventoryItem;
		bool mouseOverWall = GetMouseOverWall();
		bool resourceSelected = GetResourceSelected();

		if (!mouseOverWall && !resourceSelected) return _baseActionCooldown;
		
		WandAttribute wandAttribute = GetHarvestType(false, mouseOverWall, resourceSelected);
		AttributeData hitData = _wandInventoryItem.GetAttributeData(wandAttribute);

		GameManager.Instance.SpawnMiningProjectile(
			Player.LocalClientInstance.transform.position,
			ActionManager.MouseWorldPosition,
			hitData.MiningPower,
			false, mouseOverWall, resourceSelected);
			
		return CalcMiningSpeed(wandAttribute);
	}
	
	private float CalcMiningSpeed(WandAttribute wandAttribute)
	{
		AttributeData upgradeData = _wandInventoryItem.GetAttributeData(wandAttribute);
		float wandSpeedOfAttribute = upgradeData.MiningSpeed;
		float finalSpeed = wandSpeedOfAttribute / 60f;
		
		// Implement future buffs or speed prefex modifiers here.
		return finalSpeed;
	}
	
	private WandAttribute GetHarvestType(bool mouseOverFloor, bool mouseOverWall, bool resourceSelected)
	{
		Vector3Int tilePosMouseIsHovering = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
		Vector2Int tilePos = new (tilePosMouseIsHovering.x, tilePosMouseIsHovering.y);
		
		if(mouseOverFloor)
		{
			return Environment.Instance.GetFloorTilemapData().GetHarvestType(tilePos);
		}
		else if(mouseOverWall)
		{
			return Environment.Instance.GetWallTilemapData().GetHarvestType(tilePos);
		}
		else if(resourceSelected)
		{
			return _resourceObjectSelected.GetHarvestType();
		}
		
		Debug.LogError($"Error, could not find a harvest type for mining");
		return default;
	}
	
	private bool GetResourceSelected()
	{
		Collider2D[] colliders = Physics2D.OverlapPointAll(ActionManager.MouseWorldPosition);
		List<ResourceObject> resourceObjectsFound = new();

		if (colliders.Count() > 0)
		{
			foreach (Collider2D c in colliders)
			{
				if (c.TryGetComponent(out ResourceObject resourceObject))
				{
					resourceObjectsFound.Add(resourceObject);
				}
			}
		}

		_resourceObjectSelected = resourceObjectsFound.Count > 0 ? resourceObjectsFound.Last() : null;
		
		return _resourceObjectSelected != null;
	}
	
	private bool GetMouseOverWall()
	{
		Tilemap wallTilemap = Environment.Instance.GetWallTilemapData().GetTilemap();
		Vector3Int tilePosition = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
		
		return wallTilemap.HasTile(tilePosition);
	}
	
	// private void PerformSecondaryMiningAction()
	// {
	// 	bool mouseOverFloor = GetMouseOverFloor();

	// 	if (!mouseOverFloor) return;

	// 	WandAttribute wandAttribute = GetHarvestType(true, false, false);
	// 	AttributeData hitData = _wandItem.GetAttributeData(wandAttribute);

	// 	GameManager.Instance.SpawnMiningProjectile(
	// 		Player.LocalClientInstance.GetWandProjectileSpawnPoint().position,
	// 		MouseWorldPosition,
	// 		hitData.MiningPower,
	// 		true, false, false);

	// 	CalcMiningSpeed(wandAttribute);
	// }
	
	// private bool GetMouseOverFloor()
	// {
	// 	Tilemap floorTilemap = Environment.Instance.GetFloorTilemapData().GetTilemap();
	// 	Vector3Int tilePosition = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
		
	// 	return floorTilemap.HasTile(tilePosition);
	// }

	public override string GetDescription()
	{
		return Description;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new WandInventoryItem(this, quantity);
	}
	
	public int GetMaxUpgradeLevelAmount(WandAttribute upgradeType)
	{
		switch (upgradeType)
		{
			case WandAttribute.Mining:
				return MiningUpgrades.Count;
			case WandAttribute.WoodCutting:
				return WoodCuttingUpgrades.Count;
			case WandAttribute.Construction:
				return ConstructionUpgrades.Count;
			case WandAttribute.Range:
				return RangeUpgrades.Count;
		}
		Debug.LogError("Unsupported Upgrade, returnign 0 for max upgrade level index");
		return 0;
	}
	
	public AttributeData GetUpgradeData(WandAttribute upgradeType, int levelIndex)
	{
		switch (upgradeType)
		{
			case WandAttribute.Mining:
				if (levelIndex < MiningUpgrades.Count)
					return MiningUpgrades[levelIndex];
				break;
			case WandAttribute.WoodCutting:
				if (levelIndex < WoodCuttingUpgrades.Count)
					return WoodCuttingUpgrades[levelIndex];
				break;
			case WandAttribute.Construction:
				if (levelIndex < ConstructionUpgrades.Count)
					return ConstructionUpgrades[levelIndex];
				break;
		}
		
		Debug.LogWarning("Returning default upgrade data for " + upgradeType.ToString() + " level " + levelIndex + 
		"/nBecause either level index is out of range or could not find " + upgradeType.ToString());
		return default;
	}
	
	public RangeData GetRangeData(int levelIndex)
	{
		if (levelIndex < RangeUpgrades.Count)
			return RangeUpgrades[levelIndex];
			
		Debug.LogWarning("Returning default range data for level " + levelIndex + 
		"/nBecause either level index is out of range");
		return default;
	}
}


[Serializable]
public struct AttributeData 
{
	[GUIColor(1.0f, 1.0f, 0.0f)]
	public int MiningPower;
	public int MiningSpeed;
	public List<InventoryItem> Requirements;
}

[Serializable]
public struct RangeData 
{
	[GUIColor(1.0f, 1.0f, 0.0f)]
	public float RangeValue;
	public List<InventoryItem> Requirements;
}