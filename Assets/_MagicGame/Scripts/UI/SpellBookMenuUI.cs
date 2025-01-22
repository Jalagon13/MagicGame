using System;
using UnityEngine;

public class SpellBookMenuUI : MonoBehaviour
{
	[SerializeField] private SpellBookInventorySlotUI _spellBookInventorySlotPrefab;
	[SerializeField] private Transform _spellBookSlotsHolder;

	public SpellBookInventoryItem SelectedSpellBook { get; private set; }

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
		Debug.Log($"Placed spellbook: {newSpellBook.Item.Name}");
		
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