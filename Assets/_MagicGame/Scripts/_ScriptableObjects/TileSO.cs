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
	Floor,
	Wall,
	Ore
}

[CreateAssetMenu(fileName = "New TileSO", menuName = "Tiles/TileSO", order = 1)]
public class TileSO : RuleTile
{
	[Header("Tile Extended Properties")]
	public TileType TileType;
	[field: SerializeField]
	public List<Loot> Table { get; private set; }
	public float Hardness = 0.65f;
	
	[Header("Sounds")]
	public EventReference MiningSound;
	public EventReference PlaceSound;
	public EventReference DestroySound;
	
	[Header("Top Tiles (For Walls Only)")]
	public Sprite TopTileSingle;
	public Sprite TopTileLeft;
	public Sprite TopTileCenter;
	public Sprite TopTileRight;
}
