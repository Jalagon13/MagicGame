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
            if (SpellManager.Instance.HasEquippedSpellBook)
            {
                InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = SpellManager.Instance.SwapEquippedSpellBook(wandInInv);
                InventoryManager.Instance.ShowInventoryItemTooltip(InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex]);
            }
            else
            {
                SpellManager.Instance.EquipSpellBook(wandInInv);
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

            if (SpellManager.Instance.HasEquippedSpellBook)
            {
                // If there's already a spellbook equipped
                if (mouseItem.HasItem)
                {
                    if (mouseItem is SpellbookInventoryItem mouseSpellbookInventoryItem)
                    {
                        // Swap the equipped armor with the armor held by the mouse
                        InventoryManager.Instance.GetMouseItem().MouseInventoryItem = SpellManager.Instance.SwapEquippedSpellBook(mouseSpellbookInventoryItem);
                    }
                }
                else
                {
                    if (GameInput.Instance.GetShiftHeldDown())
                    {
                        // If Shift is held, add the unequipped armor to the inventory
                        InventoryManager.Instance.AddItem(SpellManager.Instance.RemoveEquippedSpellBook());
                    }
                    else
                    {
                        // Otherwise, place the unequipped armor on the mouse
                        InventoryManager.Instance.GetMouseItem().MouseInventoryItem = SpellManager.Instance.RemoveEquippedSpellBook();
                    }

                    Tooltip.HideUI();
                }
            }
            else if (mouseItem.HasItem && mouseItem is SpellbookInventoryItem mouseSpellbookInventoryItem)
            {
                SpellManager.Instance.EquipSpellBook(mouseSpellbookInventoryItem);
                InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
                InventoryManager.Instance.ShowInventoryItemTooltip(mouseSpellbookInventoryItem);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (SpellManager.Instance.HasEquippedSpellBook)
            {
                InGameMenu.Instance.OpenSpellbookInspectorMenu(SpellManager.Instance.EquippedSpellBook);
                SpellManager.Instance.RemoveEquippedSpellBook();
            }
        }

        UpdateSlotUI();
        InventoryManager.Instance.GetInventoryModel().UpdateInventory();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;

        if (SpellManager.Instance.HasEquippedSpellBook)
        {
            Tooltip.ShowNew();
            InventoryManager.Instance.ShowInventoryItemTooltip(SpellManager.Instance.EquippedSpellBook);
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
        EquippedSpellbookImage.enabled = SpellManager.Instance.HasEquippedSpellBook;
        SpellbookIconImage.enabled = !SpellManager.Instance.HasEquippedSpellBook;

        if (SpellManager.Instance.HasEquippedSpellBook)
        {
            // Update the icon to display the equipped armor's sprite
            EquippedSpellbookImage.sprite = SpellManager.Instance.EquippedSpellBook.Item.UiDisplay;

            if (_hovered)
            {
                Tooltip.ShowNew();
                InventoryManager.Instance.ShowInventoryItemTooltip(SpellManager.Instance.EquippedSpellBook);
            }
        }
    }
}
