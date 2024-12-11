using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;



[Serializable]
public class WandInventoryItem : InventoryItem
{
    public WandObject WandObject => (WandObject) Item;
    public Dictionary<WandAttribute, int> AttributesAndLevelIndex = new(); // Holds the upgrade type and its level

    public WandInventoryItem(ItemSO itemObject, int quantity)
    {
        Item = itemObject as WandObject;
        Quantity = quantity;
		
        // Initialize new UpgradeStorage with default values for all upgrade types
        AttributesAndLevelIndex = new() 
        {  
            [WandAttribute.Mining] = 0,
            [WandAttribute.WoodCutting] = 0,
            [WandAttribute.Construction] = 0,
            [WandAttribute.Range] = 0,
            // Add more upgrade types as needed
        };
    }
	
    public string GetDescription()
    {
        StringBuilder description = new();
        description.Append($"{WandObject.GetDescription()}<br>");
        description.Append($"<br>Attributes:<br>");
        foreach (var kvp in AttributesAndLevelIndex)
        {
            if(kvp.Key == WandAttribute.Range)
            {
                string toolSkillName = kvp.Key.ToString();
                int toolSkillLevel = kvp.Value + 1;
                float rangeValue = GetRangeValue();
                description.Append($"<color=yellow>Lvl {toolSkillLevel} {toolSkillName} </color=yellow> | " + 
                $"<color=orange>{rangeValue}  Tiles</color=orange><br>");
            }
            else
            {
                string toolSkillName = kvp.Key.ToString();
                AttributeData attributeData = GetAttributeData(kvp.Key);
                int toolSkillLevel = kvp.Value + 1;
                int attributePower = attributeData.MiningPower;
                int attributeSpeed = attributeData.MiningSpeed;
                description.Append($"<color=yellow>Lvl {toolSkillLevel} {toolSkillName} </color=yellow> | " + 
                $"<color=orange>{attributePower}  Power </color=orange>| <color=green>{30 - attributeSpeed} Speed</color=green><br>");
            }
        }
		
        return description.ToString();
    }
	
    public float GetRangeValue()
    {
        int levelIndex = AttributesAndLevelIndex[WandAttribute.Range];
        RangeData rangeData = WandObject.GetRangeData(levelIndex);
        return rangeData.RangeValue;
    }
	
    public bool NextUpgradeExists(WandAttribute upgradeType)
    {
        if(AttributesAndLevelIndex.ContainsKey(upgradeType))
        {
            int levelIndex = AttributesAndLevelIndex[upgradeType];
            int maxLevelCount = WandObject.GetMaxUpgradeLevelAmount(upgradeType);
            int maxLevelIndex = maxLevelCount - 1;
			
            if(levelIndex < maxLevelIndex)
            {
                return true;
            }
        }
		
        return false;
    }
	
    public void UpgradeWand(WandAttribute upgradeType)
    {
        if(AttributesAndLevelIndex.ContainsKey(upgradeType))
        {
            AttributesAndLevelIndex[upgradeType]++;
            int levelDisplay = AttributesAndLevelIndex[upgradeType] + 1;
            Debug.Log("Upgraded " + upgradeType.ToString() + " to level " + levelDisplay);
        }
    }
	
    public AttributeData GetAttributeData(WandAttribute upgradeType)
    {
        if (AttributesAndLevelIndex.ContainsKey(upgradeType))
        {
            int levelIndex = AttributesAndLevelIndex[upgradeType];
            AttributeData upgradeData = WandObject.GetUpgradeData(upgradeType, levelIndex);
            return upgradeData;
        }
        Debug.LogError($"UpgradeStorage does not contain key: {upgradeType}, returning default instead.");
        return default;
    }
	
    public RangeData GetRangeData()
    {
        int levelIndex = AttributesAndLevelIndex[WandAttribute.Range];
        RangeData rangeData = WandObject.GetRangeData(levelIndex);
        return rangeData;
    }
}