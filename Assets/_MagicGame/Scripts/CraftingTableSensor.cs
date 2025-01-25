using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingTableSensor : MonoBehaviour
{
    // [SerializeField] private CraftingController _craftingController;

    private void OnTriggerEnter2D(Collider2D other)
    {
        CraftingTableObject craftingTable = other.GetComponent<CraftingTableObject>();

        if(craftingTable != null)
        {
            CraftingManager.Instance.AddDataBase(craftingTable.GetCraftingRecipeDB());
        }
    }
	
    private void OnTriggerExit2D(Collider2D other)
    {
        CraftingTableObject craftingTable = other.GetComponent<CraftingTableObject>();

        if(craftingTable != null)
        {
            CraftingManager.Instance.RemoveDataBase(craftingTable.GetCraftingRecipeDB());
        }
    }
}
