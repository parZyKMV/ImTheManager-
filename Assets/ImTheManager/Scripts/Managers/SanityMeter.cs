using UnityEngine;
using UnityEngine.Events;

[System.Serializable] public class SanityChangedEvent : UnityEvent<float> { } // valor actual (0-100)
[System.Serializable] public class SanityStressEvent : UnityEvent<float, string> { } // cantidad, origen

/// <summary>
/// Medidor central de estres del jugador (0-100). Cualquier sistema del
/// juego (eventos Karen, errores en la caja, ninos llorando, equipo roto,
/// etc.) le reporta de forma independiente vía AddStress(). No depende de
/// nada mas — todo lo demas depende de este.
/// </summary>
public class SanityMeter : MonoBehaviour
{
    public static SanityMeter Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float minSanity = 0f;

    [Header("Estado actual (solo lectura, para debug)")]
    [SerializeField] private float currentStress = 0f; // 0 = tranquilo, maxSanity = Rage Mode

    [Header("Eventos")]
    public SanityChangedEvent onSanityChanged; // se dispara con el valor actual cada vez que cambia
    public SanityStressEvent onStressAdded;    // se dispara con (cantidad, origen) en cada AddStress
    public UnityEvent onMeterFull;             // se dispara UNA vez al llegar al maximo

    public float CurrentStress => currentStress;
    public float NormalizedStress => Mathf.InverseLerp(minSanity, maxSanity, currentStress);

    private bool _hasFiredMeterFull = false;

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

    /// <summary>
    /// Agrega (o resta, con un valor negativo) estres al medidor. El
    /// parametro 'source' es solo para debug/logs/UI (ej. "Karen", "CashRegister", "BrokenEquipment").
    /// </summary>
    public void AddStress(float amount, string source)
    {
        float previous = currentStress;
        currentStress = Mathf.Clamp(currentStress + amount, minSanity, maxSanity);

        onStressAdded?.Invoke(amount, source);

        if (!Mathf.Approximately(previous, currentStress))
            onSanityChanged?.Invoke(currentStress);

        if (currentStress >= maxSanity && !_hasFiredMeterFull)
        {
            _hasFiredMeterFull = true;
            onMeterFull?.Invoke();
        }

        // Si baja del maximo (ej. RageModeController lo resetea despues de
        // desahogarse), permitimos que "meter full" se pueda disparar de nuevo.
        if (currentStress < maxSanity)
            _hasFiredMeterFull = false;
    }

    /// <summary>Reinicia el medidor a 0. Uso tipico: al empezar un nuevo turno/dia.</summary>
    public void ResetMeter()
    {
        currentStress = minSanity;
        _hasFiredMeterFull = false;
        onSanityChanged?.Invoke(currentStress);
    }
}
