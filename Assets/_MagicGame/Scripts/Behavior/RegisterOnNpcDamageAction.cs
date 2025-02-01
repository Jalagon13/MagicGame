using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RegisterOnNpcDamage", story: "[Self] Registers Damage Event For [IsDamaged] and [DamagerPosition] with [KnockbackResist]", category: "Action", id: "a3512e91ace19594ef5199d89527b942")]
public partial class RegisterOnNpcDamageAction : Action
{
	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<bool> IsDamaged;
	[SerializeReference] public BlackboardVariable<Vector2> DamagerPosition;
	[SerializeReference] public BlackboardVariable<float> KnockbackResist;
	
	protected override Status OnStart()
	{
		Self.Value.GetComponent<Npc>().OnNpcDamged += OnNpcDamaged;
		Self.Value.GetComponent<Rigidbody2D>().linearDamping = KnockbackResist.Value;
	
		return Status.Running;
	}

	private void OnNpcDamaged(object sender, Npc.OnNpcDamagedEventArgs e)
	{
		IsDamaged.Value = true;
		DamagerPosition.Value = e.DamageSourcePosition;
	}

	protected override Status OnUpdate()
	{
		return Status.Success;
	}

	protected override void OnEnd()
	{
	}
}

