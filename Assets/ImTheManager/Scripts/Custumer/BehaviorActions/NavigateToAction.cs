using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom de Unity Behavior: mueve el NavMeshAgent del Agent hacia el
/// GameObject Target. Devuelve Running mientras viaja, Success al llegar.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Navigate To (NavMesh)",
    story: "[Agent] navigates to [Target]",
    category: "Action/Movement",
    id: "a1b2c3d4e5f60718293a4b5c6d7e8f90")]
public partial class NavigateToAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> StoppingDistance = new(0.3f);

    private NavMeshAgent _navAgent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null || Target?.Value == null)
            return Status.Failure;

        _navAgent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_navAgent == null)
        {
            Debug.LogError("[NavigateToAction] El Agent no tiene NavMeshAgent.");
            return Status.Failure;
        }

        _navAgent.stoppingDistance = StoppingDistance.Value;
        _navAgent.SetDestination(Target.Value.transform.position);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_navAgent == null) return Status.Failure;

        // Todavia calculando el camino: seguimos esperando.
        if (_navAgent.pathPending)
            return Status.Running;

        bool arrived = _navAgent.remainingDistance <= _navAgent.stoppingDistance
                       && (!_navAgent.hasPath || _navAgent.velocity.sqrMagnitude < 0.01f);

        return arrived ? Status.Success : Status.Running;
    }
}