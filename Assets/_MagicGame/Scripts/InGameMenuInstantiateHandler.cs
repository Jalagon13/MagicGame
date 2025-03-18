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
        ClearOldMenu();
        GameObject craftingMenuUI = Instantiate(_inGameMenuReferenceHolder.CraftingMenuPrefab, transform);
        _inGameMenuReferenceHolder.SetMenu(craftingMenuUI);
        
        return craftingMenuUI.GetComponent<CraftingMenuUI>();
    }

    public ChestMenuUI InstantiateChestMenu()
    {
        ClearOldMenu();
        GameObject chestMenuUI = Instantiate(_inGameMenuReferenceHolder.ChestMenuPrefab, transform);
        _inGameMenuReferenceHolder.SetMenu(chestMenuUI);

        return chestMenuUI.GetComponent<ChestMenuUI>();
    }

    public NpcMenuUI InstantiateNpcMenu()
    {
        ClearOldMenu();
        GameObject npcMenuUI = Instantiate(_inGameMenuReferenceHolder.NpcMenuPrefab, transform);
        _inGameMenuReferenceHolder.SetMenu(npcMenuUI);

        return npcMenuUI.GetComponent<NpcMenuUI>();
    }
    
    private void ClearOldMenu()
    {
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
