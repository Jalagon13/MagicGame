using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class CraftingTableObject : WorldObject
{
    [Header("Crafting Table Parameters")]
    [field: SerializeField] public RecipeDataBaseObject CraftingRecipeDB { get; private set; }
    [field: SerializeField] public WorldInput WorldInput { get; private set; }
    
    private void Start()
    {
        GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
    }

    private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
    {
        var centerPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
        var playerInRange = Vector2.Distance(Player.LocalClientInstance.transform.position, centerPosition) <= _chestOpenDistance;
        
        if (WorldInput.IsMouseOverIndputDetector() && playerInRange)
        {
            InGameMenu.Instance.OpenCraftingMenu(CraftingRecipeDB, gameObject);
        }
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
    }
}
