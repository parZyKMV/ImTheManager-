using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom: elige un wander point al azar de ShoppingPointManager y lo
/// guarda en la variable de salida WanderPoint, para que NavigateToAction
/// lo use despues. Se usa SOLO en la rama "explora primero" (ver
/// ShouldExploreFirstCondition) - es una variable separada de ShoppingPoint
/// a proposito, para no mezclar el punto de explorar con el de comprar.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Pick Wander Point",
    story: "[Agent] picks a [WanderPoint]",
    category: "Action/Customer",
    id: "6f708192a3b4c5d6e7f8091223344556")]
public partial class PickWanderPointAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> WanderPoint; // variable de salida

    protected override Status OnStart()
    {
        if (ShoppingPointManager.Instance == null)
        {
            Debug.LogWarning("[PickWanderPointAction] No hay ShoppingPointManager en la escena.");
            return Status.Failure;
        }

        Transform point = ShoppingPointManager.Instance.GetRandomWanderPoint();
        if (point == null)
            return Status.Failure;

        WanderPoint.Value = point.gameObject;
        return Status.Success;
    }
}
