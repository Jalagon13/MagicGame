using System;
using UnityEngine;

public class LaunchWandProjectile : MonoBehaviour
{
	private float _maxDistance; 
	private float _speed; 
	private LayerMask _collisionLayer; 
	private float _collisionRadius = 0.15f; 
	private float _rotationSpeed; 

	private Vector2 _direction;
	private float _distanceTraveled;
	private float _rotationDirection;
	private int _damage;
	

	public void Initialize(Vector2 direction, float maxDistance, float speed, LayerMask collisionLayer, float rotationSpeed, int damage, Sprite sprite)
	{
		_direction = direction.normalized;
		_maxDistance = maxDistance;
		_speed = speed;
		_collisionLayer = collisionLayer;
		_rotationSpeed = rotationSpeed;
		_damage = damage;
		
		GetComponent<SpriteRenderer>().sprite = sprite;

		_rotationDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
	}

	private void FixedUpdate()
	{
		MoveProjectile();
		RotateProjectile();
		CheckCollision();

		if (_distanceTraveled >= _maxDistance)
		{
			Destroy(gameObject);
		}
	}

	private void MoveProjectile()
	{
		Vector3 movement = (Vector3)_direction * _speed * Time.deltaTime;
		transform.position += movement;
		_distanceTraveled += movement.magnitude;
	}

	private void RotateProjectile()
	{
		float rotationAmount = _rotationSpeed * _rotationDirection * Time.deltaTime;
		transform.Rotate(0f, 0f, rotationAmount);
	}

	private void CheckCollision()
	{
		Collider2D wallHit = Physics2D.OverlapCircle(transform.position, _collisionRadius, _collisionLayer);
		
		if (wallHit != null)
		{
			Debug.Log($"Projectile collided with: {wallHit.name}");
			
			Destroy(gameObject);
			return;
		}

		Collider2D[] npcColliders = Physics2D.OverlapCircleAll(transform.position, _collisionRadius);
		foreach (Collider2D collider in npcColliders)
		{
			if (collider.TryGetComponent(out IHasHealth iHasHealth))
			{
				Debug.Log($"Hit NPC: {collider.name}");
				iHasHealth.ApplyDamage(_damage, transform.position);
				
				Destroy(gameObject);
				return;
			}
		}
	}
}