using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom: obtiene el punto de salida de la tienda (StoreExitPoint)
/// y lo guarda en la variable de salida ExitPoint, para que NavigateToAction
/// lleve al cliente hasta ahi.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Get Exit Point",
    story: "[Agent] finds the store [ExitPoint]",
    category: "Action/Customer",
    id: "f60718293a4b5c6d7e8f9012345678ab")]
public partial class GetExitPointAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ExitPoint; // variable de salida

    protected override Status OnStart()
    {
        if (StoreExitPoint.Instance == null)
        {
            Debug.LogWarning("[GetExitPointAction] No hay StoreExitPoint en la escena.");
            return Status.Failure;
        }

        ExitPoint.Value = StoreExitPoint.Instance.gameObject;
        return Status.Success;
    }
}
