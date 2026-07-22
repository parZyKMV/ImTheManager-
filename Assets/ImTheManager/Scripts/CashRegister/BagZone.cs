using UnityEngine;

/// <summary>
/// Trigger que representa la bolsa. Solo acepta productos que ya fueron
/// escaneados (evita que el jugador se salte el escaneo arrastrando
/// directo a la bolsa).
/// </summary>
[RequireComponent(typeof(Collider))]
public class BagZone : MonoBehaviour
{
    [SerializeField] private Transform bagContainer; // punto donde se acomodan los productos embolsados

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[BagZone] El collider deberia ser Trigger. Corrigiendo automaticamente.");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        ScannableProduct scannable = other.GetComponentInParent<ScannableProduct>();
        if (scannable == null) return;

        // Solo se puede embolsar si ya fue escaneado.
        if (!scannable.HasBeenScanned) return;

        var rb = scannable.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;

        if (bagContainer != null)
        {
            scannable.transform.SetParent(bagContainer);

            Debug.Log($"[BagZone] Producto embolsado: {scannable.ProductData.productName} x{scannable.ProductData}");
            // Separa cada producto para que no queden todos apilados en el mismo punto.
            // Un offset simple basado en cuantos productos ya hay en la bolsa.
            int indexInBag = bagContainer.childCount - 1;
            scannable.transform.localPosition = new Vector3(0f, indexInBag * 0.15f, 0f);
        }

        // Ya no necesita seguir interactuando con nada (arrastre, otros triggers, etc).
        other.enabled = false;

        // Si prefieres que el producto desaparezca del todo al embolsarse
        // (en vez de quedar visible apilado), descomenta esta linea:
        // scannable.gameObject.SetActive(false);
    }
}
