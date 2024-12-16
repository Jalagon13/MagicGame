using System;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NpcNetworkComponent))]
public class Npc : NetworkBehaviour, IHasHealth
{	
	public event EventHandler OnNpcKilled;
	public event EventHandler<IHasHealth.OnHealthUpdatedEventArgs> OnHealthUpdated;

	[SerializeField] private int _maxHealth;
	[SerializeField] private LootTable _lootTable;
	
	private NetworkVariable<int> _npcHealthPointNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Knockback _knockback;
	private Rigidbody2D _rigidBody2D;
	
	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			_npcHealthPointNetworkVariable.Value = _maxHealth;
			_knockback = GetComponent<Knockback>();
			_rigidBody2D = GetComponent<Rigidbody2D>();
		}
		
		_npcHealthPointNetworkVariable.OnValueChanged += UpdateHealthUI;
		
		base.OnNetworkSpawn();
	}
	
	public void ApplyDamage(int damage, Vector2 damagerPosition)
	{
		DamageNpcServerRpc(damage, damagerPosition);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DamageNpcServerRpc(int damageAmount, Vector2 damagerPosition)
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] dealing {damageAmount} damage to {gameObject.name}");
		
		_npcHealthPointNetworkVariable.Value -= damageAmount;
		
		if(_npcHealthPointNetworkVariable.Value <= 0)
		{
			OnNpcKilled?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			_knockback?.ApplyKnockback(_rigidBody2D, damagerPosition);
		}
	}
	
	private void UpdateHealthUI(int previousValue, int newValue)
	{
		Debug.Log("UpdateHealthUI");
		OnHealthUpdated?.Invoke(this, new IHasHealth.OnHealthUpdatedEventArgs
		{
			PreviousValue = previousValue,
			NewValue = newValue,
			MaxValue = _maxHealth
		});
	}
	
	public void DropLoot()
	{
		_lootTable.SpawnLoot(transform.position);
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
	
		base.OnNetworkDespawn();
	}
}