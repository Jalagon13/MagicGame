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
	private ulong _projectileId;
	private Rigidbody2D _rigidbody2D;

	private void Awake()
	{
		// Get the Rigidbody2D component
		_rigidbody2D = GetComponent<Rigidbody2D>();
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
				
				StopProjectile();
			}
		}
	}

	// Initialize the projectile with impulse force
	public void Initialize(int speed, int damage, float lifetime, Vector3 directionNormalized, ulong sourcePlayerId, ulong projectileId)
	{
		_projectileId = projectileId;
		_sourcePlayerId = sourcePlayerId;
		_damagerPosition = transform.position;
		_speed = speed;
		_damage = damage;
		_directionNormalized = directionNormalized;
		
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(_directionNormalized * _speed, ForceMode2D.Impulse);
		
		if(IsServer)
		{
			Invoke(nameof(StopProjectile), lifetime);
		}
	}
	
	private void StopProjectile()
	{
		GameManager.Instance.DestroyFakeProjectile(_sourcePlayerId, _projectileId);
		NetworkObject.Despawn();
		Destroy(gameObject);
	}
}