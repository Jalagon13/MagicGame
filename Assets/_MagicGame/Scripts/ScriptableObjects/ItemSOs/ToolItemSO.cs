using UnityEngine;

[CreateAssetMenu(fileName = "New Tool Item", menuName = "Create Item/New Tool Item")]
public class ToolItemSO : ItemSO
{
	public override float ExecutePrimaryAction(InventoryItem inventoryItem)
	{
		if(CanMine())
		{
			Vector2Int minePos = Vector2Int.FloorToInt(ActionManager.MouseWorldPosition);
			Environment.Instance.GetWallTilemapData().HitTile(minePos, 35, Player.LocalClientInstance.GetPlayerEnvironment());
			Debug.Log("Mining");
		}
	
		return _baseActionCooldown;
	}
	
	private bool CanMine()
	{
		return Player.LocalClientInstance != null && 
		!Player.LocalClientInstance.IsDead() && 
		!Pointer.IsOverUI() && 
		Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= 3;
	}

	public override float ExecuteSecondaryAction(InventoryItem inventoryItem)
	{
		return _baseActionCooldown;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}
