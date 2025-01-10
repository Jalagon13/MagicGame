using System;
using UnityEngine;

public class LaunchProjectileBehavior : MonoBehaviour
{
	public event EventHandler OnProjectileCompleted; // When it hits a wall or reaches max distance
	public event EventHandler OnProjectileNpcHit;

	private float _maxDistance; 
	private float _speed; 
	private LayerMask _collisionLayer; 
	private float _collisionRadius; 
	private float _rotationSpeed; 

	private Vector2 _direction;
	private float _distanceTraveled;
	private float _rotationDirection;
	private int _damage;
	private ItemSO _projectileItemSO;

	public void Initialize(ItemSO projectileItemSO, Vector2 direction, float maxDistance, float speed, LayerMask collisionLayer, float collisionRadius, float rotationSpeed, int damage)
	{
		_projectileItemSO = projectileItemSO;
		_direction = direction.normalized;
		_maxDistance = maxDistance;
		_speed = speed;
		_collisionLayer = collisionLayer;
		_collisionRadius = collisionRadius;
		_rotationSpeed = rotationSpeed;
		_damage = damage;

		_rotationDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
	}
	
	public void SetProjectileBehaviorSprite(Sprite sprite)
	{
		GetComponent<SpriteRenderer>().sprite = sprite;
	}

	private void FixedUpdate()
	{
		MoveProjectile();
		RotateProjectile();
		CheckCollision();

		if (_distanceTraveled >= _maxDistance)
		{
			OnProjectileCompleted?.Invoke(this, EventArgs.Empty);
			DestroyProjectile();
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
			
			OnProjectileCompleted?.Invoke(this, EventArgs.Empty);
			DestroyProjectile();
			return;
		}

		Collider2D[] npcColliders = Physics2D.OverlapCircleAll(transform.position, _collisionRadius);
		foreach (Collider2D collider in npcColliders)
		{
			if (collider.TryGetComponent(out IHasHealth iHasHealth))
			{
				Debug.Log($"Hit NPC: {collider.name}");
				iHasHealth.ApplyDamage(_damage, transform.position);
				
				OnProjectileNpcHit?.Invoke(this, EventArgs.Empty);
				DestroyProjectile();
				return;
			}
		}
	}

	private void DestroyProjectile()
	{
		OnProjectileCompleted = null;
		OnProjectileNpcHit = null;

		Destroy(gameObject);
	}

	public void DefaultProjectileCompletedBehavior(object sender, EventArgs e)
	{
		Debug.Log($"Spawning item at: {transform.position}");
		GameManager.Instance.SpawnItem(_projectileItemSO, 1, transform.position);
	}

	public void DefaultProjectileNpcHitBehavior(object sender, EventArgs e)
	{
		Debug.Log($"Spawning item at: {transform.position}");
		GameManager.Instance.SpawnItem(_projectileItemSO, 1, transform.position);
	}
}