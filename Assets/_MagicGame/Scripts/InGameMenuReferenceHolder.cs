using UnityEngine;

public class InGameMenuReferenceHolder : MonoBehaviour
{
    [field: SerializeField] public GameObject CraftingMenuPrefab { get; private set; }
    [field: SerializeField] public GameObject ChestMenuPrefab { get; private set; }
    [field: SerializeField] public GameObject NpcMenuPrefab { get; private set; }
    
    private static GameObject _currentReference;
    
    public void SetMenu(GameObject menu)
    {
        _currentReference = menu;
    }
    
    private void Update()
    {
        // Handle proximity detection logic here
    }
}
