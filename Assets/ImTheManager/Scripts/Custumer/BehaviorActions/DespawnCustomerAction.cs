using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom: el cliente desaparece al llegar a la salida (fin de su ciclo
/// de vida). Version simple v1: Destroy directo. Si mas adelante usas
/// object pooling para los clientes, cambiar esto por Desactivar +
/// devolver al pool en vez de destruir.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Despawn Customer",
    story: "[Agent] leaves the store",
    category: "Action/Customer",
    id: "0718293a4b5c6d7e8f9012345678abcd")]
public partial class DespawnCustomerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null)
            return Status.Failure;

        UnityEngine.Object.Destroy(Agent.Value);
        return Status.Success;
    }
}