using System;
using UnityEngine;

public class SwordSpell : Spell
{
	[SerializeField] private int _swingAngle;
	[SerializeField] private Transform _pivot;
	[SerializeField] private DamageCollider _damageCollider;

	private float _elapsedTime;
	private float _startAngle;
	private float _endAngle;
	private bool _isSwinging;
	
	private void Update()
	{
		transform.position = NetworkManager.ConnectedClients[_sourcePlayerId].PlayerObject.GetComponent<Player>().ProjectileSpawnPointTf.position;
	
		if (_isSwinging)
		{
			_pivot.GetChild(0).gameObject.SetActive(true);
		
			_elapsedTime += Time.deltaTime;
			float t = Mathf.Clamp01(_elapsedTime / _lifeTime); // Normalize time between 0 and 1
			float currentAngle = Mathf.LerpAngle(_startAngle, _endAngle, t);
			_pivot.rotation = Quaternion.Euler(0, 0, currentAngle);

			// Stop swinging after the lifetime is over
			if (_elapsedTime >= _lifeTime)
			{
				_isSwinging = false;
			}
		}
	}
	
	public override void Initialize(BiomeType biome, int speed, int damage, Vector3 directionNormalized, ulong sourcePlayerId, int knockback, float lifetime)
	{
		base.Initialize(biome, speed, damage, directionNormalized, sourcePlayerId, knockback, lifetime);
		
		_pivot.GetChild(0).gameObject.SetActive(false);
		
		// Determine the cardinal direction for the center angle
		float angle = Mathf.Atan2(directionNormalized.y, directionNormalized.x) * Mathf.Rad2Deg;

		// Map angle to cardinal direction
		float centerAngle = 0f;
		// if (angle >= -45f && angle < 45f)  // East
		// {
		// 	centerAngle = 0f;
		// }
		// else if (angle >= 45f && angle < 135f)  // North
		// {
		// 	centerAngle = 90f;
		// }
		// else if (angle >= 135f || angle < -135f)  // West
		// {
		// 	centerAngle = 180f;
		// }
		// else if (angle >= -135f && angle < -45f)  // South
		// {
		// 	centerAngle = 270f;
		// }
		centerAngle = angle;

		// Set up the swing rotation
		_startAngle = centerAngle - (_swingAngle / 2f); 
		_endAngle = centerAngle + (_swingAngle / 2f); 
		_elapsedTime = 0f;
		_isSwinging = true;
		
		// Initialize damage and source player's hp collider as damage exception
		_damageCollider.DamageAmount = damage;
		_damageCollider.KnockbackForce = knockback;
		_damageCollider.AddDamageExceptionCollider(NetworkManager.ConnectedClients[sourcePlayerId].PlayerObject.GetComponent<Player>().HitCollider);
		_damageCollider.OnDamage += AddDamageException;
	}

	private void AddDamageException(object sender, DamageCollider.OnDamageEventArgs e)
	{
		_damageCollider.AddDamageExceptionCollider(e.ColliderDamaged);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		
		_damageCollider.OnDamage -= AddDamageException;
	}
}