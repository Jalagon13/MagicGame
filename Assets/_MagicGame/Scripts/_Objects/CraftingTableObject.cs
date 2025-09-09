using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class CraftingTableObject : ResourceObject
{
    [Header("Crafting Table Parameters")]
    [SerializeField] 
    private GameObject _craftingMenuUI;
    
    [SerializeField] 
    private WorldInput _worldInput;
    
    private void Start()
    {
        GameInput.Instance.OnSecondaryActionStarted += GameInput_OnSecondaryActionStarted;
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnSecondaryActionStarted -= GameInput_OnSecondaryActionStarted;
    }

    private void GameInput_OnSecondaryActionStarted(object sender, EventArgs e)
    {
        var centerPosition = new Vector2(transform.position.x + 0.5f, transform.position.y + 0.5f);
        var playerInRange = Vector2.Distance(Player.Instance.transform.position, centerPosition) <= ResourceDataSO.InteractDistance;
        
        if (_worldInput.IsMouseOverIndputDetector() && playerInRange && !Pointer.IsOverUI())
        {
            InGameMenu.Instance.OpenCraftingMenu(_craftingMenuUI, gameObject);
        }
    }
}
