using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "food_", menuName = "Create Item/New Food")]
public class FoodItemSO : ItemSO
{
    [field: SerializeField] public EventReference ConsumeSound { get; private set; }
    [field: SerializeField] public float Duration { get; private set; }
    
    [field: Tooltip("Net mana gained over duration")]
    [field: SerializeField] public int NetManaGain { get; private set; }
    
    [field: Tooltip("Net health gained over duration")]
    [field: SerializeField] public int NetHealthGain { get; private set; }
    

    public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
    {
        // bool manaApplied = false;
        // bool healthApplied = false;

        // if (!Player.LocalClientInstance.PlayerStats.ManaRegenBuffActive && NetManaGain > 0 && Player.LocalClientInstance.PlayerStats.CurrentMana < Player.LocalClientInstance.PlayerStats.BaseMana)
        // {
        //     Debug.Log($"Eating {inventoryItem.Quantity} {inventoryItem.Item.Name}");

        //     int manaPerSecond = Mathf.RoundToInt(NetManaGain / Duration);
        //     Player.LocalClientInstance.PlayerStats.ApplyManaRegenBuff(manaPerSecond, Duration);
        //     manaApplied = true;
        // }

        // if (!Player.LocalClientInstance.PlayerStats.HealthRegenBuffActive && NetHealthGain > 0 && Player.LocalClientInstance.HealthState.HitPoints.Value < Player.LocalClientInstance.HealthState.MaxHealth.Value)
        // {
        //     int healthPerSecond = Mathf.RoundToInt(NetHealthGain / Duration);
        //     Player.LocalClientInstance.PlayerStats.ApplyHealthRegenBuff(healthPerSecond, Duration);
        //     healthApplied = true;
        // }

        // if (manaApplied || healthApplied)
        // {
        //     SoundManager.Instance.PlayOneShot(ConsumeSound, Player.LocalClientInstance.transform.position);
        //     InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
        // }
    
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
