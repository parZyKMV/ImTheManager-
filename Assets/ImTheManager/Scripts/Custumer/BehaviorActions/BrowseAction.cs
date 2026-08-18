using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo generico: el cliente se queda "mirando" un momento. Usado tanto en
/// la rama de compra normal (antes de tomar el producto) como en la rama
/// "Solo mirando" (que nunca llega a Take Product From Shelf).
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Browse",
    story: "[Agent] browses for a while",
    category: "Action/Customer",
    id: "2b3c4d5e6f708192a3b4c5d6e7f80912")]
public partial class BrowseAction : Action
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
        _elapsed += Time.deltaTime;
        return _elapsed >= Duration.Value ? Status.Success : Status.Running;
    }
}
