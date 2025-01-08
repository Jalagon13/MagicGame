using UnityEngine;
using UnityEngine.Tilemaps;

public class PlaceDownProjectile : MonoBehaviour
{
	[SerializeField] private float _speed = 10f; 
	[SerializeField] private float _lifetime = 2f; 
	[SerializeField] private float _maxDistance = 20f; 
	[SerializeField] private LayerMask _collisionLayer; 
	[SerializeField] private float _collisionRadius = 0.1f; 
	[SerializeField] private float _rotationSpeed; 

	private Vector2 _direction;
	private Vector3 _startPosition;
	private float _distanceTraveled;
	private ItemSO _projectileItemSO;
	private WorldObject _worldObjectSpawnedForProjectile;
	private bool _beingDestroyed;
	private float _rotationDirection;

	public void Initialize(ItemSO projectileItemSO, Vector2 direction)
	{
		_projectileItemSO = projectileItemSO;

		if (_projectileItemSO != null)
		{
			if (_projectileItemSO is DeployItemSO deployItemSO)
			{
				_worldObjectSpawnedForProjectile = Instantiate(deployItemSO.GetDeployObjectPrefab(), transform.position, Quaternion.identity);
				_worldObjectSpawnedForProjectile.transform.SetParent(transform);
			}
			else if (_projectileItemSO is BuildItemSO buildItemSO)
			{
				GetComponent<SpriteRenderer>().sprite = buildItemSO.GetWallTile().m_DefaultSprite;
			}
		}

		_direction = direction.normalized; 
		_startPosition = transform.position; 
		_rotationDirection = Random.value > 0.5f ? 1f : -1f;
	}

	private void FixedUpdate()
	{
		MoveProjectile();
		RotateProjectile();
		CheckCollision();

		if (_distanceTraveled >= _maxDistance)
		{
			OnProjectileDestroy();
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
			OnProjectileDestroy();
			return;
		}

		Collider2D[] npcColliders = Physics2D.OverlapCircleAll(transform.position, _collisionRadius);
		foreach (Collider2D collider in npcColliders)
		{
			if (collider.TryGetComponent(out IHasHealth iHasHealth))
			{
				Debug.Log($"Hit NPC: {collider.name}");
				iHasHealth.ApplyDamage(10, transform.position);
				OnProjectileDestroy(false);
				return;
			}
		}
	}

	private void OnProjectileDestroy(bool placeDownProjectile = true)
	{
		if (_beingDestroyed) return;
		_beingDestroyed = true;

		if (_projectileItemSO is DeployItemSO deployItemSO)
		{
			HandleDeployItem(deployItemSO, placeDownProjectile);
		}
		else if (_projectileItemSO is BuildItemSO buildItemSO)
		{
			HandleBuildItem(buildItemSO, placeDownProjectile);
		}

		Destroy(gameObject);
	}

	private void HandleDeployItem(DeployItemSO deployItemSO, bool placeDownProjectile)
	{
		Vector2 position = transform.position;

		if (IsClear(position) && placeDownProjectile)
		{
			Vector2Int spawnPosition = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
			Debug.Log($"Placing deployable at: {spawnPosition}");
			ObjectManager.Instance.PlaceObject(spawnPosition, deployItemSO.GetDeployObjectPrefab(), Player.LocalClientInstance.GetPlayerEnvironment());
		}
		else
		{
			Debug.Log($"Spawning item at: {transform.position}");
			GameManager.Instance.SpawnItem(_projectileItemSO, 1, transform.position);
		}
	}

	private void HandleBuildItem(BuildItemSO buildItemSO, bool placeDownProjectile)
	{
		Vector2 position = transform.position;
		Tilemap wallTilemap = Environment.Instance.GetWallTilemapData().GetTilemap();

		if (IsClear(position) && !wallTilemap.HasTile(Vector3Int.FloorToInt(position)) && placeDownProjectile)
		{
			Vector3Int tilePosition = Vector3Int.FloorToInt(position);
			Debug.Log($"Placing Tile at: {tilePosition}");
			Environment.Instance.PlaceTile(tilePosition, buildItemSO.GetWallTile(), TileType.Wall, Player.LocalClientInstance.GetPlayerEnvironment());
		}
		else
		{
			Debug.Log($"Spawning item at: {transform.position}");
			GameManager.Instance.SpawnItem(_projectileItemSO, 1, transform.position);
		}
	}

	private bool IsClear(Vector2 position)
	{
		Vector2 checkPosition = Vector2Int.FloorToInt(position);
		Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPosition + new Vector2(0.5f, 0.5f), _collisionRadius);

		foreach (Collider2D collider in colliders)
		{
			if (collider == _worldObjectSpawnedForProjectile?.GetComponent<Collider2D>()) continue;

			if (collider.TryGetComponent(out WorldObject _))
			{
				return false;
			}
		}

		return true;
	}
}