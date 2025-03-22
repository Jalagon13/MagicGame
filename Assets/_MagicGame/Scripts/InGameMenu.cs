using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InGameMenuReferenceHolder))]
public class InGameMenu : MonoBehaviour
{
    public event EventHandler OnMenuClose;
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
    
    public void OpenChestMenu(List<InventoryItem> localChestItemData, GameObject menuSourceGO, Vector2Int chestPosition)
    {
        ClearOldMenu();
        
        ChestMenuUI chestMenuUI = _instantiateHandler.InstantiateChestMenu();
        chestMenuUI.PopulateChestMenuUI(localChestItemData, chestPosition);

        _menuReferenceHolder.SetMenuSourceGO(menuSourceGO);

        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void OpenWandInspectorMenu(WandInventoryItem wand)
    {
        ClearOldMenu();
        
        WandInspectorMenuUI wandInspectorMenuUI = _instantiateHandler.InstantiateWandInspectorMenu();
        wandInspectorMenuUI.PlaceSelectedWand(wand);

        _menuReferenceHolder.SetMenuSourceGO(Player.LocalClientInstance.gameObject);

        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void OpenNpcMenu(List<ItemSO> itemsToSell, GameObject menuSourceGO)
    {
        ClearOldMenu();
        NpcMenuUI npcMenuUI = _instantiateHandler.InstantiateNpcMenu();
        npcMenuUI.SetItemsToSell(itemsToSell);
        npcMenuUI.InitializeSellSlots();

        _menuReferenceHolder.SetMenuSourceGO(menuSourceGO);

        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void InvokeOnMenuClose()
    {
        OnMenuClose?.Invoke(this, EventArgs.Empty);
    }

    public void ClearOldMenu()
    {
        _menuReferenceHolder.ClearOldMenu();
    }
    
    private void OnDestroy()
    {
        GameInput.Instance.OnInventoryToggle -= OnInventoryToggleOff;
    }
}
