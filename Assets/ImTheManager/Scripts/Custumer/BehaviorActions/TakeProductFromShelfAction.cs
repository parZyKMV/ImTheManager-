using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Reemplaza a SimulateBuyingAction: el cliente espera un momento (simulando
/// que elige el producto) y luego le quita una unidad real al ShelfSlot que
/// esta en el ShoppingPoint al que camino. Si el estante ya esta vacio, no
/// pasa nada (TakeOne() no hace nada por debajo de 0) pero el cliente sigue
/// su camino igual, para no trabarse.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Take Product From Shelf",
    story: "[Agent] takes a product from [ShoppingPoint]",
    category: "Action/Customer",
    id: "1a2b3c4d5e6f708192a3b4c5d6e7f809")]
public partial class TakeProductFromShelfAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> ShoppingPoint;
    [SerializeReference] public BlackboardVariable<float> Duration = new(1.5f);

    private float _elapsed;
    private bool _hasTaken;

    protected override Status OnStart()
    {
        _elapsed = 0f;
        _hasTaken = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed < Duration.Value)
            return Status.Running;

        if (!_hasTaken)
        {
            _hasTaken = true;

            ShelfSlot shelf = ShoppingPoint?.Value != null
                ? ShoppingPoint.Value.GetComponentInParent<ShelfSlot>()
                : null;

            var lifecycle = Agent?.Value != null ? Agent.Value.GetComponent<CustomerLifecycle>() : null;

            if (shelf != null)
            {
                bool hadStock = shelf.CurrentQuantity > 0;
                shelf.TakeOne();

                if (hadStock)
                {
                    lifecycle?.AddPurchasedProduct(shelf.ProductType);
                    Debug.Log($"[TakeProductFromShelfAction] {Agent?.Value?.name} tomo '{shelf.ProductType?.productName}' de '{shelf.name}'. Quedan {shelf.CurrentQuantity}/{shelf.MaxQuantity}.");
                }
                else
                {
                    Debug.Log($"[TakeProductFromShelfAction] '{shelf.name}' ya estaba vacio, {Agent?.Value?.name} se va con las manos vacias de ahi.");
                }
            }
            else
            {
                // Ya no es un error: el cliente puede haber elegido un punto
                // de "solo explorar" (sin ShelfSlot) a proposito, para que
                // no todos los clientes caminen exclusivamente entre estantes.
                Debug.Log($"[TakeProductFromShelfAction] {Agent?.Value?.name} estaba solo explorando esta zona, no hay productos aqui.");
            }

            // Nota: la queja por estante vacio y el desorden ya no se disparan
            // desde aca - ahora viven como nodos separados en el grafo
            // (ShouldComplainCondition/ComplainAction, ShouldCreateMessCondition/
            // CreateMessAction), colocados despues de este nodo en la Sequence.
        }

        return Status.Success;
    }
}