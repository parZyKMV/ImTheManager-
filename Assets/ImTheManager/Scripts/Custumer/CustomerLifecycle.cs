using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Va en el prefab del cliente. Lleva el estado de si ya fue atendido en la
/// caja, y ahora lleva la lista REAL de productos que fue tomando de los
/// estantes (via TakeProductFromShelfAction) en vez de asignar productos
/// al azar sin relacion con lo que compro de verdad.
/// </summary>
public class CustomerLifecycle : MonoBehaviour
{
    [Header("Perfil (arquetipo)")]
    [Tooltip("Define si compra, si se queja, si hace desorden, etc. Lo asigna CustomerSpawner al instanciar, o se deja fijo en el prefab para pruebas.")]
    [SerializeField] private CustomerProfile profile;

    public CustomerProfile Profile => profile;
    public bool WillBuy => profile == null || profile.willBuy; // sin perfil asignado, asume que compra (comportamiento anterior)

    /// <summary>Llamado por CustomerSpawner (cuando exista) al instanciar el cliente con un arquetipo especifico.</summary>
    public void SetProfile(CustomerProfile newProfile)
    {
        profile = newProfile;
    }

    public bool HasBeenServed { get; private set; } = false;
    public bool HasPlacedProducts { get; private set; } = false;

    private readonly List<ProductData> _purchasedProducts = new List<ProductData>();
    private readonly List<GameObject> _spawnedInstances = new List<GameObject>();

    /// <summary>Llamado por TakeProductFromShelfAction cada vez que el cliente toma algo real de un estante.</summary>
    public void AddPurchasedProduct(ProductData product)
    {
        if (product == null) return;
        _purchasedProducts.Add(product);
    }

    /// <summary>Usado por CreateMessAction (MisplacedProduct) para saber que producto puede dejar tirado. Null si no compro nada.</summary>
    public ProductData GetFirstPurchasedProductOrNull()
    {
        return _purchasedProducts.Count > 0 ? _purchasedProducts[0] : null;
    }

    /// <summary>
    /// Instancia sobre el mostrador exactamente los productos que el cliente
    /// tomo de verdad de los estantes. Llamado una sola vez, cuando llega
    /// al frente de la fila (desde WaitForTurnAction).
    /// </summary>
    public void PlaceProductsOnCounter()
    {
        if (HasPlacedProducts) return;
        HasPlacedProducts = true;

        Debug.Log($"[CustomerLifecycle] {name}: colocando {_purchasedProducts.Count} producto(s) comprado(s) de verdad en el mostrador.");

        if (_purchasedProducts.Count == 0)
        {
            Debug.LogWarning($"[CustomerLifecycle] {name}: no tomo ningun producto de un estante, llega a la caja con las manos vacias.");
            return;
        }

        if (CounterDropPointManager.Instance == null)
        {
            Debug.LogWarning("[CustomerLifecycle] No hay CounterDropPointManager en la escena.");
            return;
        }

        for (int i = 0; i < _purchasedProducts.Count; i++)
        {
            ProductData product = _purchasedProducts[i];

            if (product.prefab == null)
            {
                Debug.LogWarning($"[CustomerLifecycle] {name}: el ProductData '{product.productName}' no tiene 'Prefab' asignado.");
                continue;
            }

            Transform dropPoint = CounterDropPointManager.Instance.GetDropPoint(i);
            if (dropPoint == null) continue;

            GameObject instance = Instantiate(product.prefab, dropPoint.position, dropPoint.rotation);
            _spawnedInstances.Add(instance);

            Debug.Log($"[CustomerLifecycle] {name}: coloco '{product.productName}' en {dropPoint.name}.");
        }
    }

    public void MarkServed()
    {
        HasBeenServed = true;
    }
}