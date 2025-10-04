using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectWizard
{
    [CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/New Recipe")]
    public class RecipeDataSO : ScriptableObject
    {
        public ItemDataSO OutputItem;
        public int OutputAmount;
        public List<InventoryItem> ResourceList = new();
    }
}
