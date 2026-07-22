using UnityEngine;

/// <summary>
/// Se agrega al prefab de un producto fisico (junto a Pickupable), y lo conecta
/// con sus datos de precio/nombre (ProductData). RegisterScanner lo detecta
/// cuando el jugador acerca el producto al area de escaneo.
/// </summary>
public class ScannableProduct : MonoBehaviour
{
    [SerializeField] private ProductData productData;

    // Evita que el mismo producto se escanee varias veces mientras
    // se queda parado dentro del trigger del escaner.
    public bool HasBeenScanned { get; private set; } = false;

    public ProductData ProductData => productData;

    public void MarkAsScanned()
    {
        HasBeenScanned = true;
    }

    /// <summary>
    /// Reinicia el estado de escaneado. Llamalo si el producto se vuelve
    /// a recoger (por ejemplo, desde Pickupable.OnPickedUp), por si el jugador
    /// lo saca de la caja y lo quiere volver a escanear.
    ///
    /// TODO: cuando armemos el spawner de clientes / pooling de productos,
    /// hay que llamar esto al reutilizar un prefab de producto para un
    /// nuevo cliente. Ahora mismo nada lo llama todavia.
    /// </summary>
    public void ResetScanState()
    {
        HasBeenScanned = false;
    }
}
