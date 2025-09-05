using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public enum TileType
{
	Terrain, Floor, Wall, Ore, Liquid, Foliage
}

[CreateAssetMenu(fileName = "New TileSO", menuName = "Tiles/TileSO", order = 1)]
public class TileDataSO : RuleTile
{
	[field: Header("TileData Properties")]
	[Tooltip("Name of the resource world object")]
	public string StringID;
	[field: SerializeField] public TileType TileType { get; private set; }
	[field: SerializeField] public ToolType ToolTypeNeededForHarvest { get; private set; }
	[field: SerializeField] public float Hardness { get; private set; } = 0.65f;
	[field: SerializeField] public List<Loot> ItemDropTable { get; private set; }
	
	[field: Header("Game Feel")]
	[field: SerializeField] public EventReference MiningSound { get; private set; }
	[field: SerializeField] public EventReference PlaceSound { get; private set; }
	[field: SerializeField] public EventReference DestroySound { get; private set; }

	[field: Header("Top Tiles (For Walls Only)")]
	[field: SerializeField] public Sprite TopTileSingle { get; private set; }
	[field: SerializeField] public Sprite TopTileLeft { get; private set; }
	[field: SerializeField] public Sprite TopTileCenter { get; private set; }
	[field: SerializeField] public Sprite TopTileRight { get; private set; }
	
	[field: Header("Dual Grid Properties (probably make this its own class)")]
	[field: SerializeField] public Material DualGridFillTileMaterial { get; private set; }
	[field: SerializeField] public TileBase[] DualGridTiles { get; private set; }
}
