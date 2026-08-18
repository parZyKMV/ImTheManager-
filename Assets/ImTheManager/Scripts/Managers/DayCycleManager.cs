using UnityEngine;
using UnityEngine.Events;

public enum DayCyclePhase { Shift, EndOfShift, Complete }

/// <summary>
/// State machine de nivel superior para la estructura de 10 turnos.
/// Orquesta ShiftClock (el reloj de cada turno) y ProgressionData (lo que
/// se recuerda entre turnos). No sabe nada de UI - solo dispara eventos
/// para que HUDController/EndOfShiftUI/FinalDayController reaccionen.
/// </summary>
public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private ShiftClock shiftClock;

    [Header("Configuración")]
    [Tooltip("Si esta activado, el primer turno arranca solo al cargar la escena. Desactivalo si usas ClockInStation para que el jugador tenga que fichar primero.")]
    [SerializeField] private bool autoStartFirstDay = true;

    [Header("Eventos")]
    public IntEvent onDayStarted;      // dia actual (1-10)
    public UnityEvent onShiftEnded;    // se acabo el tiempo del turno, mostrar pantalla de resultados
    public UnityEvent onFinalDayReached; // se completo el dia 10 -> FinalDayController

    public DayCyclePhase CurrentPhase { get; private set; } = DayCyclePhase.Shift;
    public bool HasShiftStarted { get; private set; } = false;

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

        if (shiftClock != null)
            shiftClock.onShiftEnded.RemoveListener(HandleShiftClockEnded);
    }

    void Start()
    {
        if (shiftClock != null)
            shiftClock.onShiftEnded.AddListener(HandleShiftClockEnded);

        if (autoStartFirstDay)
        {
            int startingDay = ProgressionData.Instance != null ? ProgressionData.Instance.CurrentDay : 1;
            StartDay(startingDay);
        }
        // Si autoStartFirstDay esta desactivado, el turno queda esperando
        // a que algo externo llame StartDay() - ej. ClockInStation cuando
        // el jugador ficha.
    }

    /// <summary>Arranca un turno: resetea el SanityMeter (fresco cada dia) y prende el reloj.</summary>
    public void StartDay(int dayNumber)
    {
        CurrentPhase = DayCyclePhase.Shift;
        HasShiftStarted = true;

        // Cada turno nuevo arranca fresco en el estres, aunque el progreso
        // general (ProgressionData) recuerda el rendimiento acumulado.
        SanityMeter.Instance?.ResetMeter();

        shiftClock?.StartClock();

        onDayStarted?.Invoke(dayNumber);
    }

    void HandleShiftClockEnded()
    {
        EndDay();
    }

    /// <summary>Termina el turno actual manualmente (o lo dispara el reloj al acabarse el tiempo).</summary>
    public void EndDay()
    {
        CurrentPhase = DayCyclePhase.EndOfShift;
        shiftClock?.StopClock();

        onShiftEnded?.Invoke();
    }

    /// <summary>Llamalo desde el boton "Continuar al Dia X" de EndOfShiftUI.</summary>
    public void AdvanceToNextDay()
    {
        if (ProgressionData.Instance == null)
        {
            Debug.LogError("[DayCycleManager] No hay ProgressionData en la escena.");
            return;
        }

        if (ProgressionData.Instance.IsFinalDay)
        {
            CurrentPhase = DayCyclePhase.Complete;
            onFinalDayReached?.Invoke();
            return;
        }

        ProgressionData.Instance.AdvanceDay();
        StartDay(ProgressionData.Instance.CurrentDay);
    }
}