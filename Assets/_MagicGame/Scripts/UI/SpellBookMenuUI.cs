using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellBookMenuUI : MonoBehaviour
{
	public SpellBookInventoryItem SelectedSpellBook { get; private set; }
	
	[SerializeField] private SpellBookInventorySlotUI _spellBookInventorySlotPrefab;
	[SerializeField] private Transform _spellBookSlotsHolder;
	[SerializeField] private SpellBookSlotUI _spellBookSlotUI;
	
	private void OnEnable()
	{
		InventoryManager.Instance.OnInventorySlotShiftLeftClicked += SpellBookShortCut;
	}

	private void OnDisable()
	{
		InventoryManager.Instance.OnInventorySlotShiftLeftClicked -= SpellBookShortCut;
		
		if(HasSpellBook())
		{
			InventoryManager.Instance.AddItem(SelectedSpellBook);
		}
		
		RemoveSelectedSpellBook();
	}
	
	private void SpellBookShortCut(object sender, InventoryManager.ShortCutInventoryItemEventArgs e)
	{
		if(e.InventoryItem is SpellBookInventoryItem spellBookInInventory)
		{
			if(HasSpellBook())
			{
				InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = SwapSpellBooks(spellBookInInventory);
			}
			else
			{
				PlaceSelectedSpellBook(spellBookInInventory);
				InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new();
			}
			
			_spellBookSlotUI.UpdateSlotUI();
		}
		else if(e.InventoryItem.Item is SpellProjectileItemSO spellProjectileItemSO)
		{
			Debug.Log("a");
			if(HasSpellBook())
			{
				Debug.Log("b");
				SpellBookInventorySlotUI firstEmptySpellBookInventorySlotUI = null;
			
				foreach (Transform child in _spellBookSlotsHolder)
				{
					if(!child.GetComponent<SpellBookInventorySlotUI>().SpellBookInventorySlotIsOccupied())
					{
						Debug.Log("c");
						firstEmptySpellBookInventorySlotUI = child.GetComponent<SpellBookInventorySlotUI>();
						break;
					}
				}
				
				if(firstEmptySpellBookInventorySlotUI != null)
				{
					Debug.Log("d");
					// Found an empty spot
					firstEmptySpellBookInventorySlotUI.SetSpell(spellProjectileItemSO);
					InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new();
				}
			}
		}
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
	
	public bool HasSpellBook()
	{
		return SelectedSpellBook != null;
	}

	public SpellBookInventoryItem RemoveSelectedSpellBook()
	{
		SpellBookInventoryItem removedSpellBook = SelectedSpellBook;

		SelectedSpellBook = null;

		RemoveUI();
		
		return removedSpellBook;
	}

	public void PlaceSelectedSpellBook(SpellBookInventoryItem newSpellBook)
	{
		if (newSpellBook == null)
		{
			throw new ArgumentNullException(nameof(newSpellBook), "Cannot place a null spellbook.");
		}

		SelectedSpellBook = newSpellBook;
		Debug.Log(SelectedSpellBook == null);
		Debug.Log($"Placed spellbook: {SelectedSpellBook.Item.Name}, {SelectedSpellBook.Item.UiDisplay}");
		
		UpdateSpellBookSlotsUI();
	}

	public SpellBookInventoryItem SwapSpellBooks(SpellBookInventoryItem newSpellBook)
	{
		if (newSpellBook == null)
		{
			throw new ArgumentNullException(nameof(newSpellBook), "Cannot swap with a null spellbook.");
		}

		// Store the currently selected spellbook
		SpellBookInventoryItem previousSpellBook = SelectedSpellBook;

		// Replace the selected spellbook with the new one
		SelectedSpellBook = newSpellBook;

		Debug.Log($"Swapped spellbook. New selected spellbook: {SelectedSpellBook.Item.Name}");

		// Update the UI
		UpdateSpellBookSlotsUI();

		// Return the previous spellbook
		return previousSpellBook;
	}
	
	private void UpdateSpellBookSlotsUI()
	{
		// Clear existing craft nodes
		RemoveUI();
		
		// Create new craft nodes based on the crafting model's recipe list
		if (SelectedSpellBook != null)
		{
			for (int i = 0; i < SelectedSpellBook.SpellsArray.Length; i++)
			{
				SpellBookInventorySlotUI spellBookInventorySlot = Instantiate(_spellBookInventorySlotPrefab, _spellBookSlotsHolder);
				spellBookInventorySlot.Initialize(SelectedSpellBook, i);
			}
		}
	}
	
	private void RemoveUI()
	{
		if (_spellBookSlotsHolder.childCount != 0)
		{
			foreach (Transform child in _spellBookSlotsHolder)
			{
				Destroy(child.gameObject);
			}	
		}
	}
}