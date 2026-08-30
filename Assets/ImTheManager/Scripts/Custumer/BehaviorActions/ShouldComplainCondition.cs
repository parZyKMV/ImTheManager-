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

        // La dificultad escala la probabilidad segun el dia (dia 10 se
        // queja mas seguido que el dia 1, sin tocar los datos del profile).
        float difficultyMultiplier = DifficultyCurve.Instance != null ? DifficultyCurve.Instance.GetDifficultyMultiplier() : 1f;
        float effectiveChance = Mathf.Clamp01(profile.complaintChance * difficultyMultiplier);

        return UnityEngine.Random.value <= effectiveChance;
    }

    bool ContainsTrigger(CustomerProfile profile, ComplaintTrigger trigger)
    {
        if (profile.complaintTriggers == null) return false;
        foreach (var t in profile.complaintTriggers)
            if (t == trigger) return true;
        return false;
    }
}