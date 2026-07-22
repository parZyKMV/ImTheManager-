using UnityEngine;

/// <summary>
/// Catalogo de todos los productos disponibles en la tienda. Se usa para
/// asignarle productos al azar a cada cliente cuando aparece.
/// </summary>
public class ProductCatalog : MonoBehaviour
{
    public static ProductCatalog Instance { get; private set; }

    [SerializeField] private ProductData[] allProducts;

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

    /// <summary>Devuelve entre min y max productos al azar (con repeticion permitida).</summary>
    public ProductData[] GetRandomProducts(int min, int max)
    {
        if (allProducts == null || allProducts.Length == 0)
        {
            Debug.LogWarning("[ProductCatalog] No hay productos configurados.");
            return new ProductData[0];
        }

        int count = Random.Range(min, max + 1);
        var result = new ProductData[count];

        for (int i = 0; i < count; i++)
            result[i] = allProducts[Random.Range(0, allProducts.Length)];

        return result;
    }
}