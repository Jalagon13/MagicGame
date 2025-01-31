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
				
                GameManager.Instance.SpawnItem(itemToSpawn, amountToSpawn, spawnPos);
            }
        }
    }
	
    public Dictionary<ItemSO, int> GetItemsToSpawn()
    {
        Dictionary<ItemSO, int> lootToDrop = new();

        foreach (Loot loot in Table)
        {
            if (Random.Range(0, 100) < loot.Chance)
            {
                int dropAmount = Random.Range(loot.Min, loot.Max + 1);

                if (lootToDrop.TryGetValue(loot.Item, out int existingAmount))
                {
                    lootToDrop[loot.Item] = existingAmount + dropAmount;
                }
                else
                {
                    lootToDrop.Add(loot.Item, dropAmount);
                }
            }
        }

        return lootToDrop;
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
