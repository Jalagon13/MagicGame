using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public abstract class ItemSO : ScriptableObject
{
	[field: SerializeField] public string Name { get; private set; }
	[field: SerializeField] public Sprite UiDisplay { get; private set; }
	[field: SerializeField] public float ItemActionCooldown { get; private set; } = 0.375f; // Cooldown between action executions
	[field: SerializeField] public bool Stackable { get; private set; } = true;
	[field: TextArea]
	[field: SerializeField] public string Description { get; private set; }
	
	protected float _baseActionCooldown = 0.25f;
	
	public abstract InventoryItem CreateInventoryItem(int quantity);
	public abstract float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand);
	public abstract string GetDescription();
	
	// Returns description with line breaks
	protected string GetDescriptionBreak() 
	{
		string description = "";
		if (!string.IsNullOrWhiteSpace(Description))
			description += $"{Description}<br>";

		return description;
	}
}