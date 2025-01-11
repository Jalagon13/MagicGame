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
	[field: SerializeField] public List<ItemParameter> DefaultParameterList { get; set; }
	
	protected float _baseActionCooldown = 0.25f;
	
	public abstract InventoryItem CreateInventoryItem(int quantity);
	public abstract float ExecuteItemAction(InventoryItem inventoryItem);
	public abstract string GetDescription();
	
	public float ExtractParameterValue(ItemParameter paramter)
	{
		if (DefaultParameterList.Contains(paramter))
		{
			int index = DefaultParameterList.IndexOf(paramter);
			return DefaultParameterList[index].Value;
		}
		
		return 0;
	}
	
	// Returns description with line breaks
	protected string GetDescriptionBreak() 
	{
		string description = "";
		if (!string.IsNullOrWhiteSpace(Description))
			description += $"{Description}<br>";

		return description;
	}
}

[Serializable]
public struct ItemParameter : IEquatable<ItemParameter>
{
	public ItemParameterSO Parameter;
	public float Value;

	public bool Equals(ItemParameter other)
	{
		return other.Parameter == Parameter;
	}
}