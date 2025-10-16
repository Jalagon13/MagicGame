using UnityEngine;

namespace ProjectTinker
{
    // Brainstorm
    // For each tool they can be created at runtime and depending on the tool can hold the IDs of the components used to craft it.
    // So this PickaxeItemSO, will just hold like instructions on how to craft it like what components it wants to be crafted.
    // So maybe I can create a general component ItemSO, and then to differentiate each component, I can have a public enum of ComponentType or something.
    // So like in ComponentType there can be a PickaxeHead and a Handle type. Each Component Item has an in-inventory UI sprite and a for-crafted-item sprite
    // for when you craft the tool with component items, and the dynamic item sprite is created by layering compoent for-crafted-item sprites on top of each other
    // Definitely need to play Tinker's Construct more to remember how it worked.
    
    [CreateAssetMenu(fileName = "New Pickaxe", menuName = "Create Item/New Pickaxe")]
    public class PickaxeItemSO : ItemDataSO
    {

    
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
            return Description;
        }
    }
}
