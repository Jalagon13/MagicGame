using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/New Recipe")]
public class RecipeSO : ScriptableObject
{
    public ItemDataSO OutputItem;
    public int OutputAmount;
    public List<InventoryItem> ResourceList = new();
}
