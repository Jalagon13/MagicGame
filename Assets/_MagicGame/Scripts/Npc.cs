using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NpcNetworkComponent))]
public class Npc : NetworkBehaviour, IHasHealth
{	
	public event EventHandler OnNpcKilled;
	public event EventHandler<OnNpcDamagedEventArgs> OnNpcDamged;
	public class OnNpcDamagedEventArgs : EventArgs
	{
		public Vector2 DamageSourcePosition;
	}
	public event EventHandler<IHasHealth.OnHealthUpdatedEventArgs> OnHealthUpdated;

	[SerializeField] private int _maxHealth;
	[Range(0, 100), SerializeField] private float _knockbackResist;
	[SerializeField] private bool _invincible = false;
	[SerializeField] private MMF_Player _damageNumberFeedbacks;
	[field: SerializeField] public List<Loot> Table { get; private set; }
	
	private NetworkVariable<int> _npcHealthPointNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Vector2 _damageSourcePosition;
	private Knockback _knockback;
	
	private void Awake()
	{
		_knockback = GetComponent<Knockback>();
	}
	
	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			_npcHealthPointNetworkVariable.Value = _maxHealth;
		}
		
		_npcHealthPointNetworkVariable.OnValueChanged += UpdateHealthUI;
		_npcHealthPointNetworkVariable.OnValueChanged += OnDamged;
		
		base.OnNetworkSpawn();
	}

	private void OnDamged(int previousValue, int newValue)
	{
		// If the new value is less than the previous value than this npc has been damaged
		if(newValue < previousValue)
		{
			OnNpcDamged?.Invoke(this, new OnNpcDamagedEventArgs
			{
				DamageSourcePosition = _damageSourcePosition
			});
		}
	}

	public void ApplyDamage(int damage, Vector2 damagerPosition, int knockbackForce)
	{
		DamageNpcServerRpc(damage, damagerPosition, knockbackForce);
		
		// Set damage and play damage feedback
		MMF_FloatingText floatingText = _damageNumberFeedbacks.GetFeedbackOfType<MMF_FloatingText>();
		floatingText.Value = damage.ToString();
		_damageNumberFeedbacks.PlayFeedbacks(transform.position);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DamageNpcServerRpc(int damageAmount, Vector2 damagerPosition, int knockbackForce)
	{
		_damageSourcePosition = damagerPosition;
		
		if(!_invincible)
		{
			_npcHealthPointNetworkVariable.Value -= damageAmount;
		}
		
		if(_npcHealthPointNetworkVariable.Value <= 0)
		{
			OnNpcKilled?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			_knockback.ApplyKnockback(damagerPosition, _knockbackResist, knockbackForce); // NTFS: Add knockback force
		}
	}
	
	private void UpdateHealthUI(int previousValue, int newValue)
	{
		OnHealthUpdated?.Invoke(this, new IHasHealth.OnHealthUpdatedEventArgs
		{
			PreviousValue = previousValue,
			NewValue = newValue,
			MaxValue = _maxHealth
		});
	}
	
	public void DropLoot()
	{
		LootTable.SpawnLoot(Table, transform.position, GetComponent<NpcNetworkComponent>().NpcBiomeType);
	}
	
	public void DestroySelf()
	{	
		NetworkObject.Despawn();
		Destroy(gameObject);
	}
	
	public bool IsDead()
	{
		return _npcHealthPointNetworkVariable.Value <= 0;
	}

	public override void OnNetworkDespawn()
	{
		_npcHealthPointNetworkVariable.OnValueChanged -= UpdateHealthUI;
		_npcHealthPointNetworkVariable.OnValueChanged -= OnDamged;
	
		base.OnNetworkDespawn();
	}
}