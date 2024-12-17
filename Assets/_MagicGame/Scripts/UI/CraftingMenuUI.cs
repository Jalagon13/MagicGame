using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CraftingMenuUI : MonoBehaviour
{
    [SerializeField] private CraftNodeUI _craftNodeUIPrefab;
    [SerializeField] private Transform _craftNodeContainer;
	
    private List<RecipeDataBaseObject> _craftingDatabase;
	
    private void OnEnable()
    {
        UpdateCraftNodesView();		
    }
	
    private void Start()
    {
        CraftingManager.Instance.OnCraftingDataUpdated += CraftingManager_OnCraftingDataUpdated;
    }

    private void CraftingManager_OnCraftingDataUpdated(object sender, CraftingManager.OnCraftingDataUpdatedEventArgs e)
    {
        _craftingDatabase = e.CraftingDatabase;
		
        // If crafting menu is open, update the craft nodes view
        UpdateCraftNodesView();
    }

    private void UpdateCraftNodesView()
    {
        if(_craftingDatabase == null) return;

        // Clear existing craft nodes
        if(_craftNodeContainer.childCount != 0)
        {
            foreach (Transform child in _craftNodeContainer)
            {
                Destroy(child.gameObject);
            }	
        }
		
        // Create new craft nodes based on the crafting model's recipe list
        foreach (RecipeDataBaseObject recipeDatabase in _craftingDatabase)
        {
            foreach (RecipeSO recipeSO in recipeDatabase.RecipeDatabase)
            {
                CreateCraftNode(recipeSO);
            }
        }
    }
	
    private void CreateCraftNode(RecipeSO recipeSO)
    {
        // Instantiate a new craft node prefab, put it in the craft node container, and execute the Initialize method in CraftNodeView
        CraftNodeUI craftNodeUI = Instantiate(_craftNodeUIPrefab, _craftNodeContainer);
        craftNodeUI.Initialize(recipeSO);
    }
	
    private void OnDestroy()
    {
        CraftingManager.Instance.OnCraftingDataUpdated -= CraftingManager_OnCraftingDataUpdated;
    }
}
