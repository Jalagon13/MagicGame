using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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
public class WandObject : ItemSO
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
	

    public override void ExecutePrimaryAction()
    {
		
    }

    public override void ExecuteSecondaryAction()
    {
		
    }
	
    public override string GetDescription()
    {
        return Description;
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