using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Escucha SanityMeter.onMeterFull. Mientras esta activo, los props
/// marcados como "knockable" pasan de kinematic a fisica real (Rigidbody
/// normal, sin fractura/destruccion) para que el jugador los pueda empujar/
/// tirar. Lleva la cuenta de cuantos se cayeron, para el reporte de fin de
/// turno (EndOfShiftUI).
/// </summary>
public class RageModeController : MonoBehaviour
{
    public static RageModeController Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float rageModeDuration = 10f;
    [Tooltip("A cuanto estres vuelve el SanityMeter al terminar Rage Mode. No a 0 del todo - se calma pero queda algo de tension.")]
    [SerializeField] private float sanityAfterRage = 30f;

    [Header("Props afectados")]
    [Tooltip("Props que pasan a fisica real durante Rage Mode. Deben tener Rigidbody (normalmente kinematic).")]
    [SerializeField] private Rigidbody[] knockableProps;

    [Header("Player RageMode Stats")]
    [SerializeField] RPS_ThirdPersonController playercontroller;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float jumpMultiplier = 1.5f;
    [SerializeField] PlayerInteractor playerInteractor;
    [SerializeField] private float throwForceMultiplier = 1.5f;

    [Header("Eventos")]
    public UnityEvent onRageModeStarted;
    public UnityEvent onRageModeEnded;

    public bool IsActive { get; private set; } = false;
    public int PropsKnockedOverThisRage { get; private set; } = 0;
    public int TotalPropsKnockedOverThisShift { get; private set; } = 0;

    private float _timer = 0f;
    private readonly HashSet<Rigidbody> _knockedThisRage = new HashSet<Rigidbody>();

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

        if (SanityMeter.Instance != null)
            SanityMeter.Instance.onMeterFull.RemoveListener(StartRageMode);

        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.onDayStarted.RemoveListener(HandleDayStarted);
    }

    void Start()
    {
        if (SanityMeter.Instance != null)
            SanityMeter.Instance.onMeterFull.AddListener(StartRageMode);

        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.onDayStarted.AddListener(HandleDayStarted);

        
    }

    void HandleDayStarted(int day)
    {
        TotalPropsKnockedOverThisShift = 0;
    }

    void Update()
    {
        if (!IsActive) return;

        _timer += Time.deltaTime;
        if (_timer >= rageModeDuration)
            EndRageMode();
    }

    public void StartRageMode()
    {
        if (IsActive) return;

        IsActive = true;
        _timer = 0f;
        PropsKnockedOverThisRage = 0;
        _knockedThisRage.Clear();

        playercontroller.walkSpeed = playercontroller.walkSpeed * speedMultiplier;
        playercontroller.jumpForce = playercontroller.jumpForce * speedMultiplier;
        playerInteractor.throwForce = playerInteractor.throwForce * throwForceMultiplier;

        SetPropsPhysicsEnabled(true);

        onRageModeStarted?.Invoke();
    }

    void EndRageMode()
    {
        IsActive = false;
        SetPropsPhysicsEnabled(false);

        TotalPropsKnockedOverThisShift += PropsKnockedOverThisRage;

        // El estres baja pero no queda en 0 - se descarga pero no "resetea"
        // como si nada hubiera pasado.
        if (SanityMeter.Instance != null)
        {
            SanityMeter.Instance.ResetMeter();
            SanityMeter.Instance.AddStress(sanityAfterRage, "RageModeCooldown");
        }

        playercontroller.walkSpeed = 3f;
        playercontroller.jumpForce = 6f;
        playerInteractor.throwForce = 8f;

        onRageModeEnded?.Invoke();
    }

    void SetPropsPhysicsEnabled(bool physicsEnabled)
    {
        if (knockableProps == null) return;

        foreach (var rb in knockableProps)
        {
            if (rb == null) continue;
            rb.isKinematic = !physicsEnabled;
        }
    }

    /// <summary>Llamado por KnockableProp cuando un prop se cae/golpea durante Rage Mode.</summary>
    public void ReportPropKnocked(Rigidbody prop)
    {
        if (!IsActive || prop == null) return;

        if (_knockedThisRage.Add(prop))
            PropsKnockedOverThisRage++;
    }
}
