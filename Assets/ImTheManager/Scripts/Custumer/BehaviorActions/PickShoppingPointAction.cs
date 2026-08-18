using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo custom: elige un punto de compra al azar de ShoppingPointManager
/// y lo guarda en la variable de salida ShoppingPoint para que
/// NavigateToAction lo use despues.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Pick Shopping Point",
    story: "[Agent] picks a [ShoppingPoint]",
    category: "Action/Customer",
    id: "b2c3d4e5f60718293a4b5c6d7e8f9012")]
public partial class PickShoppingPointAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ShoppingPoint; // variable de salida

    protected override Status OnStart()
    {
        if (ShoppingPointManager.Instance == null)
        {
            Debug.LogWarning("[PickShoppingPointAction] No hay ShoppingPointManager en la escena.");
            return Status.Failure;
        }

        Transform point = ShoppingPointManager.Instance.GetRandomShoppingPoint();
        if (point == null)
            return Status.Failure;

        ShoppingPoint.Value = point.gameObject;
        return Status.Success;
    }
}
