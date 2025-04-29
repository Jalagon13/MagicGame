using UnityEngine;

[CreateAssetMenu(fileName = "Spell_", menuName = "Create Item/New Mining Spell")]
public class MiningSpellItemSO : SpellItemSO
{
    [field: Header("Mining Spell")]
    [field: Tooltip("Power of each mining tick")]
    [field: SerializeField] public int MiningPower { get; private set; }
    [field: Tooltip("Mining speed / 60 = time between mining ticks")]
    [field: SerializeField] public float MiningRange { get; private set; }

    public bool PlayerWithinMiningRangeOfMouse()
    {
        return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
    }

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
