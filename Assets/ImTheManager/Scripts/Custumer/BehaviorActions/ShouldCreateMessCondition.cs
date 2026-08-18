using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ShouldCreateMessCondition", story: "[Self]", category: "Conditions", id: "df8fdc5925e5813fb14c9639e0027d76")]
public partial class ShouldCreateMessCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self?.Value == null) return false;

        var lifecycle = Self.Value.GetComponent<CustomerLifecycle>();
        var profile = lifecycle?.Profile;
        if (profile == null) return false;

        return UnityEngine.Random.value <= profile.messChance;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
