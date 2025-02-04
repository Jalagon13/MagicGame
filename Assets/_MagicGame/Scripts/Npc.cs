using System;
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
	[SerializeField] private MMF_Player _damageNumberFeedbacks;
	[SerializeField] private LootTable _lootTable;
	
	private NetworkVariable<int> _npcHealthPointNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Vector2 _damageSourcePosition;
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

	public void ApplyDamage(int damage, Vector2 damagerPosition)
	{
		DamageNpcServerRpc(damage, damagerPosition);
		
		// Set damage and play damage feedback
		MMF_FloatingText floatingText = _damageNumberFeedbacks.GetFeedbackOfType<MMF_FloatingText>();
		floatingText.Value = damage.ToString();
		_damageNumberFeedbacks.PlayFeedbacks(transform.position);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DamageNpcServerRpc(int damageAmount, Vector2 damagerPosition)
	{
		_damageSourcePosition = damagerPosition;
		_npcHealthPointNetworkVariable.Value -= damageAmount;
		
		if(_npcHealthPointNetworkVariable.Value <= 0)
		{
			OnNpcKilled?.Invoke(this, EventArgs.Empty);
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
		_lootTable.SpawnLoot(transform.position, GetComponent<NpcNetworkComponent>().NpcBiomeType);
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