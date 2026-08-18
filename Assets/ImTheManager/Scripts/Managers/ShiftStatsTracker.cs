using UnityEngine;

/// <summary>
/// Fuente unica de verdad para las estadisticas del turno actual: dinero
/// ganado y clientes atendidos. Se resetea solo cuando empieza un dia nuevo
/// (escucha DayCycleManager.onDayStarted). Tanto HUDController como
/// EndOfShiftUI leen de aca en vez de contar cada uno por su lado.
/// </summary>
public class ShiftStatsTracker : MonoBehaviour
{
    public static ShiftStatsTracker Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField] private CashRegisterManager registerManager;

    [Header("Eventos")]
    public FloatEvent onMoneyChanged;           // dinero total ganado en el turno
    public IntEvent onCustomersServedChanged;   // clientes atendidos en el turno

    public float MoneyEarnedThisShift { get; private set; } = 0f;
    public int CustomersServedThisShift { get; private set; } = 0;

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

        if (dayCycleManager != null)
            dayCycleManager.onDayStarted.RemoveListener(HandleDayStarted);

        if (registerManager != null)
            registerManager.onTransactionComplete.RemoveListener(HandleTransactionComplete);
    }

    void Start()
    {
        if (dayCycleManager != null)
            dayCycleManager.onDayStarted.AddListener(HandleDayStarted);

        if (registerManager != null)
            registerManager.onTransactionComplete.AddListener(HandleTransactionComplete);
    }

    void HandleDayStarted(int day)
    {
        MoneyEarnedThisShift = 0f;
        CustomersServedThisShift = 0;

        onMoneyChanged?.Invoke(MoneyEarnedThisShift);
        onCustomersServedChanged?.Invoke(CustomersServedThisShift);
    }

    // Se dispara con CUALQUIER transaccion completada (correcta o no) - la
    // caja ya cobro el total, sin importar si el cambio estuvo bien.
    void HandleTransactionComplete()
    {
        if (registerManager == null) return;

        MoneyEarnedThisShift += registerManager.CurrentTotal;
        CustomersServedThisShift++;

        onMoneyChanged?.Invoke(MoneyEarnedThisShift);
        onCustomersServedChanged?.Invoke(CustomersServedThisShift);
    }
}
