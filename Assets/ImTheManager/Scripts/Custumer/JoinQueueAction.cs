using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom: le pide a CustomerQueueManager un lugar en la fila.
/// Si hay lugar, guarda la posicion asignada en la variable de salida
/// QueueSlot para que NavigateToAction lleve al cliente hasta ahi.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Join Queue",
    story: "[Agent] joins the [QueueSlot]",
    category: "Action/Customer",
    id: "d4e5f60718293a4b5c6d7e8f90123456")]
public partial class JoinQueueAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> QueueSlot; // variable de salida

    protected override Status OnStart()
    {
        if (CustomerQueueManager.Instance == null)
        {
            Debug.LogWarning("[JoinQueueAction] No hay CustomerQueueManager en la escena.");
            return Status.Failure;
        }

        if (Agent?.Value == null)
            return Status.Failure;

        bool joined = CustomerQueueManager.Instance.TryJoinQueue(Agent.Value.transform, out Transform slot);
        if (!joined)
            return Status.Failure; // fila llena. Version simple v1: no hay logica de "esperar afuera" todavia.

        QueueSlot.Value = slot.gameObject;
        return Status.Success;
    }
}
