using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
	public event EventHandler<ArmorChangedEventArgs> OnArmorEquipped;
	public event EventHandler<ArmorChangedEventArgs> OnArmorUnEquipped;
	public class ArmorChangedEventArgs : EventArgs 
	{
	    public ArmorItemSO ArmorItem;
	}

	public static PlayerStats Instance { get; private set; }

	[field: SerializeField] public int BaseMana { get; private set; } = 50;
	[field: SerializeField] public int ManaRegenPerSecond { get; private set; } = 10;
	[field: SerializeField] public int HealthRegenPerSecond { get; private set; } = 5;
	[field: SerializeField] public float BaseSpeed { get; private set; }
	[field: SerializeField] public float TurnSharpness { get; private set; }
	[field: Range(0, 1f)]
	[field: SerializeField] public float KnockbackResist { get; private set; }
	
	public float CurrentSpeed { get; private set; }
	public int CurrentMana { get; private set; }
	public bool ManaRegenBuffActive => _temporaryManaRegen.HasValue;
	public bool HealthRegenBuffActive => _temporaryHealthRegen.HasValue;
	
	private NetworkHealthState _healthState;
	private List<int> _equippedArmorItemIdList = new();
	private float _speedModifier = 1f;
	private float _manaRegenAccumulator = 0f;
	private float _healthRegenAccumulator = 0f;
	private int? _temporaryManaRegen = null;
	private Timer _manaBuffTimer;
	private int? _temporaryHealthRegen = null;
	private Timer _healthBuffTimer;
	
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

		_manaBuffTimer?.Tick(Time.deltaTime);
		_healthBuffTimer?.Tick(Time.deltaTime);

		RegenerateMana();
		RegenerateHealth();
	}

	private void RegenerateMana()
	{
		if (CurrentMana < BaseMana)
		{
			int regenRate = _temporaryManaRegen.HasValue ? _temporaryManaRegen.Value : ManaRegenPerSecond;
			_manaRegenAccumulator += regenRate * Time.deltaTime;

			int manaToRegen = Mathf.FloorToInt(_manaRegenAccumulator);

			if (manaToRegen > 0)
			{
				_manaRegenAccumulator -= manaToRegen;

				CurrentMana = Mathf.Min(CurrentMana + manaToRegen, BaseMana);
			}
		}
	}

	private void RegenerateHealth()
	{
	    if (_healthState == null || _healthState.IsDead || _healthState.HitPoints.Value >= _healthState.MaxHealth.Value) return;

	    int regenRate = _temporaryHealthRegen.HasValue ? _temporaryHealthRegen.Value : HealthRegenPerSecond;
	    _healthRegenAccumulator += regenRate * Time.deltaTime;

	    int healthToRegen = Mathf.FloorToInt(_healthRegenAccumulator);

	    if (healthToRegen > 0)
	    {
	        _healthRegenAccumulator -= healthToRegen;
			_healthState.HealRpc(healthToRegen);
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
			OnArmorEquipped?.Invoke(this, new ArmorChangedEventArgs { ArmorItem = armor });
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
			OnArmorUnEquipped?.Invoke(this, new ArmorChangedEventArgs { ArmorItem = armor });
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
	
	public void ApplyManaRegenBuff(int manaPerSecond, float duration)
	{
		_temporaryManaRegen = manaPerSecond;
		_manaBuffTimer = new(duration);
		_manaBuffTimer.OnTimerEnd += EndManaRegenBuff;
	}

	public void ApplyHealthRegenBuff(int healthPerSecond, float duration)
	{
		_temporaryHealthRegen = healthPerSecond;
		_healthBuffTimer = new(duration);
		_healthBuffTimer.OnTimerEnd += EndHealthRegenBuff;
	}

    private void EndManaRegenBuff(object sender, EventArgs e)
	{
		_manaBuffTimer.OnTimerEnd -= EndManaRegenBuff;
		_temporaryManaRegen = null;
	}

	private void EndHealthRegenBuff(object sender, EventArgs e)
	{
		_healthBuffTimer.OnTimerEnd -= EndHealthRegenBuff;
		_temporaryHealthRegen = null;
	}

    public override void OnDestroy()
	{
		base.OnDestroy();
	}
}
