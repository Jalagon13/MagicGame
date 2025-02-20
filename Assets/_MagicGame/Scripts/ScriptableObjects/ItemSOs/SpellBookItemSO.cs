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
[CreateAssetMenu(fileName = "New SpellBook", menuName = "Create Item/New SpellBook")]
public class SpellBookItemSO : ItemSO
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
	
	private WorldObject _resourceObjectSelected;
	private SpellBookInventoryItem _wandInventoryItem;
	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		if(inventoryItem is SpellBookInventoryItem && PlayerInRangeOfMouse())
		{
			_wandInventoryItem = inventoryItem as SpellBookInventoryItem;
			AttributeData hitData = _wandInventoryItem.GetAttributeData(WandAttribute.Mining);

			if (Environment.Instance.WallTm.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition)))
			{
				Environment.Instance.HitWallTile(Player.LocalClientInstance.CurrentBiome.Value, Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), hitData.MiningPower);
				SoundManager.Instance.PlayOneShot(FMODEvents.Instance.WandCast, Player.LocalClientInstance.transform.position);
			}
			else if (ObjectManager.Instance.TryToFindWorldObject(Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), out WorldObject wo))
			{
				ObjectManager.Instance.HitObject(Player.LocalClientInstance.CurrentBiome.Value, wo, hitData.MiningPower);
			}

			return CalcMiningSpeed(WandAttribute.Mining);
		}
		
		return _baseActionCooldown;
	}
	
	private bool PlayerInRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= 4;
	}
	
	private float CalcMiningSpeed(WandAttribute wandAttribute)
	{
		AttributeData upgradeData = _wandInventoryItem.GetAttributeData(wandAttribute);
		float wandSpeedOfAttribute = upgradeData.MiningSpeed;
		float finalSpeed = wandSpeedOfAttribute / 60f;
		
		// Implement future buffs or speed prefex modifiers here.
		return finalSpeed;
	}
	
	private bool GetResourceSelected()
	{
		Collider2D[] colliders = Physics2D.OverlapPointAll(ActionManager.MouseWorldPosition);
		List<WorldObject> resourceObjectsFound = new();

		if (colliders.Count() > 0)
		{
			foreach (Collider2D c in colliders)
			{
				if (c.TryGetComponent(out WorldObject resourceObject))
				{
					resourceObjectsFound.Add(resourceObject);
				}
			}
		}

		_resourceObjectSelected = resourceObjectsFound.Count > 0 ? resourceObjectsFound.Last() : null;
		
		return _resourceObjectSelected != null;
	}
	
	public override string GetDescription()
	{
		return Description;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new SpellBookInventoryItem(this, quantity);
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