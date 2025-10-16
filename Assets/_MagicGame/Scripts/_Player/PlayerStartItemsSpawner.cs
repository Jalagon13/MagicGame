using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ProjectTinker
{
    public class PlayerStartItemsSpawner : NetworkBehaviour
    {
        [SerializeField] private List<InventoryItem> _startingItems = new();

        private void Awake()
        {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }

        public override void OnDestroy()
        {
            Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
        }

        private void Player_OnAnyPlayerSpawned(object sender, Player.PlayerIdEventArgs e)
        {
            if (Player.Instance != null && e.PlayerId == Player.Instance.OwnerClientId)
            {
                foreach (InventoryItem item in _startingItems)
                {
                    InventoryItem itemToAdd = item.Item.CreateInventoryItem(item.Quantity);
                    InventoryManager.Instance.AddItem(itemToAdd, false);
                    // yield return new WaitForEndOfFrame();
                }
            }
        }
    }
}
