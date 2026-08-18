using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo Action: el cliente se queja de verdad (le agrega estres al
/// SanityMeter). Solo se ejecuta cuando ShouldComplainCondition (antes en
/// la misma Sequence) ya evaluo true - este nodo no vuelve a tirar el dado,
/// solo aplica la consecuencia.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Complain",
    story: "[Agent] complains",
    category: "Action/Customer",
    id: "3c4d5e6f708192a3b4c5d6e7f8091223")]
public partial class ComplainAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var lifecycle = Agent.Value.GetComponent<CustomerLifecycle>();
        var profile = lifecycle?.Profile;

        if (profile == null)
        {
            Debug.LogWarning("[ComplainAction] No hay CustomerProfile, no se puede calcular el estres.");
            return Status.Failure;
        }

        Debug.Log($"[ComplainAction] {Agent.Value.name} se queja (estante vacio).");

        if (SanityMeter.Instance != null)
            SanityMeter.Instance.AddStress(profile.complaintStressAmount, "Complaint:EmptyShelf");

        return Status.Success;
    }
}
