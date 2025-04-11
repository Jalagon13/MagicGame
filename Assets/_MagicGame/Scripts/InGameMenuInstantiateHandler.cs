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
        GameObject craftingMenuUI = Instantiate(_inGameMenuReferenceHolder.CraftingMenuPrefab, transform);
        
        return craftingMenuUI.GetComponent<CraftingMenuUI>();
    }

    public ChestMenuUI InstantiateChestMenu()
    {
        GameObject chestMenuUI = Instantiate(_inGameMenuReferenceHolder.ChestMenuPrefab, transform);

        return chestMenuUI.GetComponent<ChestMenuUI>();
    }

    public WandInspectorMenuUI InstantiateWandInspectorMenu()
    {
        GameObject wandInspectorMenuUI = Instantiate(_inGameMenuReferenceHolder.WandInspectorMenuPrefab, transform);

        return wandInspectorMenuUI.GetComponent<WandInspectorMenuUI>();
    }

    public NpcMenuUI InstantiateNpcMenu()
    {
        GameObject npcMenuUI = Instantiate(_inGameMenuReferenceHolder.NpcMenuPrefab, transform);

        return npcMenuUI.GetComponent<NpcMenuUI>();
    }
}
