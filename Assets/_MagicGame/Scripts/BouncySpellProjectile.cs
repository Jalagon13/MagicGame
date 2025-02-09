using System;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BouncySpellProjectile : NetworkBehaviour
{
	private int _speed;
	private int _damage;
	private Vector3 _directionNormalized;
	private Vector3 _damagerPosition;
	private ulong _sourcePlayerId;
	private Rigidbody2D _rigidbody2D;

	private void Awake()
	{
		// Get the Rigidbody2D component
		_rigidbody2D = GetComponent<Rigidbody2D>();
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if(!IsServer) return;
	
		// If is overlapping with the collider attached to the player who sent it, don't damage it
		if(NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject == null || NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject.GetComponent<Player>().HitCollider == other) return;

		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			npcToDamage.ApplyDamage(_damage, _damagerPosition, 20);
			GetComponent<SpellNetworkComponent>().StopProjectile();
			return;
		}
	}

	// Initialize the projectile with impulse force
	public void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId)
	{
		_sourcePlayerId = sourcePlayerId;
		_damagerPosition = transform.position;
		_speed = speed;
		_damage = damage;
		_directionNormalized = directionNormalized;
		
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(_directionNormalized * _speed, ForceMode2D.Impulse);
	}
}