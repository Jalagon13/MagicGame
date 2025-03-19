using UnityEngine;

public class InGameMenuInstantiateHandler : MonoBehaviour
{
    private InGameMenuReferenceHolder _inGameMenuReferenceHolder;
    
    private void Awake()
    {
        _inGameMenuReferenceHolder = GetComponent<InGameMenuReferenceHolder>();
    }
    
    public CraftingMenuUI InstantiateCraftingMenu()
    {
        Debug.Log("Instantiating Crafting Menu UI");
        GameObject craftingMenuUI = Instantiate(_inGameMenuReferenceHolder.CraftingMenuPrefab, transform);
        
        return craftingMenuUI.GetComponent<CraftingMenuUI>();
    }

    public ChestMenuUI InstantiateChestMenu()
    {
        GameObject chestMenuUI = Instantiate(_inGameMenuReferenceHolder.ChestMenuPrefab, transform);

        return chestMenuUI.GetComponent<ChestMenuUI>();
    }

    public NpcMenuUI InstantiateNpcMenu()
    {
        GameObject npcMenuUI = Instantiate(_inGameMenuReferenceHolder.NpcMenuPrefab, transform);

        return npcMenuUI.GetComponent<NpcMenuUI>();
    }
}
