using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "staff_", menuName = "Create Item/New Staff")]
public class StaffItemSO : ItemSO
{
	[Tooltip("Power of each mining tick")]
	[field: SerializeField] public int MiningPower { get; private set; }
	[Tooltip("Mining speed / 60 = time between mining ticks")]
	[field: SerializeField] public int MiningSpeed { get; private set; }
	[field: SerializeField] public float MiningRange { get; private set; }

	private WorldObject _resourceObjectSelected;
	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		// if(!PlayerWithinMiningRangeOfMouse()) return _baseActionCooldown;
		
		// Vector2Int mousePos = Vector2Int.FloorToInt(ActionManager.MouseWorldPosition);

		// if (TileManager.Instance.WallTm.HasTile((Vector3Int)mousePos))
		// {
		// 	int tileID = GameManager.Instance.GetTileIdFromTileBase(TileManager.Instance.WallTm.GetTile((Vector3Int)mousePos));
			
		// 	TileManager.Instance.DestroyTileServerRpc(mousePos, tileID, Player.LocalClientInstance.CurrentPlayerBiome.Value);
			
		// 	return MiningSpeed / 60f;
		// }
		// else if (ObjectManager.Instance.TryToFindWorldObject(Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), out WorldObject wo))
		// {
		// 	ObjectManager.Instance.DestroyObjectServerRpc(Player.LocalClientInstance.CurrentPlayerBiome.Value, mousePos, GameManager.Instance.GetIDFromWorldObject(wo));

		// 	return MiningSpeed / 60f;
		// }
		
		return _baseActionCooldown;
	}
	
	public bool PlayerWithinMiningRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
	}
	
	private bool GetResourceSelected()
	{
		Collider2D[] colliders = Physics2D.OverlapPointAll(ActionManager.MouseWorldPosition);
		List<WorldObject> resourceObjectsFound = new();

		if (colliders.Count() > 0)
		{
			foreach (Collider2D c in colliders)
			{
				if (c.TryGetComponent(out WorldObject resourceObject))
				{
					resourceObjectsFound.Add(resourceObject);
				}
			}
		}

		_resourceObjectSelected = resourceObjectsFound.Count > 0 ? resourceObjectsFound.Last() : null;
		
		return _resourceObjectSelected != null;
	}
	
	public override string GetDescription()
	{
		return Description;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}