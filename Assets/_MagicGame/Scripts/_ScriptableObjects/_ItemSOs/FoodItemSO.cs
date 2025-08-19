using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "food_", menuName = "Create Item/New Food")]
public class FoodItemSO : ItemSO
{
    [field: SerializeField] public EventReference ConsumeSound { get; private set; }
    
    [field: Tooltip("Net mana gained over duration")]
    [field: SerializeField] public int ManaGain { get; private set; }

    [field: Tooltip("Net mana gained over duration")]
    [field: SerializeField] public int HealthGain { get; private set; }

    public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
    {
        bool gainedStat = false;
    
        if(!Player.Instance.SpellCastController.PlayerManaSystem.IsFullOnMana())
        {
            Player.Instance.SpellCastController.PlayerManaSystem.AddMana(ManaGain);
            gainedStat = true;
        }
        
        if(!Player.Instance.ServerCharacter.NetHealthState.IsFullHp())
        {
            Player.Instance.ServerCharacter.NetHealthState.AddHp(HealthGain);
            gainedStat = true;
        }

        if(gainedStat)
        {
            Debug.Log($"Gained Stat");
            SoundManager.Instance.PlayOneShot(ConsumeSound, Player.Instance.transform.position);
            InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
        }

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
