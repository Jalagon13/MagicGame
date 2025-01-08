using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
	[SerializeField] private float _speed = 10f; // Speed of the projectile
	[SerializeField] private float _lifetime = 2f; // Time in seconds before the projectile despawns
	[SerializeField] private float _maxDistance = 20f; // Maximum distance the projectile can travel
	[SerializeField] private LayerMask _collisionLayer; // Layer mask to detect collisions
	[SerializeField] private float _collisionRadius = 0.1f; // Radius for collision detection

	private Vector2 _direction;
	private Vector3 _startPosition;
	private float _distanceTraveled = 0f;

	/// <summary>
	/// Initializes the projectile with the given item and direction.
	/// </summary>
	/// <param name="projectileItemSO">The item data for the projectile.</param>
	/// <param name="direction">The direction to move the projectile.</param>
	public void Initialize(ItemSO projectileItemSO, Vector2 direction)
	{
		if (projectileItemSO != null)
		{
			GetComponent<SpriteRenderer>().sprite = projectileItemSO.UiDisplay;
		}

		_direction = direction.normalized; // Ensure direction is normalized
		_startPosition = transform.position; // Store the starting position
		Destroy(gameObject, _lifetime); // Destroy the projectile after its lifetime
	}

	private void FixedUpdate()
	{
		// Move the projectile
		Vector3 movement = (Vector3)_direction * _speed * Time.deltaTime;
		transform.position += movement;

		// Track the distance traveled
		_distanceTraveled += movement.magnitude;

		// Collision check
		CheckCollision();

		// Check if the projectile has exceeded the maximum distance
		if (_distanceTraveled >= _maxDistance)
		{
			Destroy(gameObject);
		}
	}

	private void CheckCollision()
	{
		// Use Physics2D.OverlapCircle to detect collisions with a specific layer
		Collider2D hit = Physics2D.OverlapCircle(transform.position, _collisionRadius, _collisionLayer);
		if (hit != null)
		{
			// Perform the desired action on collision
			Debug.Log($"Projectile collided with: {hit.name}");

			// Destroy the projectile after collision
			Destroy(gameObject);
		}
	}
}