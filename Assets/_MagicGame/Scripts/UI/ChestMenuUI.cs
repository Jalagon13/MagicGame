using System;
using System.Collections.Generic;
using UnityEngine;

public class ChestMenuUI : MonoBehaviour
{
    [field: SerializeField] public Transform ChestSlotsUITransform { get; private set; }

    private Vector2Int _chestPosition;
    private List<InventoryItem> _localChestItemData;

    private void Start()
    {
        ChestManager.Instance.OnChestUpdated += ChestManager_OnChestUpdated;
    }
    
    private void OnDisable()
    {
        ChestManager.Instance.OpenChestPosition = null;
        ChestManager.Instance.LocalChestItemData = null;
        ChestManager.Instance.IsChestOpen = false;
        ChestManager.Instance.CloseChest(_chestPosition, Player.LocalClientInstance.CurrentPlayerBiome.Value, _localChestItemData);
        ChestManager.Instance.OnChestUpdated -= ChestManager_OnChestUpdated;
    }

    private void ChestManager_OnChestUpdated(object sender, ChestManager.ChestEventArgs e)
    {
        PopulateChestMenuUI(e.ChestItemData, _chestPosition);
    }

    public void PopulateChestMenuUI(List<InventoryItem> localChestItemData, Vector2Int chestPosition)
    {
        _chestPosition = chestPosition;
        _localChestItemData = localChestItemData;
        
        ChestManager.Instance.LocalChestItemData = localChestItemData;
        ChestManager.Instance.IsChestOpen = true;
        ChestManager.Instance.OpenChestPosition = chestPosition;
        ChestManager.Instance.AddChestIdServerRpc(chestPosition, Player.LocalClientInstance.CurrentPlayerBiome.Value);

        foreach (Transform child in ChestSlotsUITransform)
        {
            int chestSlotIndex = child.GetSiblingIndex();

            child.GetComponent<InventorySlotUI>().InitializeInvSlotUI(chestSlotIndex, ChestManager.Instance.LocalChestItemData);
            child.GetComponent<InventorySlotUI>().UpdateDisplayUI(localChestItemData[chestSlotIndex]);
        }
    }
}