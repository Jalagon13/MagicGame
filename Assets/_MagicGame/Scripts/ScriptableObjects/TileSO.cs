using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public enum TileType
{
	Ground,
	Wall,
	Floor
}

[CreateAssetMenu()]
public class TileSO : RuleTile
{
	[Header("Tile Extended Properties")]
	public TileType TileType;
	public WandAttribute HarvestType;
	public ItemSO DropItem;
	public int MaxHitPoints;
	
	[Header("Sounds")]
	public AudioClip HitSound;
	public AudioClip PlaceSound;
	public AudioClip DestroySound;
}
