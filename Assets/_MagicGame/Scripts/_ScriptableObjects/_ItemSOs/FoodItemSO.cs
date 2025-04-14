using UnityEngine;

[CreateAssetMenu(fileName = "food_", menuName = "Create Item/New Food")]
public class FoodItemSO : ItemSO
{
    public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
    {
        return _baseActionCooldown;
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
