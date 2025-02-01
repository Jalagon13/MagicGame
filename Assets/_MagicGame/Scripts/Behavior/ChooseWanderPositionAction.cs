using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.Tilemaps;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ChooseWanderPosition", story: "[Self] Determines [WanderPosition] For [MaxWanderDistance]", category: "Action", id: "8cf19c6c2950a3bd5da7ecce5fb7a10f")]
public partial class ChooseWanderPositionAction : Action
{
	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<Vector2> WanderPosition;
	[SerializeReference] public BlackboardVariable<float> MaxWanderDistance;
	private const float MinValidDistance = 0.5f;
	private const float CollisionBackupDistance = 0.25f;
	
	protected override Status OnStart()
	{
		BiomeType biomeType = Self.Value.GetComponent<NpcNetworkComponent>().NpcBiomeType;
		TilemapCollider2D tilemapWallCollider  = Pathfinding.Instance.BiomeToLoadedPathfindingChunks[biomeType].WallColliderTm.GetComponent<TilemapCollider2D>();
	
		if (Self.Value == null)
		{
			Debug.LogWarning("Self is null in ChooseWanderPositionAction");
			return Status.Failure;
		}

		Vector2 selfPosition = Self.Value.transform.position;
		Vector2 chosenPosition = selfPosition;

		int maxAttempts = 10; // Prevent infinite loops

		for (int i = 0; i < maxAttempts; i++)
		{
			// Pick a random direction
			float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
			Vector2 direction = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));

			// Perform raycast
			RaycastHit2D hit = Physics2D.Raycast(selfPosition, direction, MaxWanderDistance);

			if (hit.collider == tilemapWallCollider)
			{
				// If we hit something, back up 0.25 units from the collision point
				Debug.Log($"Hit {tilemapWallCollider} backing dat ass up 0.25 units");
				chosenPosition = hit.point - (direction * CollisionBackupDistance);
			}
			else
			{
				// If nothing is hit, set to max range
				chosenPosition = selfPosition + (direction * MaxWanderDistance);
			}

			// Check if the position is valid
			float distance = Vector2.Distance(selfPosition, chosenPosition);
			if (distance >= MinValidDistance)
			{
				break;
			}
		}

		// If we exhausted attempts, just use the last found position
		WanderPosition.Value = chosenPosition;

		return Status.Success;
	}

	protected override Status OnUpdate()
	{
		return Status.Success;
	}

	protected override void OnEnd()
	{
	}
}