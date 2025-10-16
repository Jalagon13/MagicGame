using System.Text;
using UnityEngine;

namespace ProjectTinker
{
    [CreateAssetMenu(fileName = "New Resource Item", menuName = "Create Item/New Resource Item")]
    public class ResourceItemSO : ItemDataSO
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
            StringBuilder description = new();
            description.Append($"Crafting Material<br>");
            description.Append($"{GetDescriptionBreak()}");

            return description.ToString();
        }
    }
}
