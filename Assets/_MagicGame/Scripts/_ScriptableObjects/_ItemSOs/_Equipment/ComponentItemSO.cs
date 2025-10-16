using UnityEngine;

namespace ProjectTinker
{
    public enum ComponentType
    {
        PickaxeHead,
        Handle,
        Binding
    }

    public class ComponentItemSO : ItemDataSO
    {
        [SerializeField] 
        private ComponentType _componentType;
        
        [field: SerializeField, Tooltip("Sprite used to dynamically create the equipment in question")] 
        public Sprite CraftedSprite { get; private set; }
    
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
