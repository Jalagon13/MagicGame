using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InGameMenuReferenceHolder))]
public class InGameMenu : MonoBehaviour
{
    public static InGameMenu Instance { get; private set; }
    
    public event EventHandler OnMenuOpen;
    
    private InGameMenuReferenceHolder _menuReferenceHolder;
    private InGameMenuInstantiateHandler _instantiateHandler;
    
    private void Awake()
    {
        Instance = this;
        _menuReferenceHolder = GetComponent<InGameMenuReferenceHolder>();
        _instantiateHandler = GetComponent<InGameMenuInstantiateHandler>();
    }
    
    private void Start()
    {
        GameInput.Instance.OnInventoryToggle += OnInventoryToggleOff;
    }

    private void OnInventoryToggleOff(object sender, GameInput.OnToggleInventoryEventArgs e)
    {
        if (!e.InventoryOpen)
        {
            ClearOldMenu();
        }
    }

    public void OpenCraftingMenu(RecipeDataBaseObject recipeDataBase, GameObject menuSourceGO)
    {
        ClearOldMenu();

        CraftingMenuUI craftingMenuUI = _instantiateHandler.InstantiateCraftingMenu();
        craftingMenuUI.CraftingTitleText.text = recipeDataBase.DatabaseName;
        craftingMenuUI.PopulateCraftingMenuUI(recipeDataBase);

        _menuReferenceHolder.SetMenuSourceGO(menuSourceGO);

        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void OpenChestMenu(List<InventoryItem> localChestItemData, GameObject menuSourceGO)
    {
        ClearOldMenu();
        
        ChestMenuUI chestMenuUI = _instantiateHandler.InstantiateChestMenu();
        chestMenuUI.PopulateChestMenuUI(localChestItemData);

        _menuReferenceHolder.SetMenuSourceGO(menuSourceGO);

        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void OpenNpcMenu()
    {
        ClearOldMenu();
        NpcMenuUI npcMenuUI = _instantiateHandler.InstantiateNpcMenu();

        // Npc Menu Set up

        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }

    public void ClearOldMenu()
    {
        Debug.Log("Clearing Old Menu");
        _menuReferenceHolder.ClearOldMenu();
    }
    
    private void OnDestroy()
    {
        GameInput.Instance.OnInventoryToggle -= OnInventoryToggleOff;
    }
}
