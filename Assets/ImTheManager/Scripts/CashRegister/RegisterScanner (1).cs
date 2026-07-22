using UnityEngine;

/// <summary>
/// Va en un Collider (Is Trigger = true) ubicado en la zona de escaneo de la caja.
/// Cuando un producto (con ScannableProduct) entra en esta zona, se escanea
/// automaticamente y se le avisa al CashRegisterManager.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RegisterScanner : MonoBehaviour
{
    [Header("Feedback (opcional)")]
    [SerializeField] private AudioSource beepAudioSource; // sonido de "beep" al escanear

    void Awake()
    {
        // Aseguramos que el collider de este objeto sea un trigger, por si
        // alguien lo olvida marcar en el Inspector.
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[RegisterScanner] El collider deberia ser Trigger. Corrigiendo automaticamente.");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // GetComponentInParent porque el collider del producto puede estar
        // en un hijo del objeto que realmente tiene ScannableProduct.
        ScannableProduct scannable = other.GetComponentInParent<ScannableProduct>();

        if (scannable == null) return;
        if (scannable.HasBeenScanned) return; // evita doble conteo mientras sigue dentro del trigger

        if (CashRegisterManager.Instance == null)
        {
            Debug.LogWarning("[RegisterScanner] No hay un CashRegisterManager en la escena.");
            return;
        }

        
        scannable.MarkAsScanned();
        CashRegisterManager.Instance.ScanProduct(scannable.ProductData);
        Debug.Log($"[RegisterScanner] Escaneado: {scannable.ProductData.productName} (${scannable.ProductData.price})");

        if (beepAudioSource != null)
            beepAudioSource.Play();
    }
}
