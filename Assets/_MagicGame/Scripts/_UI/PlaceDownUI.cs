using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace ProjectWizard
{
    public class PlaceDownUI : MonoBehaviour
    {
        private ItemDataSO _focusItemSO;
        private SpriteRenderer _sr;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnFocusItemSet;
        }

        private void HotbarManager_OnFocusItemSet(object sender, HotbarManager.OnFocusItemSetEventArgs e)
        {
            _focusItemSO = GameDataRegistry.Instance.GetItemDataFromItemId(e.SelectedItemId);

            if (_focusItemSO == null)
            {
                HideIndicator();
                return;
            }
        }

        private void FixedUpdate()
        {
            Vector2 parentPos = ActionManager.MouseWorldPosition;
            Vector2 indicatorPosition = new(Mathf.FloorToInt(parentPos.x), Mathf.FloorToInt(parentPos.y));
            transform.position = indicatorPosition + new Vector2(0.5f, 0.5f);

            if (_focusItemSO == null) return;

            if ((_focusItemSO is DeployItemSO || _focusItemSO is TileItemSO || _focusItemSO is NpcItemSO) && IsClear(transform.position) && IsInRange())
            {
                ShowIndicator();
            }
            else
            {
                HideIndicator();
            }
        }

        private bool IsInRange()
        {
            if (Player.Instance == null) return false;

            return Vector2.Distance(transform.position, Player.Instance.transform.position) < 3;
        }


        public bool PlayerInRangeOfMouse()
        {
            return Vector2.Distance(Player.Instance.transform.position, ActionManager.MouseWorldPosition) <= 3;
        }

        private bool IsClear(Vector2 position)
        {
            Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);

            foreach (Collider2D col in colliders)
            {
                if (col.TryGetComponent(out ResourceObject clickable))
                    return false;
            }

            return true;
        }

        private void ShowIndicator()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.enabled = true;
        }

        private void HideIndicator()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.enabled = false;
        }

        private void OnDestroy()
        {
            HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnFocusItemSet;
        }
    }
}
