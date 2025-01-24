using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
	[field: SerializeField] public int StartingPlayerHealth { get; private set; } = 100;
	public int PlayerDefense { get; private set; }
	
	private List<int> _equippedArmorItemIdList = new();
	
	public void EquipArmor(ArmorItemSO armor)
	{
		int itemId = GameManager.Instance.GetItemIdFromItemSO(armor);
	
		if(!_equippedArmorItemIdList.Contains(itemId))
		{
			_equippedArmorItemIdList.Add(itemId);
			Debug.Log($"Armor Equipped: {armor.Name}");
		}
		
		UpdatePlayerStats();
	}
	
	public void UnequipArmor(ArmorItemSO armor)
	{
		if (armor == null)
		{
			Debug.LogWarning("Attempted to unequip null armor.");
			return;
		}

		int itemId = GameManager.Instance.GetItemIdFromItemSO(armor);

		if (_equippedArmorItemIdList.Contains(itemId))
		{
			_equippedArmorItemIdList.Remove(itemId);
			Debug.Log($"Armor Unequipped: {armor.Name}");
		}
		else
		{
			Debug.LogWarning($"Armor not found in equipped list: {armor.Name}");
		}

		UpdatePlayerStats();
	}

	private void UpdatePlayerStats()
	{
		PlayerDefense = 0;
	
		foreach (int armorId in _equippedArmorItemIdList)
		{
			var armor = GameManager.Instance.GetItemSOFromItemId(armorId) as ArmorItemSO;
			PlayerDefense += armor.DefenseAmount;
		}
		
		Debug.Log($"Player Defense: {PlayerDefense}");
	}
}
