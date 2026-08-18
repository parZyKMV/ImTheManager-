using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ShouldExploreFirst", story: "[Self]", category: "Conditions", id: "8803e7b11df11117b187693db2678b23")]
public partial class ShouldExploreFirstCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self?.Value == null) return false;

        var lifecycle = Self.Value.GetComponent<CustomerLifecycle>();
        var profile = lifecycle?.Profile;
        if (profile == null) return false;

        return UnityEngine.Random.value <= profile.exploreFirstChance;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
