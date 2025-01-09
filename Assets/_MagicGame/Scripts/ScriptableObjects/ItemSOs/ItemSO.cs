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
	[field: SerializeField] public GameObject ProjectileForm { get; private set; }
	[field: SerializeField] public int ProjectileDamage { get; private set; }
	[field: SerializeField] public bool Stackable { get; private set; }
	[field: TextArea]
	[field: SerializeField] public string Description { get; private set; }
	[field: SerializeField] public List<ItemParameter> DefaultParameterList { get; set; }
	
	public abstract InventoryItem CreateInventoryItem(int quantity);
	public abstract void ExecutePrimaryAction(InventoryItem inventoryItem);
	public abstract void ExecuteSecondaryAction(InventoryItem inventoryItem);
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