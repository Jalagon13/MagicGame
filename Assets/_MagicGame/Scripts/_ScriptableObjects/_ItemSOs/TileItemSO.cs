using System.Collections;
using System.Collections.Generic;
using System.Text;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Tile Item", menuName = "Create Item/New Tile Item")]
public class TileItemSO : ItemDataSO
{
	[field: SerializeField] public TileDataSO TileToPlace { get; private set; }
	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		if (!PlayerInRangeOfMouse()) return _baseActionCooldown;

		var pos = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
		var floorTmHasTile = TileManager.Instance.FloorTm.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition));
		var wallTmHasTile = TileManager.Instance.WallTm.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition));
		ushort syncTileId = GameDataRegistry.Instance.GetUShortIdFromTileData(TileToPlace);

		switch (TileToPlace.TileType)
		{
			case TileType.Floor:
				if(!floorTmHasTile && !wallTmHasTile)
				{
					ChunkManager.Instance.PlaceTileServerRpc((Vector2Int)pos, syncTileId, Player.Instance.CurrentBiome.Value, TileToPlace.TileType);
					InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
					SoundManager.Instance.PlayOneShot(TileToPlace.MiningSound, pos);
				}
				break;
			case TileType.Wall:
				if (!wallTmHasTile && IsClear(new(pos.x, pos.y)))
				{
					ChunkManager.Instance.PlaceTileServerRpc((Vector2Int)pos, syncTileId, Player.Instance.CurrentBiome.Value, TileToPlace.TileType);
					Pathfinding.Instance.AddPfWallTileServerRpc((Vector2Int)pos, Player.Instance.CurrentBiome.Value);
					InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
					SoundManager.Instance.PlayOneShot(TileToPlace.MiningSound, pos);
				}
				break;
		}
		
		return _baseActionCooldown;
	}
	

	public bool PlayerInRangeOfMouse()
	{
		return Vector2.Distance(Player.Instance.transform.position, ActionManager.MouseWorldPosition) <= 3;
	}

	public override string GetDescription()
	{
		StringBuilder description = new();
		description.Append($"Building Material<br>");
		description.Append($"Left Click to place wall<br>");
		// description.Append($"Right Click to place floor<br>");
		description.Append($"{GetDescriptionBreak()}");
		
		return description.ToString();
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
	
	public bool IsClear(Vector2 position)
	{
		Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
		var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);

		foreach(Collider2D col in colliders)
		{
			// if(col.TryGetComponent(out WorldObject clickable) || col.TryGetComponent(out Npc npc)) 
			// 	return false;
		}

		return true;
	}
}
