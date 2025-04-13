using System;
using AdvancedTooltips.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellbookSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [field: SerializeField] public Image EquippedSpellbookImage { get; private set; }
    [field: SerializeField] public Image SpellbookIconImage { get; private set; }

    private bool _hovered;
    
    private void OnEnable()
    {
        InventoryManager.Instance.OnInventorySlotShiftLeftClicked += OnInventorySlotShiftLeftClicked_SpellBookShortCut;
    }
    
    private void OnDisable()
    {
        InventoryManager.Instance.OnInventorySlotShiftLeftClicked -= OnInventorySlotShiftLeftClicked_SpellBookShortCut;
    }

    private void OnInventorySlotShiftLeftClicked_SpellBookShortCut(object sender, InventoryManager.ShortCutInventoryItemEventArgs e)
    {
        if (e.InventoryItem is SpellbookInventoryItem wandInInv)
        {
            if (MagicManager.Instance.HasEquippedSpellBook)
            {
                InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = MagicManager.Instance.SwapEquippedSpellBook(wandInInv);
                InventoryManager.Instance.ShowInventoryItemTooltip(InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex]);
            }
            else
            {
                MagicManager.Instance.EquipSpellBook(wandInInv);
                InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new();
                Tooltip.HideUI();
            }

            UpdateSlotUI();
            InventoryManager.Instance.GetInventoryModel().UpdateInventory();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Get the item currently held by the mouse (if any)
            InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;

            if (MagicManager.Instance.HasEquippedSpellBook)
            {
                // If there's already a spellbook equipped
                if (mouseItem.HasItem)
                {
                    if (mouseItem is SpellbookInventoryItem mouseSpellbookInventoryItem)
                    {
                        // Swap the equipped armor with the armor held by the mouse
                        InventoryManager.Instance.GetMouseItem().MouseInventoryItem = MagicManager.Instance.SwapEquippedSpellBook(mouseSpellbookInventoryItem);
                    }
                }
                else
                {
                    if (GameInput.Instance.GetShiftHeldDown())
                    {
                        // If Shift is held, add the unequipped armor to the inventory
                        InventoryManager.Instance.AddItem(MagicManager.Instance.RemoveEquippedSpellBook());
                    }
                    else
                    {
                        // Otherwise, place the unequipped armor on the mouse
                        InventoryManager.Instance.GetMouseItem().MouseInventoryItem = MagicManager.Instance.RemoveEquippedSpellBook();
                    }

                    Tooltip.HideUI();
                }
            }
            else if (mouseItem.HasItem && mouseItem is SpellbookInventoryItem mouseSpellbookInventoryItem)
            {
                MagicManager.Instance.EquipSpellBook(mouseSpellbookInventoryItem);
                InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
                InventoryManager.Instance.ShowInventoryItemTooltip(mouseSpellbookInventoryItem);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (MagicManager.Instance.HasEquippedSpellBook)
            {
                InGameMenu.Instance.OpenSpellbookInspectorMenu(MagicManager.Instance.EquippedSpellBook);
                MagicManager.Instance.RemoveEquippedSpellBook();
            }
        }

        UpdateSlotUI();
        InventoryManager.Instance.GetInventoryModel().UpdateInventory();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;

        if (MagicManager.Instance.HasEquippedSpellBook)
        {
            Tooltip.ShowNew();
            InventoryManager.Instance.ShowInventoryItemTooltip(MagicManager.Instance.EquippedSpellBook);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;

        Tooltip.HideUI();
    }

    public void UpdateSlotUI()
    {
        // Enable or disable the icon based on whether armor is equipped
        EquippedSpellbookImage.enabled = MagicManager.Instance.HasEquippedSpellBook;
        SpellbookIconImage.enabled = !MagicManager.Instance.HasEquippedSpellBook;

        if (MagicManager.Instance.HasEquippedSpellBook)
        {
            // Update the icon to display the equipped armor's sprite
            EquippedSpellbookImage.sprite = MagicManager.Instance.EquippedSpellBook.Item.UiDisplay;

            if (_hovered)
            {
                Tooltip.ShowNew();
                InventoryManager.Instance.ShowInventoryItemTooltip(MagicManager.Instance.EquippedSpellBook);
            }
        }
    }
}
