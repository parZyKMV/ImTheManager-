using UnityEngine;

/// <summary>
/// Lista de puntos en la tienda donde un cliente puede ir a "comprar" algo.
/// Version simple v1: no evita que dos clientes elijan el mismo punto.
/// </summary>
public class ShoppingPointManager : MonoBehaviour
{
    public static ShoppingPointManager Instance { get; private set; }

    [SerializeField] private Transform[] shoppingPoints;

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

    public Transform GetRandomShoppingPoint()
    {
        if (shoppingPoints == null || shoppingPoints.Length == 0)
        {
            Debug.LogWarning("[ShoppingPointManager] No hay shopping points configurados.");
            return null;
        }

        return shoppingPoints[Random.Range(0, shoppingPoints.Length)];
    }
}
