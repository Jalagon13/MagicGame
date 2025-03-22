using System;
using System.Collections.Generic;
using FMODUnity;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Unity.Behavior;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NpcNetworkComponent))]
public class Npc : NetworkBehaviour
{	
	public event System.EventHandler OnNpcKilled;
	public event EventHandler<OnNpcDamagedEventArgs> OnNpcDamged;
	public class OnNpcDamagedEventArgs : EventArgs
	{
		public Vector2 DamageSourcePosition;
	}

	[SerializeField] private float _playerDetectionRange = 10f;
	[SerializeField] private float _knockbackResist;
	[SerializeField] private int _damage;
	[SerializeField] private DamageCollider _damageCollider;
	[SerializeField] private EventReference _damageSound;
	[SerializeField] private List<Loot> Table = new();
	
	private Knockback _knockback;
	private NetworkHealthState _healthState;
	private ChaseAIStateMachine _stateMachine;
	private Shop _shop;
	
	public BiomeType Biome { get { return GetComponent<NpcNetworkComponent>().NpcBiomeType; } }
	
	private void Awake()
	{
		_knockback = GetComponent<Knockback>();
		_healthState = GetComponent<NetworkHealthState>();
		_stateMachine = GetComponent<ChaseAIStateMachine>();
		_shop = GetComponent<Shop>();

		if (_damageCollider != null)
		{
			_damageCollider.AddDamageExceptionCollider(GetComponent<Collider2D>());
			_damageCollider.DamageAmount = _damage;
		}
	}
	
	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			_healthState.OnHitPointsDamaged += OnNpcDamaged;
			_healthState.OnHitPointsDepleted += OnNpcDeath;
			_healthState.OnHitPointsReplenished += OnNpcHealed;
		}
		
		base.OnNetworkSpawn();
	}
	
	private void Update()
	{
		if(!IsServer || _shop.PlayersUsingShop.Count > 0) return;
		
		Player closestPlayer = MultiplayerManager.Instance.GetClosestPlayer(transform.position, GetComponent<NpcNetworkComponent>().NpcBiomeType);
		if (closestPlayer != null)
		{
			float distanceToPlayer = Vector3.Distance(transform.position, closestPlayer.transform.position);
			_stateMachine.CanMove = distanceToPlayer <= _playerDetectionRange;
		}
		else
		{
			_stateMachine.CanMove = false;
		}
	}

	private void OnNpcDamaged(object sender, NetworkHealthState.HitPointsDamagedEventArgs e)
	{
		SoundManager.Instance.PlayOneShot(_damageSound, transform.position);
		GameManager.Instance.PlayDamageNumbers(e.DamageTaken, transform.position, GetComponent<NpcNetworkComponent>().NpcBiomeType);

		_knockback.ApplyKnockback(e.SourcePosition, _knockbackResist, e.KnockbackForce);

		OnNpcDamged?.Invoke(this, new OnNpcDamagedEventArgs
		{
			DamageSourcePosition = e.SourcePosition
		});
	}

	private void OnNpcDeath(object sender, EventArgs e)
	{
		KillNpc();
	}

	private void OnNpcHealed(object sender, EventArgs e)
	{
		
	}
	
	public void KillNpc()
	{
		OnNpcKilled?.Invoke(this, EventArgs.Empty);
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
	
	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			_healthState.OnHitPointsDamaged -= OnNpcDamaged;
			_healthState.OnHitPointsDepleted -= OnNpcDeath;
			_healthState.OnHitPointsReplenished -= OnNpcHealed;
		}

		base.OnNetworkDespawn();
	}
}