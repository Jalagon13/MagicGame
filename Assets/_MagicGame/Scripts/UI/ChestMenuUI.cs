using System;
using System.Collections.Generic;
using UnityEngine;

public class ChestMenuUI : MonoBehaviour
{
    [field: SerializeField] public Transform ChestSlotsUITransform { get; private set; }

    private void Start()
    {
        ChestManager.Instance.OnChestUpdated += ChestManager_OnChestUpdated;
    }

    private void ChestManager_OnChestUpdated(object sender, ChestManager.ChestEventArgs e)
    {
        Debug.Log("Updating Chest Menu UI");
        PopulateChestMenuUI(e.ChestItemData);
    }

    public void PopulateChestMenuUI(List<InventoryItem> localChestItemData)
    {
        Debug.Log("Populating Chest Menu UI");
        foreach (Transform child in ChestSlotsUITransform)
        {
            int chestSlotIndex = child.GetSiblingIndex();

            child.GetComponent<InventorySlotUI>().InitializeInvSlotUI(chestSlotIndex, ChestManager.Instance.GetOpenChestInventoryItems());
            child.GetComponent<InventorySlotUI>().UpdateDisplayUI(localChestItemData[chestSlotIndex]);
        }
    }

    private void OnDestroy()
    {
        ChestManager.Instance.OnChestUpdated -= ChestManager_OnChestUpdated;
    }
}