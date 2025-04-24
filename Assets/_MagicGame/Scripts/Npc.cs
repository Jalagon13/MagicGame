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
	public event System.EventHandler OnServerNpcKilled;
	public event EventHandler<OnNpcDamagedEventArgs> OnServerNpcDamged;
	public event EventHandler<OnNpcDamagedEventArgs> OnClientNpcDamged;
	public class OnNpcDamagedEventArgs : EventArgs
	{
		public Vector2 DamageSourcePosition;
	}

	[field: SerializeField] public NpcSO NpcSO { get; private set; }
	[Range(0, 1f)]
	[SerializeField] private float _knockbackResist;
	[SerializeField] private int _damage;
	[SerializeField] private DamageCollider _damageCollider;
	[field: SerializeField] public EventReference DamageSound { get; private set; }
	[field: SerializeField] public EventReference DeathSound { get; private set; }
	[SerializeField] private List<Loot> Table = new();
	
	private Knockback _knockback;
	private NetworkHealthState _healthState;
	
	public BiomeType Biome { get { return GetComponent<NpcNetworkComponent>().NpcBiomeType; } }
	
	private void Awake()
	{
		_knockback = GetComponent<Knockback>();
		_healthState = GetComponent<NetworkHealthState>();

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
	
	private void OnNpcDamaged(object sender, NetworkHealthState.HitPointsDamagedEventArgs e)
	{
		SoundManager.Instance.PlayOneShot(DamageSound, transform.position);
		GameManager.Instance.PlayDamageNumbersClientRpc(e.DamageTaken, transform.position, GetComponent<NpcNetworkComponent>().NpcBiomeType, Color.yellow);

		_knockback.ApplyKnockback(e.SourcePosition, _knockbackResist, e.KnockbackForce);

		OnServerNpcDamged?.Invoke(this, new OnNpcDamagedEventArgs
		{
			DamageSourcePosition = e.SourcePosition
		});

		OnNpcDamagedClientRpc(e.SourcePosition);
	}
	
	[Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
	private void OnNpcDamagedClientRpc(Vector2 sourcePosition)
	{
		OnClientNpcDamged?.Invoke(this, new OnNpcDamagedEventArgs
		{
			DamageSourcePosition = sourcePosition
		});
	}

	private void OnNpcDeath(object sender, EventArgs e)
	{
		SoundManager.Instance.PlayOneShot(DeathSound, transform.position);
		KillNpc();
	}

	private void OnNpcHealed(object sender, EventArgs e)
	{
		
	}
	
	public void KillNpc()
	{
		OnServerNpcKilled?.Invoke(this, EventArgs.Empty);
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