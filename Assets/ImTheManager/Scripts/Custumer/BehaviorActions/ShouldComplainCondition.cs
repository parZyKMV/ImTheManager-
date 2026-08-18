using Unity.Behavior;
using UnityEngine;
using System;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "ShouldComplain", story: "[Self] to [ShopingPint]", category: "Conditions", id: "c7d39f088b1edc0d3d1ab1ec8d3c9ab2")]
public partial class ShouldComplainCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> ShopingPint;

    public override bool IsTrue()
    {
        if (Self?.Value == null) return false;

        var lifecycle = Self.Value.GetComponent<CustomerLifecycle>();
        var profile = lifecycle?.Profile;
        if (profile == null) return false;

        ShelfSlot shelf = ShopingPint?.Value != null
            ? ShopingPint.Value.GetComponentInParent<ShelfSlot>()
            : null;

        bool emptyShelfTriggered = shelf != null && shelf.IsEmpty && ContainsTrigger(profile, ComplaintTrigger.EmptyShelf);
        if (!emptyShelfTriggered) return false;

        return UnityEngine.Random.value <= profile.complaintChance;
    }

    bool ContainsTrigger(CustomerProfile profile, ComplaintTrigger trigger)
    {
        if (profile.complaintTriggers == null) return false;
        foreach (var t in profile.complaintTriggers)
            if (t == trigger) return true;
        return false;
    }
}
