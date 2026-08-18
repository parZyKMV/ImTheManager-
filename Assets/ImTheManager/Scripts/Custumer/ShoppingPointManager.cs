using UnityEngine;

/// <summary>
/// Puntos de la tienda para el recorrido del cliente, separados en 2 listas
/// explicitas:
/// - shoppingPoints: SIEMPRE tienen un ShelfSlot (hay algo que comprar ahi).
/// - wanderPoints: zonas sin productos (solo para explorar/pasear).
/// Version simple v1: no evita que dos clientes elijan el mismo punto.
/// </summary>
public class ShoppingPointManager : MonoBehaviour
{
    public static ShoppingPointManager Instance { get; private set; }

    [Header("Puntos con estante (siempre tienen ShelfSlot)")]
    [SerializeField] private Transform[] shoppingPoints;

    [Header("Puntos de exploracion (sin productos)")]
    [SerializeField] private Transform[] wanderPoints;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Elige un punto de compra al azar. Deberia SIEMPRE tener un ShelfSlot.</summary>
    public Transform GetRandomShoppingPoint()
    {
        if (shoppingPoints == null || shoppingPoints.Length == 0)
        {
            Debug.LogWarning("[ShoppingPointManager] No hay shopping points configurados.");
            return null;
        }

        return shoppingPoints[Random.Range(0, shoppingPoints.Length)];
    }

    /// <summary>Elige un punto de exploracion al azar (sin productos).</summary>
    public Transform GetRandomWanderPoint()
    {
        if (wanderPoints == null || wanderPoints.Length == 0)
        {
            Debug.LogWarning("[ShoppingPointManager] No hay wander points configurados.");
            return null;
        }

        return wanderPoints[Random.Range(0, wanderPoints.Length)];
    }
}