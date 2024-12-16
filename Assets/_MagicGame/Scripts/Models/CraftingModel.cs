using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CraftingModel
{
    public event EventHandler OnCraftingDatabaseChanged;
    // Store a dynamic list of crafting databases that can change 
    private List<RecipeDataBaseObject> _craftingDatabase = new();
	
    public CraftingModel(RecipeDataBaseObject defaultDatabase)
    {
        // Initialize default database here
        _craftingDatabase.Add(defaultDatabase);
    }
	
    public void AddRecipeDatabase(RecipeDataBaseObject newDatabase)
    {
        if(!_craftingDatabase.Contains(newDatabase))
        {
            _craftingDatabase.Add(newDatabase);
            OnCraftingDatabaseChanged?.Invoke(this, EventArgs.Empty);
        }
    }
	
    public void RemoveRecipeDatabase(RecipeDataBaseObject databaseToRemove)
    {
        if(_craftingDatabase.Contains(databaseToRemove))
        {
            _craftingDatabase.Remove(databaseToRemove);
            OnCraftingDatabaseChanged?.Invoke(this, EventArgs.Empty);
        }
    }
	
    public List<RecipeDataBaseObject> GetCraftingDatabase()
    {
        return _craftingDatabase;
    }
}
