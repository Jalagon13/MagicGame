using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WallColliderDetector : MonoBehaviour
{
	// Define the event
	public event EventHandler<WallCollisionEventArgs> OnTouchingWall;
	public class WallCollisionEventArgs : EventArgs
	{
		public ContactPoint2D[] ContactPoints;
	} 
	
	private BiomeType _colliderBiome;
	private Collider2D _wallDetectorCollider;

	private void Awake()
	{
		_wallDetectorCollider = GetComponent<Collider2D>();
	}
	
	private void Start()
	{
		Pathfinding.Instance.OnPathfindingTilemapCreated += UpdateCollisions;
	}

	private void UpdateCollisions(object sender, Pathfinding.PathfindingTilemapEventArgs e)
	{
		if(e.Environment != _colliderBiome)
		{
			Debug.Log(transform.root.name + " Ignoring detection of " + e.TilemapCollider.name);
			Physics2D.IgnoreCollision(_wallDetectorCollider, e.TilemapCollider);
		}
	}

	public void SetEnvironment(BiomeType spawnBiome, Dictionary<BiomeType, TilemapCollider2D> registeredPfBiomes) // Sets the environment whose walls this collider will detect
	{
		_colliderBiome = spawnBiome;
		
		foreach (var biome in registeredPfBiomes)
		{
			if(biome.Key != _colliderBiome)
			{
				Physics2D.IgnoreCollision(_wallDetectorCollider, biome.Value);
			}
		}
	}
	
	private void OnCollisionEnter2D(Collision2D other)
	{
		if (Player.LocalClientInstance.OwnerClientId == NetworkManager.ServerClientId)
		{
			if (other.gameObject.layer == 9) return; // Ignore local walls for the server
		}

		// Trigger the bounce/knockback effect immediately
		OnTouchingWall?.Invoke(this, new WallCollisionEventArgs()
		{
			ContactPoints = other.contacts
		});
	}

	private void OnDestroy()
	{
		Pathfinding.Instance.OnPathfindingTilemapCreated -= UpdateCollisions;
	}
}