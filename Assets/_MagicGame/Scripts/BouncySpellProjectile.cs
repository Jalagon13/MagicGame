using System;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class BouncySpellProjectile : NetworkBehaviour
{
	private int _speed;
	private int _damage;
	private float _lifetime;
	private Vector3 _directionNormalized;
	private Vector3 _damagerPosition;
	private ulong _sourcePlayerId;
	private float _timeAlive;
	private Rigidbody2D _rigidbody2D;

	private void Awake()
	{
		// Get the Rigidbody2D component
		_rigidbody2D = GetComponent<Rigidbody2D>();
	}
	
	private void FixedUpdate()
	{
		// Increment time alive
		_timeAlive += Time.deltaTime;

		// Destroy the projectile when its lifetime is exceeded
		if (_timeAlive >= _lifetime)
		{
			if(IsServer)
			{
				StopProjectileClientRPC(RpcTarget.Single(_sourcePlayerId, RpcTargetUse.Persistent));
				// Destroy(gameObject);
			}
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		// If is overlapping with the collider attached to the player who sent it, don't damage it
		if(NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject.GetComponent<Player>().HitCollider == other) return;

		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			if(IsServer)
			{
				npcToDamage.ApplyDamage(_damage, _damagerPosition);
				
				StopProjectileClientRPC(RpcTarget.Single(_sourcePlayerId, RpcTargetUse.Persistent));
				// Destroy(gameObject);
			}
		}
	}

	// Initialize the projectile with impulse force
	public void Initialize(int speed, int damage, float lifetime, Vector3 directionNormalized, ulong sourcePlayerId)
	{
		_sourcePlayerId = sourcePlayerId;
		_damagerPosition = transform.position;
		_speed = speed;
		_damage = damage;
		_lifetime = lifetime;
		_directionNormalized = directionNormalized;
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(_directionNormalized * _speed, ForceMode2D.Impulse);
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void StopProjectileClientRPC(RpcParams rpcParams)
	{
		Debug.Log($"Test.. {gameObject.name}");
	}
}