using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
	public static PlayerStats Instance { get; private set; }

	[field: SerializeField] public float BaseMana { get; private set; } = 50f;
	[field: SerializeField] public float BaseSpeed { get; private set; }
	[field: SerializeField] public float TurnSharpness { get; private set; }
	
	public float CurrentSpeed { get; private set; }
	
	private List<int> _equippedArmorItemIdList = new();
	private float _speedModifier = 1f;
	private NetworkHealthState _healthState;
	
	private void Awake()
	{
		_healthState = GetComponent<NetworkHealthState>();
	}

    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
			Instance = this;
		}
    }
    
    private void Start()
    {
		CurrentSpeed = BaseSpeed;
	}

    private void Update()
	{
		CurrentSpeed = BaseSpeed * _speedModifier;
	}
	
	public void ApplySpeedModifier(float modifier)
	{
		_speedModifier = modifier;
	}
	
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
		int currentDefense = 0;
	
		foreach (int armorId in _equippedArmorItemIdList)
		{
			var armor = GameManager.Instance.GetItemSOFromItemId(armorId) as ArmorItemSO;
			currentDefense += armor.DefenseAmount;
		}

		_healthState.SetCurrentDefenseRpc(currentDefense);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
	}
}
