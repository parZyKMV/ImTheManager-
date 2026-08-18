using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo Action: el cliente deja un desorden real. Elige un tipo al azar de
/// profile.possibleMessTypes y dispara la mecanica correspondiente:
/// - ShelfDisorder: desordena el ShelfSlot al que vino (necesita ShoppingPoint)
/// - Trash: instancia un TrashItem en su posicion actual
/// - MisplacedProduct: instancia un producto real (de lo que compro) en su posicion actual
/// Solo se ejecuta cuando ShouldCreateMessCondition (antes en la misma Sequence) ya evaluo true.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Create Mess",
    story: "[Agent] creates a mess",
    category: "Action/Customer",
    id: "4d5e6f708192a3b4c5d6e7f809122334")]
public partial class CreateMessAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> ShoppingPoint; // necesario solo para ShelfDisorder
    [SerializeReference] public BlackboardVariable<GameObject> TrashPrefab; // prefab con TrashItem
    [SerializeReference] public BlackboardVariable<string> PickupableLayerName = new("Pickupable");

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var lifecycle = Agent.Value.GetComponent<CustomerLifecycle>();
        var profile = lifecycle?.Profile;

        if (profile == null || profile.possibleMessTypes == null || profile.possibleMessTypes.Length == 0)
        {
            Debug.LogWarning("[CreateMessAction] No hay CustomerProfile o 'Possible Mess Types' vacio.");
            return Status.Failure;
        }

        // Si el ShoppingPoint no tiene estante (zona de "solo explorar"),
        // ShelfDisorder y MisplacedProduct no tienen sentido - solo Trash
        // funciona en cualquier parte de la tienda.
        bool hasShelf = ShoppingPoint?.Value != null
            && ShoppingPoint.Value.GetComponentInParent<ShelfSlot>() != null;

        System.Collections.Generic.List<MessType> validTypes = new System.Collections.Generic.List<MessType>();
        foreach (var type in profile.possibleMessTypes)
        {
            if (!hasShelf && type != MessType.Trash) continue;
            validTypes.Add(type);
        }

        if (validTypes.Count == 0)
        {
            // Nada valido para hacer en esta zona (ej. perfil solo tiene
            // ShelfDisorder configurado, pero el cliente esta en una zona sin estante).
            return Status.Success;
        }

        MessType chosenType = validTypes[UnityEngine.Random.Range(0, validTypes.Count)];

        switch (chosenType)
        {
            case MessType.ShelfDisorder:
                CreateShelfDisorder();
                break;
            case MessType.Trash:
                CreateTrash();
                break;
            case MessType.MisplacedProduct:
                CreateMisplacedProduct(lifecycle);
                break;
        }

        return Status.Success;
    }

    void CreateShelfDisorder()
    {
        ShelfSlot shelf = ShoppingPoint?.Value != null
            ? ShoppingPoint.Value.GetComponentInParent<ShelfSlot>()
            : null;

        if (shelf == null)
        {
            Debug.LogWarning("[CreateMessAction] ShelfDisorder elegido pero no hay ShelfSlot en el ShoppingPoint.");
            return;
        }

        shelf.MakeDisordered();
        Debug.Log($"[CreateMessAction] {Agent.Value.name} desordeno el estante '{shelf.name}'.");
    }

    void CreateTrash()
    {
        if (TrashPrefab?.Value == null)
        {
            Debug.LogWarning("[CreateMessAction] Trash elegido pero no hay 'Trash Prefab' asignado en el nodo.");
            return;
        }

        UnityEngine.Object.Instantiate(TrashPrefab.Value, Agent.Value.transform.position, Quaternion.identity);
        Debug.Log($"[CreateMessAction] {Agent.Value.name} tiro basura en {Agent.Value.transform.position}.");
    }

    void CreateMisplacedProduct(CustomerLifecycle lifecycle)
    {
        // Usamos el producto del estante al que vino a mirar, NO el que ya
        // compro - en el orden recomendado del grafo (Browse -> Quejas/
        // Desorden -> Compra), este nodo corre ANTES de Take Product From
        // Shelf, asi que el cliente todavia no tiene nada en su lista de compras.
        ShelfSlot shelf = ShoppingPoint?.Value != null
            ? ShoppingPoint.Value.GetComponentInParent<ShelfSlot>()
            : null;

        if (shelf == null || shelf.ProductType == null || shelf.ProductType.prefab == null)
        {
            Debug.LogWarning("[CreateMessAction] MisplacedProduct elegido pero no hay un producto valido en el ShoppingPoint.");
            return;
        }

        if (shelf.CurrentQuantity <= 0)
        {
            Debug.Log("[CreateMessAction] MisplacedProduct elegido pero el estante ya estaba vacio, no hay nada que dejar tirado.");
            return;
        }

        // El producto sale fisicamente del estante (se descuenta el stock real),
        // solo que en vez de ir a la caja termina tirado en otro lado.
        shelf.TakeOne();

        // El prefab base vive en la layer "CounterItem" (la que usa el
        // arrastre point-and-click de la caja registradora) - hay que forzarlo
        // a "Pickupable" para que PlayerInteractor lo pueda detectar y agarrar.
        GameObject instance = UnityEngine.Object.Instantiate(shelf.ProductType.prefab, Agent.Value.transform.position, Quaternion.identity);

        string layerName = PickupableLayerName?.Value ?? "Pickupable";
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            SetLayerRecursively(instance, layer);
        else
            Debug.LogWarning($"[CreateMessAction] No existe la layer '{layerName}'.");

        Debug.Log($"[CreateMessAction] {Agent.Value.name} dejo '{shelf.ProductType.productName}' fuera de lugar en {Agent.Value.transform.position}.");

        instance.AddComponent<MisplacedProductMarker>();
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}