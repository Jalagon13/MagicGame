using UnityEngine;
using UnityEngine.Tilemaps;

public class PlaceDownItemProjectileForm : MonoBehaviour
{
	private ItemSO _projectileItemSO;
	private WorldObject _worldObjectVisualSpawnedForProjectile;
	private bool _beingDestroyed;
	private GameObject _projectileParent;

	public void Initialize(ItemSO projectileItemSO, Transform projectileParent)
	{
		_projectileItemSO = projectileItemSO;
		_projectileParent = projectileParent.gameObject;

		if (_projectileItemSO is DeployItemSO deployItemSO)
		{
			_worldObjectVisualSpawnedForProjectile = Instantiate(deployItemSO.GetDeployObjectPrefab(), transform.position, Quaternion.identity);
			_worldObjectVisualSpawnedForProjectile.transform.SetParent(transform);
		}
		else if (_projectileItemSO is BuildItemSO buildItemSO)
		{
			GetComponent<SpriteRenderer>().sprite = buildItemSO.GetWallTile().m_DefaultSprite;
		}
		
		transform.SetParent(projectileParent);
	}
	
	public void OnProjectileNpcHit(object sender, System.EventArgs e)
	{
		SpawnItem();
	}
	
	public void OnProjectileCompleted(object sender, System.EventArgs e)
	{
		if (_beingDestroyed) return;
		_beingDestroyed = true;
	
		if (_projectileItemSO is DeployItemSO deployItemSO)
		{
			DeployItem(deployItemSO);
		}
		else if (_projectileItemSO is BuildItemSO buildItemSO)
		{
			PlaceTile(buildItemSO);
		}
	}

	private void DeployItem(DeployItemSO deployItemSO)
	{
		Vector2 position = _projectileParent.transform.position;

		if (IsClear(position))
		{
			Vector2Int spawnPosition = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
			Debug.Log($"Placing deployable at: {spawnPosition}");
			ObjectManager.Instance.PlaceObject(spawnPosition, deployItemSO.GetDeployObjectPrefab(), Player.LocalClientInstance.GetPlayerEnvironment());
		}
		else
		{
			SpawnItem();
		}
	}

	private void PlaceTile(BuildItemSO buildItemSO)
	{
		Vector2 position = _projectileParent.transform.position;
		Tilemap wallTilemap = Environment.Instance.GetWallTilemapData().GetTilemap();

		if (IsClear(position) && !wallTilemap.HasTile(Vector3Int.FloorToInt(position)))
		{
			Vector3Int tilePosition = Vector3Int.FloorToInt(position);
			Debug.Log($"Placing Tile at: {tilePosition}");
			Environment.Instance.PlaceTile(tilePosition, buildItemSO.GetWallTile(), TileType.Wall, Player.LocalClientInstance.GetPlayerEnvironment());
		}
		else
		{
			SpawnItem();
		}
	}

	private bool IsClear(Vector2 position)
	{
		Vector2 checkPosition = Vector2Int.FloorToInt(position);
		Collider2D[] colliders = Physics2D.OverlapCircleAll(checkPosition + new Vector2(0.5f, 0.5f), 0.25f);

		foreach (Collider2D collider in colliders)
		{
			if (collider == _worldObjectVisualSpawnedForProjectile?.GetComponent<Collider2D>()) continue;

			if (collider.TryGetComponent(out WorldObject _))
			{
				return false;
			}
		}

		return true;
	}
	
	private void SpawnItem()
	{
		Debug.Log($"Spawning item at: {_projectileParent.transform.position}");
		GameManager.Instance.SpawnItem(_projectileItemSO, 1, _projectileParent.transform.position);
	}
}