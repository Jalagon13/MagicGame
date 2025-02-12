using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[SelectionBase]
public class WorldObject : MonoBehaviour // Base class for every "physical" asset in the world
{	
	[field: SerializeField] public string WorldObjectName { get; private set; }
	[field: SerializeField] public int MaxHp { get; private set; }
	[field: SerializeField] public bool PassThrough { get; private set; } = false;
	[field: SerializeField] public WandAttribute HarvestType { get; private set; }
	[field: SerializeField] public List<Loot> Table { get; private set; }
	[field: SerializeField] public EventReference ResourceHit { get; private set; }
	[field: SerializeField] public EventReference ResourceDestroyed { get; private set; }
	
	private void Awake()
	{
		transform.GetChild(0).gameObject.SetActive(!PassThrough); // Disable local collider so player can walk through it
	}
	
	public void DestroyObject(Vector2Int objectPosition, BiomeType biome)
	{
		LootTable.SpawnLoot(Table, (Vector2)objectPosition + (Vector2.one * 0.5f), Player.LocalClientInstance.CurrentBiome.Value);
		SoundManager.Instance.PlayOneShot(ResourceDestroyed, transform.position);
		ChunkManager.Instance.RemoveObjectDataFromChunk(objectPosition, biome);
		
		if(!PassThrough)
		{
			Pathfinding.Instance.RemovePfWallTile(objectPosition, biome);
		}
		
		Environment.Instance.RemoveTileVisData((Vector3Int)objectPosition);
		Lightmap.Instance.UpdateLightMap();
	}
	
	public void DestroySelf()
	{
		Destroy(gameObject);
	}
}
