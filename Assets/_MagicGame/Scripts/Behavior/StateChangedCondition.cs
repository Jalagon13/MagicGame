using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "StateChanged", story: "[CurrentState] Has Changed", category: "Conditions", id: "56f7551a782e16593ca0be744001da94")]
public partial class StateChangedCondition : Condition
{
    [SerializeReference] public BlackboardVariable<LivestockState> CurrentState;

    public override bool IsTrue()
    {
        return true;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
