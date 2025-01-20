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
	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		var pos = Vector3Int.FloorToInt(ActionManager.MouseWorldPosition);
		
		Tilemap wallTilemap = Environment.Instance.GetWallTilemapData().GetTilemap();
		
		bool wallTmHasTile = wallTilemap.HasTile(Vector3Int.FloorToInt(ActionManager.MouseWorldPosition));
		
		if(IsClear(new(pos.x, pos.y)) && !wallTmHasTile && PlayerInRangeOfMouse())
		{
			Environment.Instance.PlaceTile(pos, _wallTile, TileType.Wall, Player.LocalClientInstance.GetPlayerEnvironment());
			
			InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
			
			SoundManager.Instance.PlayOneShot(_wallTile.HitSound, pos);
		}
		
		return _baseActionCooldown;
	}
	

	public bool PlayerInRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= 3;
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
