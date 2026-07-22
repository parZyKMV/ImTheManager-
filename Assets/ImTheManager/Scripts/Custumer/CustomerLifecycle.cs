using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Va en el prefab del cliente. Lleva el estado de si ya fue atendido en la
/// caja, y ahora tambien que productos "compro" y los coloca fisicamente
/// en el mostrador cuando llega al frente de la fila.
/// </summary>
public class CustomerLifecycle : MonoBehaviour
{
    [Header("Productos asignados")]
    [SerializeField] private int minProducts = 1;
    [SerializeField] private int maxProducts = 3;

    public bool HasBeenServed { get; private set; } = false;
    public bool HasPlacedProducts { get; private set; } = false;

    private ProductData[] _assignedProducts;
    private readonly List<GameObject> _spawnedInstances = new List<GameObject>();

    void Awake()
    {
        AssignRandomProducts();
    }

    void AssignRandomProducts()
    {
        if (ProductCatalog.Instance == null)
        {
            Debug.LogWarning("[CustomerLifecycle] No hay ProductCatalog en la escena.");
            _assignedProducts = new ProductData[0];
            return;
        }

        _assignedProducts = ProductCatalog.Instance.GetRandomProducts(minProducts, maxProducts);
    }

    /// <summary>
    /// Instancia los productos asignados sobre el mostrador. Llamado una sola
    /// vez, cuando el cliente llega al frente de la fila (desde WaitForTurnAction).
    /// </summary>
    public void PlaceProductsOnCounter()
    {
        if (HasPlacedProducts) return;
        HasPlacedProducts = true;

        if (CounterDropPointManager.Instance == null)
        {
            Debug.LogWarning("[CustomerLifecycle] No hay CounterDropPointManager en la escena.");
            return;
        }

        for (int i = 0; i < _assignedProducts.Length; i++)
        {
            ProductData product = _assignedProducts[i];
            if (product == null || product.prefab == null) continue;

            Transform dropPoint = CounterDropPointManager.Instance.GetDropPoint(i);
            if (dropPoint == null) continue;

            GameObject instance = Instantiate(product.prefab, dropPoint.position, dropPoint.rotation);
            _spawnedInstances.Add(instance);
        }
    }

    public void MarkServed()
    {
        HasBeenServed = true;
    }
}