using UnityEngine;

[CreateAssetMenu(fileName = "New Mining Spell", menuName = "Create Item/New Mining Spell")]
public class MiningSpellItemSO : ItemDataSO
{
    [field: SerializeField] 
    public ToolType ToolType { get; private set; }

    [field: SerializeField] 
    public int MiningPower { get; private set; } = 1;
    
    [field: SerializeField] 
    public float MiningRange { get; private set; } = 4;

    public override InventoryItem CreateInventoryItem(int quantity)
    {
        return new InventoryItem(this, quantity);
    }

    public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
    {
        return _baseActionCooldown;
    }

    public override string GetDescription()
    {
        return string.Empty;
    }

    public bool PlayerWithinMiningRangeOfMouse()
    {
        return Vector2.Distance(Player.Instance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
    }
}
