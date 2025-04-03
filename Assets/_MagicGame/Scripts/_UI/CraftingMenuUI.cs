using System;
using TMPro;
using UnityEngine;

public class CraftingMenuUI : MonoBehaviour
{
    [field: SerializeField] public TextMeshProUGUI CraftingTitleText { get; set; }
    [field: SerializeField] public CraftNodeUI CraftingNodeUIPrefab { get; private set; }
    [field: SerializeField] public Transform CraftingNodesHolder { get; private set; }

    public void PopulateCraftingMenuUI(RecipeDataBaseObject recipeDataBase)
    {
        foreach (RecipeSO recipeSO in recipeDataBase.RecipeDatabase)
        {
            CraftNodeUI craftNodeUI = Instantiate(CraftingNodeUIPrefab, CraftingNodesHolder);
            craftNodeUI.Initialize(recipeSO);
        }
    }
}