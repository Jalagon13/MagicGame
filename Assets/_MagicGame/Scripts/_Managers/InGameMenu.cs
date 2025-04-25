using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InGameMenuReferenceHolder))]
public class InGameMenu : MonoBehaviour
{
    public static InGameMenu Instance { get; private set; }
    public event EventHandler OnMenuClose;
    public event EventHandler OnMenuOpen;
    
    [field: SerializeField] public GameObject DefaultCraftingMenu { get; private set; }
    
    private InGameMenuReferenceHolder _menuReferenceHolder;
    private InGameMenuInstantiateHandler _instantiateHandler;
    private bool _menuOpen => transform.childCount > 0;
    
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
        if (e.InventoryOpen)
        {
            if (!_menuOpen)
            {
                Debug.Log("Opening default menu");
                DefaultCraftingMenu.SetActive(true);
            }
        }
        else
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
        Debug.Log("Closing default menu");
        DefaultCraftingMenu.SetActive(false);
        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void OpenChestMenu(List<InventoryItem> localChestItemData, GameObject menuSourceGO, Vector2Int chestPosition)
    {
        ClearOldMenu();
        
        ChestMenuUI chestMenuUI = _instantiateHandler.InstantiateChestMenu();
        chestMenuUI.PopulateChestMenuUI(localChestItemData, chestPosition);

        _menuReferenceHolder.SetMenuSourceGO(menuSourceGO);
        Debug.Log("Closing default menu");
        DefaultCraftingMenu.SetActive(false);
        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void OpenSpellbookInspectorMenu(SpellbookInventoryItem wand)
    {
        ClearOldMenu();
        
        SpellbookInspectorMenuUI spellbookInspectorMenuUI = _instantiateHandler.InstantiateWandInspectorMenu();
        spellbookInspectorMenuUI.PlaceSelectedWand(wand);

        _menuReferenceHolder.SetMenuSourceGO(Player.LocalClientInstance.gameObject);
        Debug.Log("Closing default menu");
        DefaultCraftingMenu.SetActive(false);
        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void OpenNpcMenu(List<ItemSO> itemsToSell, GameObject menuSourceGO)
    {
        ClearOldMenu();
        
        NpcMenuUI npcMenuUI = _instantiateHandler.InstantiateNpcMenu();
        npcMenuUI.SetItemsToSell(itemsToSell);
        npcMenuUI.InitializeSellSlots();

        _menuReferenceHolder.SetMenuSourceGO(menuSourceGO);
        Debug.Log("Closing default menu");
        DefaultCraftingMenu.SetActive(false);
        OnMenuOpen?.Invoke(this, EventArgs.Empty);
    }
    
    public void InvokeOnMenuClose()
    {
        DefaultCraftingMenu.SetActive(true);
        OnMenuClose?.Invoke(this, EventArgs.Empty);
    }

    public void ClearOldMenu()
    {
        DefaultCraftingMenu.SetActive(true);
        _menuReferenceHolder.ClearOldMenu();
    }
    
    private void OnDestroy()
    {
        GameInput.Instance.OnInventoryToggle -= OnInventoryToggleOff;
    }
}
