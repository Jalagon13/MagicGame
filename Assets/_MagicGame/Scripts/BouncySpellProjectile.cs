using System;
using UnityEngine;

public class BouncySpellProjectile : MonoBehaviour
{
	[SerializeField] private NpcWallCollider _wallCollider;

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
		
		_wallCollider.OnWallCollide += BounceOffWall;
	}

	private void FixedUpdate()
	{
		// Increment time alive
		_timeAlive += Time.deltaTime;

		// Destroy the projectile when its lifetime is exceeded
		if (_timeAlive >= _lifetime)
		{
			Destroy(gameObject);
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if(Player.LocalClientInstance.HitCollider == other) return;

		if (other.TryGetComponent(out IHasHealth npcToDamage))
		{
			npcToDamage.ApplyDamage(_damage, _damagerPosition);
			Destroy(gameObject);
		}
	}

	// Initialize the projectile with impulse force
	public void Initialize(int speed, int damage, float lifetime, Vector3 directionNormalized)
	{
		_damagerPosition = transform.position;
		_speed = speed;
		_damage = damage;
		_lifetime = lifetime;
		_directionNormalized = directionNormalized;

		// Apply an initial impulse to the Rigidbody2D to launch the projectile
		_rigidbody2D.linearVelocity = Vector2.zero;  // Reset any previous velocity
		_rigidbody2D.AddForce(_directionNormalized * _speed, ForceMode2D.Impulse);
	}

	private void BounceOffWall(object sender, NpcWallCollider.WallCollisionEventArgs e)
	{
		// Particles and game feel here
	}

	private void OnDestroy()
	{
		_wallCollider.OnWallCollide -= BounceOffWall;
	}
}