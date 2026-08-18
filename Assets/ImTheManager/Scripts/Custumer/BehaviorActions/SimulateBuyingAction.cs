using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom: simula al cliente "agarrando" un producto esperando un
/// tiempo fijo. Version simple v1: no hay animacion ni producto real todavia,
/// solo la pausa. Facil de reemplazar despues por logica real de agarrar
/// un Pickupable del estante.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Simulate Buying",
    story: "[Agent] picks up a product",
    category: "Action/Customer",
    id: "c3d4e5f60718293a4b5c6d7e8f901234")]
public partial class SimulateBuyingAction : Action
{
    [SerializeReference] public BlackboardVariable<float> Duration = new(1.5f);

    private float _elapsed;

    protected override Status OnStart()
    {
        _elapsed = 0f;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += UnityEngine.Time.deltaTime;
        return _elapsed >= Duration.Value ? Status.Success : Status.Running;
    }
}
