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
	private ItemSO _projectileItemSO;

	/// <summary>
	/// Initializes the projectile with the given item and direction.
	/// </summary>
	/// <param name="projectileItemSO">The item data for the projectile.</param>
	/// <param name="direction">The direction to move the projectile.</param>
	public void Initialize(ItemSO projectileItemSO, Vector2 direction)
	{
		_projectileItemSO = projectileItemSO;
	
		if (_projectileItemSO != null)
		{
			GetComponent<SpriteRenderer>().sprite = _projectileItemSO.UiDisplay;
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
			OnProjectileDestroy();
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
			OnProjectileDestroy();
		}
	}
	
	private void OnProjectileDestroy()
	{
		if(_projectileItemSO is DeployItemSO deployItemSO)
		{
			Debug.Log("Deploying object");
			Vector2 pos = transform.position;
		
			if(IsClear(pos))
			{
				Debug.Log("Deploying object at position");
				Vector2Int spawnPosition = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
				
				ObjectManager.Instance.PlaceObject(spawnPosition, deployItemSO.GetDeployObjectPrefab(), Player.LocalClientInstance.GetPlayerEnvironment());
			}
		}
	
		Destroy(gameObject);
	}
	
	private bool IsClear(Vector2 position)
	{
		Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
		var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);

		foreach(Collider2D col in colliders)
		{
			if(col.TryGetComponent(out ResourceObject clickable)) 
				return false;
		}

		return true;
	}
}