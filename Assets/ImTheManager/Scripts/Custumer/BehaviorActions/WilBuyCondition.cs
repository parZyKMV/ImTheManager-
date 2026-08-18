using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "WilBuy", story: "[Self]", category: "Conditions", id: "06766e76369d163e50892e0eb19de921")]
public partial class WilBuyCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self?.Value == null) return true;

        var lifecycle = Self.Value.GetComponent<CustomerLifecycle>();
        if (lifecycle == null || lifecycle.Profile == null) return true;

        return lifecycle.Profile.willBuy;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
