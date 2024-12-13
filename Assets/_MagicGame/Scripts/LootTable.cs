using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class LootTable
{
    public List<Loot> Table;
	
    public void SpawnLoot(Vector2 spawnPos)
    {
        var itemsToSpawn = GetItemsToSpawn();

        if(itemsToSpawn.Count > 0)
        {
            foreach (var itemsToSpawnKVP in itemsToSpawn)
            {
                // Spawn Loot here.
				
                ItemSO itemToSpawn = itemsToSpawnKVP.Key;
                int amountToSpawn = itemsToSpawnKVP.Value;	
				
                GameManager.Instance.SpawnItem(itemToSpawn, amountToSpawn, spawnPos, true);
            }
        }
    }
	
    public Dictionary<ItemSO, int> GetItemsToSpawn()
    {
        Dictionary<ItemSO, int> lootReturn = new();

        foreach (Loot loot in Table)
        {
            if (Random.Range(0, 100) < loot.Chance)
            {
                int dropAmount = Random.Range(loot.Min, loot.Max + 1);
                lootReturn.Add(loot.Item, dropAmount);
            }
        }

        return lootReturn;
    }
}

[Serializable]
public class Loot 
{
    public ItemSO Item;
    public int Min;
    public int Max;
    [Range(0.0f, 100.0f)]
    public float Chance;
}
