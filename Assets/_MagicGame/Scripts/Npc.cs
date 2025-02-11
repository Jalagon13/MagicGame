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

	[SerializeField] private bool _invincible = false;
	[SerializeField] private int _maxHealth;
	[Range(0, 100), SerializeField] private float _knockbackResist;
	[SerializeField] private int _damage;
	[field: SerializeField] public float IFrameLength { get; private set; } = 0.166f;
	[SerializeField] private DamageCollider _damageCollider;
	[SerializeField] private MMF_Player _damageNumberFeedbacks;
	[field: SerializeField] public List<Loot> Table { get; private set; }
	
	private NetworkVariable<int> _npcHealthPointNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Vector2 _damageSourcePosition;
	private Knockback _knockback;
	private Timer _iFrameTimer;
	
	private void Awake()
	{
		_knockback = GetComponent<Knockback>();
		
		
		if(_damageCollider != null)
		{
			_damageCollider.AddDamageExceptionCollider(GetComponent<Collider2D>());
			_damageCollider.DamageAmount = _damage;
		}
	}
	
	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			_npcHealthPointNetworkVariable.Value = _maxHealth;
			_iFrameTimer = new(IFrameLength);
		}
		
		_npcHealthPointNetworkVariable.OnValueChanged += UpdateHealthUI;
		_npcHealthPointNetworkVariable.OnValueChanged += OnDamged;
		
		base.OnNetworkSpawn();
	}
	
	private void Update()
	{
		if(IsServer)
		{
			_iFrameTimer.Tick(Time.deltaTime);
		}
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
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DamageNpcServerRpc(int damageAmount, Vector2 damagerPosition, int knockbackForce)
	{
		if (_iFrameTimer.RemainingSeconds > 0)
		{
			return;
		}
	
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
			_iFrameTimer.RemainingSeconds = IFrameLength;
		}
		
		// Set damage and play damage feedback
		MMF_FloatingText floatingText = _damageNumberFeedbacks.GetFeedbackOfType<MMF_FloatingText>();
		floatingText.Value = damageAmount.ToString();
		_damageNumberFeedbacks.PlayFeedbacks(transform.position);
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