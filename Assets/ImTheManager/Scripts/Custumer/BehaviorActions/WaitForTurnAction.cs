using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom: se queda en Running hasta que el cliente realmente fue
/// atendido en la caja (CustomerLifecycle.HasBeenServed). Ademas, apenas
/// llega al frente de la fila, coloca sus productos sobre el mostrador.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait For Turn",
    story: "[Agent] waits for their turn",
    category: "Action/Customer",
    id: "e5f60718293a4b5c6d7e8f9012345678")]
public partial class WaitForTurnAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    private bool _hasAnnouncedTurn;

    protected override Status OnStart()
    {
        _hasAnnouncedTurn = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (CustomerQueueManager.Instance == null || Agent?.Value == null)
            return Status.Failure;

        // Si lo desplazaron de su lugar en la fila (ej. lo agarraron y
        // lanzaron durante Rage Mode), lo hacemos volver caminando solo.
        ReturnToSlotIfDisplaced();

        bool isAtFront = CustomerQueueManager.Instance.IsAtFrontOfQueue(Agent.Value.transform);
        var lifecycle = Agent.Value.GetComponent<CustomerLifecycle>();

        if (isAtFront && !_hasAnnouncedTurn)
        {
            _hasAnnouncedTurn = true;
            Debug.Log($"[WaitForTurnAction] {Agent.Value.name} llego al frente de la fila.");
            lifecycle?.PlaceProductsOnCounter();
        }

        if (lifecycle == null)
        {
            Debug.LogWarning("[WaitForTurnAction] El cliente no tiene CustomerLifecycle.");
            return Status.Running;
        }

        // Se completa unicamente cuando RegisterModeController marca a este
        // cliente como atendido tras una transaccion (cobro + cambio) exitosa.
        return lifecycle.HasBeenServed ? Status.Success : Status.Running;
    }

    // Si el cliente termino lejos de su slot asignado (ej. lo agarraron y
    // lanzaron durante Rage Mode mientras esperaba en la fila), le vuelve
    // a pedir el destino - el mismo patron de auto-recuperacion que usa
    // NavigateToAction para el mismo tipo de situacion.
    void ReturnToSlotIfDisplaced()
    {
        Transform assignedSlot = CustomerQueueManager.Instance.GetSlotForCustomer(Agent.Value.transform);
        if (assignedSlot == null) return;

        var navAgent = Agent.Value.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent == null || !navAgent.enabled) return;

        // Ya tiene un camino en curso o esta lo bastante cerca - no hace falta nada.
        if (navAgent.hasPath || navAgent.pathPending) return;

        float distanceToSlot = Vector3.Distance(Agent.Value.transform.position, assignedSlot.position);
        if (distanceToSlot > navAgent.stoppingDistance + 0.1f)
            navAgent.SetDestination(assignedSlot.position);
    }
}