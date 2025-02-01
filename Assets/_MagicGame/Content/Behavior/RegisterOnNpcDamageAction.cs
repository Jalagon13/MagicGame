using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RegisterOnNpcDamage", story: "[Self] Registers Npc OnDamaged For [IsDamaged]", category: "Action", id: "a3512e91ace19594ef5199d89527b942")]
public partial class RegisterOnNpcDamageAction : Action
{
	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<bool> IsDamaged;
	[SerializeReference] public BlackboardVariable<Vector2> DamageSourcePosition;
	protected override Status OnStart()
	{
		Self.Value.GetComponent<Npc>().OnNpcDamged += OnNpcDamaged;
	
		return Status.Running;
	}

    private void OnNpcDamaged(object sender, Npc.OnNpcDamagedEventArgs e)
	{
		IsDamaged.Value = true;
		DamageSourcePosition.Value = e.DamageSourcePosition;
	}

	protected override Status OnUpdate()
	{
		return Status.Success;
	}

	protected override void OnEnd()
	{
	}
}

