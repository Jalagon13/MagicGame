using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
	public static PlayerStats Instance { get; private set; }

	[field: SerializeField] public int BaseMana { get; private set; } = 50;
	[field: SerializeField] public int ManaRegenPerSecond { get; private set; } = 10;
	[field: SerializeField] public float BaseSpeed { get; private set; }
	[field: SerializeField] public float TurnSharpness { get; private set; }
	
	public float CurrentSpeed { get; private set; }
	public int CurrentMana { get; private set; }
	
	private NetworkHealthState _healthState;
	private List<int> _equippedArmorItemIdList = new();
	private float _speedModifier = 1f;
	private float _manaRegenAccumulator = 0f;
	
	private void Awake()
	{
		_healthState = GetComponent<NetworkHealthState>();
		CurrentMana = BaseMana;
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

		RegenerateMana();
	}

	private void RegenerateMana()
	{
		if (CurrentMana < BaseMana)
		{
			_manaRegenAccumulator += ManaRegenPerSecond * Time.deltaTime;

			int manaToRegen = Mathf.FloorToInt(_manaRegenAccumulator);

			if (manaToRegen > 0)
			{
				_manaRegenAccumulator -= manaToRegen;

				CurrentMana = Mathf.Min(CurrentMana + manaToRegen, BaseMana);
			}
		}
	}

	public void SubtractMana(int amount)
	{
		if (amount > 0) 
		{
			CurrentMana = Mathf.Max(CurrentMana - amount, 0);
		}
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
		}
		
		UpdatePlayerStats();
	}
	
	public void UnequipArmor(ArmorItemSO armor)
	{
		if (armor == null)
		{
			return;
		}

		int itemId = GameManager.Instance.GetItemIdFromItemSO(armor);

		if (_equippedArmorItemIdList.Contains(itemId))
		{
			_equippedArmorItemIdList.Remove(itemId);
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
