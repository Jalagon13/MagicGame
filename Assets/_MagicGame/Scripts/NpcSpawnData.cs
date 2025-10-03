using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum SingleBiomeType
{
    None = 0,
    Forest = 1,
    Cave = 2,
}

[Serializable]
public class NpcSpawnData
{
    [field: SerializeField]
    public List<BiomeSpawnRule> AllSpawnRules { get; private set; } = new();

    public BiomeSpawnRule GetSpawnRules(BiomeType biomeType)
    {
        // Convert BiomeType to SingleBiomeType for comparison
        SingleBiomeType singleBiomeType = (SingleBiomeType)biomeType;
        
        foreach (var rule in AllSpawnRules)
        {
            if (rule.Biome == singleBiomeType)
                return rule;
        }

        Debug.LogError($"{biomeType} has not been found.");
        return default;
    }

    public CharacterSpawnData SelectRandomNpc(BiomeType biomeType)
	{
		BiomeSpawnRule spawnRule = GetSpawnRules(biomeType);
		if (spawnRule == null || spawnRule.CharacterSpawnData.Count == 0)
		{
			Debug.LogError($"No spawn data found for biome: {biomeType}");
			return null;
		}

		float totalWeight = 0f;
		foreach (CharacterSpawnData entry in spawnRule.CharacterSpawnData)
		{
			totalWeight += entry.RelativeSpawnWeight;
		}

		float randomValue = UnityEngine.Random.Range(0, totalWeight);
		float currentWeight = 0f;

		foreach (var entry in spawnRule.CharacterSpawnData)
		{
			currentWeight += entry.RelativeSpawnWeight;
			if (randomValue <= currentWeight)
			{
				return entry;
			}
		}

		Debug.LogError($"Random NPC selection failed unexpectedly in {biomeType}. Returning first NPC in the table.");
		return spawnRule.CharacterSpawnData[0];
	}
}

[Serializable]
public class BiomeSpawnRule
{
    [field: SerializeField]
    public SingleBiomeType Biome { get; private set; }
    
    [field: SerializeField, Tooltip("How many NPCs spawn per minute in this biome"), Range(0f, 60f)]
    public float SpawnsPerMinute { get; private set; }
    
    [field: SerializeField, Tooltip("Maximum NPCs that can exist in this biome at once.")]
    public int MaxNpcSlotAmount { get; private set; }
    
    [field: SerializeField]
    public bool HasDayNightCycle { get; private set; }

    [field: SerializeField]
    public List<CharacterSpawnData> CharacterSpawnData { get; private set; } = new();
}

[Serializable]
public class CharacterSpawnData
{
    [field: SerializeField]
    public CharacterDataSO CharacterData { get; private set; }
	
	[field: SerializeField, Tooltip("Higher values mean higher spawn chances."), Range(0.01f, 100.0f)]
	public float RelativeSpawnWeight { get; private set; }
	
	[field: SerializeField]
	public SpawnTimeCondition SpawnCondition { get; private set; }
}

public enum SpawnTimeCondition
{
	AnyTime,
	DayOnly,
	NightOnly
}