using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStartItemsSpawner : NetworkBehaviour
{
    [SerializeField] private bool _spawnWandItems;
    [SerializeField] private List<WandInventoryItem> _startWandItems = new();
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
            if (_spawnWandItems)
            {
                foreach (WandInventoryItem wandInvItem in _startWandItems)
                {
                    if (wandInvItem.Item is not WandItemSO)
                    {
                        Debug.LogWarning($"{wandInvItem.Item} is not a wand. skipping it");
                        continue;
                    }

                    WandItemSO wandItemSO = wandInvItem.Item as WandItemSO;
                    WandInventoryItem wandItemToAdd = (WandInventoryItem)wandItemSO.CreateInventoryItem(1);

                    for (int i = 0; i < wandInvItem.MagicArray.Length; i++)
                    {
                        if (i < wandItemSO.Capacity)
                        {
                            wandItemToAdd.MagicArray[i] = wandInvItem.MagicArray[i];
                        }
                        else
                        {
                            Debug.LogWarning($"{wandInvItem.MagicArray[i].Name} being skipped because it is out of the index of {wandItemSO.Name}'s Capacity ({wandItemSO.Capacity})");
                        }
                    }

                    InventoryManager.Instance.AddItem(wandItemToAdd, false);
                    // yield return new WaitForEndOfFrame();
                }
            }

            foreach (InventoryItem item in _startingItems)
            {
                InventoryItem itemToAdd = item.Item.CreateInventoryItem(item.Quantity);
                InventoryManager.Instance.AddItem(itemToAdd, false);
                // yield return new WaitForEndOfFrame();
            }
        }
    }
}
