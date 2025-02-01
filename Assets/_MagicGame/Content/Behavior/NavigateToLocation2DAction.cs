using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "NavigateToLocation2D", story: "[Self] navigates to [Position2D] with Speed [SpeedValue]", category: "Action", id: "5e5b2aa13ca36d423113aef3183c1acf")]
public partial class NavigateToLocation2DAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<Vector2> Position2D;
    [SerializeReference] public BlackboardVariable<float> SpeedValue;
	[SerializeReference] public BlackboardVariable<float> StoppingDistance;

	private Rigidbody2D _rb2d;
	private float _distanceToTarget;
	private float _stuckTimer = 0f; // Time spent trying to reach the destination
	private const float StuckThreshold = 4f; // Time threshold to consider as stuck (seconds)

	protected override Status OnStart()
	{
		if (Self.Value == null)
		{
			Debug.LogError("NavigateToLocation2DAction: Self is null!");
			return Status.Failure;
		}

		_rb2d = Self.Value.GetComponent<Rigidbody2D>();
		if (_rb2d == null)
		{
			Debug.LogError("NavigateToLocation2DAction: No Rigidbody2D found on Self!");
			return Status.Failure;
		}

		_stuckTimer = 0f; // Reset stuck timer when starting
		_distanceToTarget = Vector2.Distance(Self.Value.transform.position, Position2D);
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		if (Self.Value == null || _rb2d == null)
			return Status.Failure;

		Vector2 currentPosition = _rb2d.position;
		Vector2 targetPosition = Position2D.Value;
		Vector2 direction = (targetPosition - currentPosition).normalized;
		float currentDistanceToTarget = Vector2.Distance(currentPosition, targetPosition);

		// Check if close enough to the target
		if (currentDistanceToTarget <= StoppingDistance.Value)
		{
			_rb2d.linearVelocity = Vector2.zero; // Stop moving
			Debug.LogWarning("Stopped at destination");
			return Status.Success;
		}

		// Move towards target
		_rb2d.MovePosition(currentPosition + direction * SpeedValue.Value * Time.deltaTime);

		// Increment stuck timer and check if NPC has been stuck too long
		_stuckTimer += Time.deltaTime;
		if (_stuckTimer >= StuckThreshold || currentDistanceToTarget <= StoppingDistance.Value)
		{
			Debug.LogWarning("NPC is stuck or reached destination!");
			return Status.Success;
		}

		return Status.Running;
	}

	protected override void OnEnd()
	{
		// Stop movement on end
		if (_rb2d != null)
			_rb2d.linearVelocity = Vector2.zero;
	}
}