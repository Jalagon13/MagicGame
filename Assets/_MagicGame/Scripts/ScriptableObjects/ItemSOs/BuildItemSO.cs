using System.Collections;
using System.Collections.Generic;
using System.Text;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Build Object", menuName = "Create Item/New Build Object")]
public class BuildItemSO : ItemSO
{
	[SerializeField] private TileSO _wallTile;
	[SerializeField] private TileSO _floorTile;
	// [SerializeField] private TilemapObject _wallTm, _floorTm, _spawnFloorTilemap;
	
	public override void ExecutePrimaryAction(InventoryItem inventoryItem)
	{
		var pos = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
		
		Tilemap wallTilemap = Environment.Instance.GetWallTilemapData().GetTilemap();
		
		bool wallTmHasTile = wallTilemap.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition));
		
		if(IsClear(new(pos.x, pos.y)) && !wallTmHasTile && ActionManager.Instance.PlayerInRangeOfMouse())
		{
			Environment.Instance.PlaceTile(pos, _wallTile, TileType.Wall, Player.LocalClientInstance.GetPlayerEnvironment());
			
			InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
			
			MMSoundManagerSoundPlayEvent.Trigger(_wallTile.PlaceSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, pitch:UnityEngine.Random.Range(0.9f, 1.1f));
		}
	}

	public override void ExecuteSecondaryAction(InventoryItem inventoryItem)
	{
		var pos = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
		
		Tilemap floorTilemap = Environment.Instance.GetFloorTilemapData().GetTilemap();
		
		bool floorTmHasTile = floorTilemap.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition));
		
		if(!floorTmHasTile && ActionManager.Instance.PlayerInRangeOfMouse())
		{
			Environment.Instance.PlaceTile(pos, _floorTile, TileType.Floor, Player.LocalClientInstance.GetPlayerEnvironment());
			
			InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
			
			MMSoundManagerSoundPlayEvent.Trigger(_floorTile.PlaceSound, MMSoundManager.MMSoundManagerTracks.Sfx, default, pitch:UnityEngine.Random.Range(0.9f, 1.1f));
		}
	}

	public override string GetDescription()
	{
		StringBuilder description = new();
		description.Append($"Building Material<br>");
		description.Append($"Left Click to place wall<br>");
		description.Append($"Right Click to place floor<br>");
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
			if(col.TryGetComponent(out ResourceObject clickable)) 
				return false;
		}

		return true;
	}
	
	public TileSO GetWallTile()
	{
		return _wallTile;
	}
}
