using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class CraftingTable : ResourceObject
{
    [Header("Crafting Table Parameters")]
    [SerializeField] private RecipeDataBaseObject _craftingRecipeDB;
	
    public RecipeDataBaseObject GetCraftingRecipeDB()
    {
        return _craftingRecipeDB;
    }
}
