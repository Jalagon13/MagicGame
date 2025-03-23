using System.Collections;
using System.Collections.Generic;
using FMODUnity;
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

[CreateAssetMenu(fileName = "New TileSO", menuName = "Tiles/TileSO", order = 1)]
public class TileSO : RuleTile
{
	[Header("Tile Extended Properties")]
	public TileType TileType;
	public ItemSO DropItem;
	public int MaxHitPoints;
	public float Hardness = 1f;
	
	[Header("Sounds")]
	public EventReference MiningSound;
	public EventReference PlaceSound;
	public EventReference DestroySound;
}
