using UnityEngine;

public class InGameMenuReferenceHolder : MonoBehaviour
{
    [field: SerializeField] public GameObject CraftingMenuPrefab { get; private set; }
    [field: SerializeField] public GameObject ChestMenuPrefab { get; private set; }
    [field: SerializeField] public GameObject WandInspectorMenuPrefab { get; private set; }
    [field: SerializeField] public GameObject NpcMenuPrefab { get; private set; }
    [Tooltip("The distance at which the menu will be cleared if the player moves away")]
    [field: SerializeField] public float MenuSourceDistanceCheck { get; private set; } = 2.75f;

    private static GameObject _currentMenuGO;
    private bool _hasMenuBeenCleared;

    public void SetMenuSourceGO(GameObject menuSourceObject)
    {
        _currentMenuGO = menuSourceObject;
        _hasMenuBeenCleared = false; // Reset flag when a new menu is set
    }

    private void Update()
    {
        if (_currentMenuGO != null)
        {
            // While the menuGO still exists, check proximity
            Vector2 playerPosition = Player.Instance.transform.position;
            Vector2 offSetPosition = new Vector2(_currentMenuGO.transform.position.x + 0.5f, _currentMenuGO.transform.position.y + 0.5f);
            float distanceToPlayer = Vector2.Distance(playerPosition, offSetPosition);

            if (distanceToPlayer > MenuSourceDistanceCheck)
            {
                if (!_hasMenuBeenCleared)
                    ClearOldMenu();
            }
        }
        else if (!_hasMenuBeenCleared)
        {
            // The menu object was destroyed this frame
            ClearOldMenu();
        }
    }

    public void ClearOldMenu()
    {
        _hasMenuBeenCleared = true;
        _currentMenuGO = null;

        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
        
        InGameMenu.Instance.InvokeOnMenuClose();
    }
}