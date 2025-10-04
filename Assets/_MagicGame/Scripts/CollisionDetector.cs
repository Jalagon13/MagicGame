using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProjectWizard
{
	public class CollisionDetector : NetworkBehaviour
	{
		private BiomeType _colliderBiome;
		private Collider2D _wallDetectorCollider;

		private void Awake()
		{
			_wallDetectorCollider = GetComponent<Collider2D>();
		}

		private void Start()
		{
			if (!IsServer) return;

			Pathfinding.Instance.OnPathfindingTilemapCreated += UpdateCollisions;
		}

		public override void OnDestroy()
		{
			if (!IsServer) return;

			Pathfinding.Instance.OnPathfindingTilemapCreated -= UpdateCollisions;
		}

		private void UpdateCollisions(object sender, Pathfinding.PathfindingTilemapEventArgs e)
		{
			if (e.Biome != _colliderBiome)
			{
				Physics2D.IgnoreCollision(_wallDetectorCollider, e.TilemapCollider);
			}
		}

		public void SetBiome(BiomeType spawnBiome)
		{
			_colliderBiome = spawnBiome;

			if (!IsServer) return;

			foreach (var biome in Pathfinding.Instance.GetExistingPathfindingBiomes())
			{
				bool ignore = biome.Key != _colliderBiome;
				Physics2D.IgnoreCollision(_wallDetectorCollider, biome.Value, ignore);
			}
		}
	}
}
