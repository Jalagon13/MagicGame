using UnityEngine;

[RequireComponent(typeof(InGameMenuReferenceHolder))]
public class InGameMenu : MonoBehaviour
{
    public InGameMenu Instance { get; private set; }
    
    private InGameMenuReferenceHolder _menuReferenceHolder;
    private InGameMenuInstantiateHandler _instantiateHandler;
    
    private void Awake()
    {
        Instance = this;
        _menuReferenceHolder = GetComponent<InGameMenuReferenceHolder>();
        _instantiateHandler = GetComponent<InGameMenuInstantiateHandler>();
    }
    
    public void CraftingMenu()
    {
        CraftingMenuUI craftingMenuUI = _instantiateHandler.InstantiateCraftingMenu();
        
        // Crafting Menu Set up
    }
    
    public void ChestMenu()
    {
        ChestMenuUI chestMenuUI = _instantiateHandler.InstantiateChestMenu();
        
        // Chest Menu Set up
    }
    
    public void NpcMenu()
    {
        NpcMenuUI npcMenuUI = _instantiateHandler.InstantiateNpcMenu();
        
        // Npc Menu Set up
    }
}
