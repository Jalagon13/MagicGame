using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[SelectionBase]
public class WorldObject : MonoBehaviour // Base class for every "physical" asset in the world
{	
	public static float InteractDistance = 2.75f;
	
	[field: SerializeField] public string WorldObjectName { get; private set; }
	[field: SerializeField] public float Hardness { get; private set; } = 1f;
	[field: SerializeField] public bool PassThrough { get; private set; } = false;
	[field: SerializeField] public bool CanBeDestroyed { get; private set; } = true;
	[field: SerializeField] public List<Loot> Table { get; private set; }
	[field: SerializeField] public EventReference MiningSound { get; private set; }
	[field: SerializeField] public EventReference ResourceDestroyed { get; private set; }
	[field: SerializeField] public EventReference PlaceSound { get; private set; }
	
	protected CardinalDirection _orientation;


	private void Awake()
	{
		transform.GetChild(0).gameObject.SetActive(!PassThrough); // Disable local collider so player can walk through it
	}
	
	public virtual void SetOrientation(CardinalDirection orientation)
	{
		_orientation = orientation;
	}
	
	public void DestroyObject(Vector2Int objectPosition, BiomeType biome)
	{
		LootTable.SpawnLoot(Table, (Vector2)objectPosition + (Vector2.one * 0.5f), biome);
		SoundManager.Instance.PlayOneShot(ResourceDestroyed, transform.position);
		ChunkManager.Instance.RemoveObjectDataFromChunkServerRpc(objectPosition, biome);
		
		if(!PassThrough)
		{
			Pathfinding.Instance.RemovePfWallTileServerRpc(objectPosition, biome);
		}
		
		Lightmap.Instance.UpdateLightMap();
	}
	
	protected bool PlayerInRangeOfPosition(Vector2 position)
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, position) <= InteractDistance;
	}
	
	public void DestroySelf()
	{
		Destroy(gameObject);
	}
}
