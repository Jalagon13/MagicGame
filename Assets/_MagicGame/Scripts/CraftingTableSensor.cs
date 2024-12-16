using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingTableSensor : MonoBehaviour
{
    // [SerializeField] private CraftingController _craftingController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        CraftingTable craftingTable = other.GetComponent<CraftingTable>();

        if(craftingTable != null)
        {
            CraftingManager.Instance.AddDataBase(craftingTable.GetCraftingRecipeDB());
        }
    }
	
    private void OnTriggerExit2D(Collider2D other)
    {
        CraftingTable craftingTable = other.GetComponent<CraftingTable>();

        if(craftingTable != null)
        {
            CraftingManager.Instance.RemoveDataBase(craftingTable.GetCraftingRecipeDB());
        }
    }
}
