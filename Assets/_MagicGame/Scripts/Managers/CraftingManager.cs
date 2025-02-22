using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
	public event EventHandler<OnCraftingDataUpdatedEventArgs> OnCraftingDataUpdated;
	public class OnCraftingDataUpdatedEventArgs : EventArgs 
	{
		public List<RecipeDataBaseObject> CraftingDatabase;
	}

	public static CraftingManager Instance { get; private set; }
	
	[SerializeField] private RecipeDataBaseObject _defaultRecipeDatabase; 
	
	private CraftingModel _craftingModel;
	
	private void Awake()
	{
		Instance = this;
		
		_craftingModel = new(_defaultRecipeDatabase);
	}
	
	private void Start()
	{
		_craftingModel.OnCraftingDatabaseChanged += CraftingModel_OnCraftingDatabaseChanged;
		
		InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
	}

	private void CraftingModel_OnCraftingDatabaseChanged(object sender, EventArgs e)
	{
		RefreshCraftingMenu();
	}

	private void InventoryManager_OnInventoryUpdated(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
	{
		RefreshCraftingMenu();
	}

	public void RefreshCraftingMenu()
	{
		OnCraftingDataUpdated?.Invoke(this, new OnCraftingDataUpdatedEventArgs
		{
			CraftingDatabase = _craftingModel.GetCraftingDatabase()
		});
	}
	
	// When crafting table is moved into range
	public void AddDataBase(RecipeDataBaseObject newDataBase)
	{
		_craftingModel.AddRecipeDatabase(newDataBase);
	}
	
	// When crafting table is moved out of range
	public void RemoveDataBase(RecipeDataBaseObject dataBaseToRemove)
	{
		_craftingModel.RemoveRecipeDatabase(dataBaseToRemove);
	}
	
	private void OnDestroy()
	{
		_craftingModel.OnCraftingDatabaseChanged -= CraftingModel_OnCraftingDatabaseChanged;
		InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
	}
}
