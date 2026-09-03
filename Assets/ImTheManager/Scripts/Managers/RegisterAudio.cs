using UnityEngine;

/// <summary>
/// Sonido de "compra completada" - suena cada vez que una transaccion
/// termina en la caja, sin importar si el cambio fue correcto o no
/// (CashRegisterManager.onTransactionComplete se dispara para cualquiera).
/// </summary>
public class RegisterAudio : MonoBehaviour
{
    [SerializeField] private CashRegisterManager registerManager;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip purchaseCompleteClip;

    void Start()
    {
        if (registerManager != null)
            registerManager.onTransactionComplete.AddListener(PlayPurchaseSound);
    }

    void OnDestroy()
    {
        if (registerManager != null)
            registerManager.onTransactionComplete.RemoveListener(PlayPurchaseSound);
    }

    void PlayPurchaseSound()
    {
        if (audioSource != null && purchaseCompleteClip != null)
            audioSource.PlayOneShot(purchaseCompleteClip);
    }
}
