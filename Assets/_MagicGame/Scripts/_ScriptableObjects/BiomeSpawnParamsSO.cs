using System;
using System.Collections.Generic;
using UnityEngine;

public class BiomeSpawnParamsSO : ScriptableObject
{
	[field: SerializeField] public List<BiomeSpawnRule> AllBiomeSpawnRules { get; private set; } = new();

	public BiomeSpawnRule GetCurrentBiomeSpawnRule()
	{
		if(Player.LocalClientInstance == null) return default;
	
		foreach (BiomeSpawnRule biomeSpawnRule in AllBiomeSpawnRules)
		{
			if (biomeSpawnRule.Biome == Player.LocalClientInstance.CurrentPlayerBiome.Value)
			{
				return biomeSpawnRule;
			}
		}

		Debug.LogError($"{Player.LocalClientInstance.CurrentPlayerBiome.Value} has not been found.");
		return default;
	}
	
	public BiomeSpawnRule GetBiomeSpawnRule(BiomeType biomeType)
	{
		foreach (BiomeSpawnRule biomeSpawnRule in AllBiomeSpawnRules)
		{
			if (biomeSpawnRule.Biome == biomeType)
			{
				return biomeSpawnRule;
			}
		}

		Debug.LogError($"{biomeType} has not been found.");
		return default;
	}
}

[Serializable]
public class BiomeSpawnRule
{
	public BiomeType Biome;
	[Tooltip("Higher values mean lower spawn rates.")]
	public int SpawnRateDenominator;
	[Tooltip("Maximum NPCs that can exist in this biome at once.")]
	public int MaxNpcSlotAmount;
	public bool HasDayNightCycle;
	public List<NpcSpawnData> NpcSpawnTable;

	public NpcSpawnData GetRandomNpc()
	{
		bool isNightTime = WorldManager.Instance.IsNight;
	
		List<NpcSpawnData> validNpcEntries = new();

		// Filter NPCs based on the time of day (if applicable)
		foreach (var entry in NpcSpawnTable)
		{
			if (!HasDayNightCycle || entry.SpawnCondition == SpawnTimeCondition.AnyTime ||
				(isNightTime && entry.SpawnCondition == SpawnTimeCondition.NightOnly) ||
				(!isNightTime && entry.SpawnCondition == SpawnTimeCondition.DayOnly))
			{
				validNpcEntries.Add(entry);
			}
		}

		// If no NPCs match the time condition, fall back to spawning any available NPC
		if (validNpcEntries.Count == 0)
		{
			validNpcEntries.AddRange(NpcSpawnTable);
			Debug.LogWarning($"No valid NPCs found for the current time in {Biome}. Falling back to any available NPC.");
		}

		return SelectRandomNpc(validNpcEntries);
	}

	private NpcSpawnData SelectRandomNpc(List<NpcSpawnData> npcEntries)
	{
		float totalWeight = 0f;
		foreach (var entry in npcEntries)
		{
			totalWeight += entry.RelativeSpawnWeight;
		}

		float randomValue = UnityEngine.Random.Range(0, totalWeight);
		float currentWeight = 0f;

		foreach (var entry in npcEntries)
		{
			currentWeight += entry.RelativeSpawnWeight;
			if (randomValue <= currentWeight)
			{
				return entry;
			}
		}

		Debug.LogError($"Random NPC selection failed unexpectedly in {Biome}. Returning first NPC in the table.");
		return npcEntries[0];
	}
}

[Serializable]
public class NpcSpawnData
{
	public NpcSO NpcData;
	
	[Tooltip("Higher values mean higher spawn chances.")]
	[Range(0.01f, 100.0f)]
	public float RelativeSpawnWeight;
	public SpawnTimeCondition SpawnCondition;
}

public enum SpawnTimeCondition
{
	AnyTime,
	DayOnly,
	NightOnly
}