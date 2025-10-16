using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace ProjectTinker
{
    public class ActionManager : MonoBehaviour
    {
        public static ActionManager Instance { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        public static Vector2 PlayerToMouseDirNormalized { get; private set; }
        public static Vector3Int MouseTilePosition { get; private set; }

        private Timer _itemActionTimer;
        private Transform _mouseTriggerTf;

        private void Awake()
        {
            Instance = this;
            _mouseTriggerTf = transform.GetChild(1).transform;
            _mouseTriggerTf.parent = null;

            _itemActionTimer = new Timer(0.25f);
        }

        private void Update()
        {
            if (Player.Instance == null) return;

            MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            MouseTilePosition = Vector3Int.FloorToInt(MouseWorldPosition);
            PlayerToMouseDirNormalized = (MouseWorldPosition - (Vector2)Player.Instance.transform.position).normalized;
            _mouseTriggerTf.position = MouseWorldPosition;

            TickTimers(Time.deltaTime);
            HandleItemActionExecutions();
        }

        private void HandleItemActionExecutions()
        {
            if (Player.Instance == null || Player.Instance.ServerCharacter.LifeState == LifeState.Dead || Pointer.IsOverUI() || !GameInput.Instance.GetInputsEnabled()) return;

            if (GameInput.Instance.GetPrimaryHeldDown() && InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem))
            {
                if (_itemActionTimer.RemainingSeconds <= 0)
                {
                    _itemActionTimer.RemainingSeconds = selectedInventoryItem.Item.ExecuteItemAction(selectedInventoryItem, Player.Instance.PlayerHand);
                }
            }
        }

        private void TickTimers(float deltaTile)
        {
            _itemActionTimer.Tick(deltaTile);
        }
    }
}
