using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

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

        return lifecycle.HasBeenServed ? Status.Success : Status.Running;
    }
}