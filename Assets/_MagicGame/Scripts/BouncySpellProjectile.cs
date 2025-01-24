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
				Destroy(gameObject);
			}
		}
	}

	// private void OnTriggerEnter2D(Collider2D other)
	// {
	// 	if(Player.LocalClientInstance.HitCollider == other) return;

	// 	if (other.TryGetComponent(out IHasHealth npcToDamage))
	// 	{
	// 		if(IsServer)
	// 		{
	// 			npcToDamage.ApplyDamage(_damage, _damagerPosition);
	// 		}
			
	// 		Destroy(gameObject);
	// 	}
	// }

	// Initialize the projectile with impulse force
	public void Initialize(int speed, int damage, float lifetime, Vector3 directionNormalized)
	{
	
		_damagerPosition = transform.position;
		_speed = speed;
		_damage = damage;
		_lifetime = lifetime;
		_directionNormalized = directionNormalized;
		Debug.Log("Initialized and force is being added");
		// Apply an initial impulse to the Rigidbody2D to launch the projectile
		_rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
		_rigidbody2D.AddForce(_directionNormalized * _speed, ForceMode2D.Impulse);
		Debug.Log($"Velocity: {_rigidbody2D.linearVelocity.magnitude}, Speed: {_speed}, Direction {_directionNormalized}");
	}
}