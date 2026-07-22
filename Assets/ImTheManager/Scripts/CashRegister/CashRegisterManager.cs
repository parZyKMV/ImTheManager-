using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class ProductDataEvent : UnityEvent<ProductData> { }
[System.Serializable] public class FloatEvent : UnityEvent<float> { }
[System.Serializable] public class BoolEvent : UnityEvent<bool> { }

/// <summary>
/// Cerebro de la caja registradora. Lleva el registro de productos escaneados,
/// calcula el total, determina cuanto paga el "cliente", y valida el cambio
/// que el jugador le da de vuelta.
/// </summary>
public class CashRegisterManager : MonoBehaviour
{
    public static CashRegisterManager Instance { get; private set; }

    public enum RegisterState { Scanning, WaitingForCharge, GivingChange, Complete }

    [Header("Pago del cliente")]
    [Tooltip("Denominaciones disponibles para que el 'cliente' pague, de menor a mayor. " +
             "El cliente paga con la mas chica que alcance a cubrir el total.")]
    [SerializeField] private float[] paymentDenominations = { 5f, 10f, 20f, 50f, 100f };

    [Header("Eventos (conecta tu UI/audio aqui)")]
    public ProductDataEvent onItemScanned;      // se dispara cada vez que se escanea un producto
    public FloatEvent onTotalUpdated;           // se dispara con el nuevo total cada vez que cambia
    public FloatEvent onChangeRequested;        // se dispara con el cambio a dar cuando empieza el mini-juego
    public BoolEvent onChangeResult;            // true = cambio correcto, false = incorrecto
    public UnityEvent onTransactionComplete;    // se dispara al terminar toda la venta

    public RegisterState CurrentState { get; private set; } = RegisterState.Scanning;
    public float CurrentTotal { get; private set; } = 0f;
    public float AmountPaidByCustomer { get; private set; } = 0f;
    public float ChangeOwed { get; private set; } = 0f;

    private readonly List<ProductData> _scannedItems = new List<ProductData>();

    // Margen de tolerancia para redondeo de floats al comparar montos de dinero.
    private const float MoneyEpsilon = 0.001f;

    void Awake()
    {
        // Singleton simple: asume una sola caja registradora activa en la escena.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CashRegisterManager] Ya existe una instancia. Destruyendo duplicado.");
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

    // ===== ESCANEO ==========================================================

    /// <summary>
    /// Llamado por RegisterScanner cuando un producto entra al area de escaneo.
    /// </summary>
    public void ScanProduct(ProductData product)
    {
        if (CurrentState != RegisterState.Scanning)
        {
            Debug.LogWarning("[CashRegisterManager] No se puede escanear: la caja no esta en modo Scanning.");
            return;
        }

        if (product == null) return;

        _scannedItems.Add(product);
        CurrentTotal += product.price;

        onItemScanned?.Invoke(product);
        onTotalUpdated?.Invoke(CurrentTotal);
    }

    // ===== COBRO =============================================================

    /// <summary>
    /// Llamalo desde tu boton/tecla de "Cobrar" cuando el jugador termino de escanear.
    /// Calcula con cuanto "paga" el cliente y arranca el mini-juego de cambio.
    /// </summary>
    public void FinishScanningAndCharge()
    {
        if (CurrentState != RegisterState.Scanning) return;

        if (_scannedItems.Count == 0)
        {
            Debug.LogWarning("[CashRegisterManager] No hay productos escaneados todavia.");
            return;
        }

        CurrentState = RegisterState.WaitingForCharge;

        AmountPaidByCustomer = DeterminePaymentAmount(CurrentTotal);
        ChangeOwed = Mathf.Max(0f, AmountPaidByCustomer - CurrentTotal);

        CurrentState = RegisterState.GivingChange;
        onChangeRequested?.Invoke(ChangeOwed);
    }

    // Elige la denominacion mas chica de la lista que alcance a cubrir el total,
    // simulando que el cliente paga con el billete "logico" mas cercano.
    private float DeterminePaymentAmount(float total)
    {
        if (paymentDenominations == null || paymentDenominations.Length == 0)
        {
            Debug.LogError("[CashRegisterManager] No hay 'Payment Denominations' configuradas. Usando el total exacto como fallback.");
            return total;
        }

        foreach (float denomination in paymentDenominations)
        {
            if (denomination >= total)
                return denomination;
        }

        // Si el total supera todas las denominaciones definidas, sube al siguiente
        // multiplo de la denominacion mas grande disponible.
        float largest = paymentDenominations[paymentDenominations.Length - 1];
        return Mathf.Ceil(total / largest) * largest;
    }

    // ===== CAMBIO =============================================================

    /// <summary>
    /// Llamalo desde ChangeMinigameController cuando el jugador confirma
    /// cuanto cambio le esta dando al cliente.
    /// </summary>
    public void SubmitChange(float amountGiven)
    {
        if (CurrentState != RegisterState.GivingChange) return;

        bool isCorrect = Mathf.Abs(amountGiven - ChangeOwed) < MoneyEpsilon;

        onChangeResult?.Invoke(isCorrect);

        CurrentState = RegisterState.Complete;
        onTransactionComplete?.Invoke();
    }

    // ===== RESET PARA EL SIGUIENTE CLIENTE ===================================

    /// <summary>
    /// Llamalo para limpiar la transaccion y quedar listo para el proximo cliente.
    /// </summary>
    public void ResetTransaction()
    {
        _scannedItems.Clear();
        CurrentTotal = 0f;
        AmountPaidByCustomer = 0f;
        ChangeOwed = 0f;
        CurrentState = RegisterState.Scanning;

        onTotalUpdated?.Invoke(CurrentTotal);
    }
}
