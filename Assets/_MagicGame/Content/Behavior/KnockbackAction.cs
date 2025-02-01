using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Knockback", story: "Apply Knockback On [Self] From [Position]", category: "Action", id: "398fc439c4437a902e8397edc195dfbf")]
public partial class KnockbackAction : Action
{
	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<Vector2> Position;

	protected override Status OnStart()
	{
		Self.Value.GetComponent<Knockback>().ApplyKnockback(Self.Value.GetComponent<Rigidbody2D>(), Position);
	
		return Status.Running;
	}

	protected override Status OnUpdate()
	{
		return Status.Success;
	}

	protected override void OnEnd()
	{
	}
}

