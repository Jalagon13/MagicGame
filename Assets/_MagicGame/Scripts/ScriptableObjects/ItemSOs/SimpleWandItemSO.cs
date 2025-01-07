using UnityEngine;

public class SimpleWandItemSO : ItemSO
{

	[SerializeField] private GameObject _simpleProjectilePrefab;

	public override void ExecutePrimaryAction()
	{

	}

	public override void ExecuteSecondaryAction()
	{

	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new SimpleWandInventoryItem(this, quantity);
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}
