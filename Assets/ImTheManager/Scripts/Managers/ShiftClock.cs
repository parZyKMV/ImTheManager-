using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Convierte tiempo real transcurrido en un reloj de juego de 8 horas
/// (~12 minutos reales por turno, configurable). Expone la hora actual
/// para el HUD y para programar eventos ("el camion llega a las 2 PM").
/// No sabe nada de dias/turnos en si - eso lo maneja DayCycleManager.
/// </summary>
public class ShiftClock : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Cuantos segundos reales dura un turno completo (default: 12 min = 720s).")]
    [SerializeField] private float realSecondsPerShift = 720f;
    [Tooltip("Hora de juego (24hs) en la que arranca el turno. Ej: 8 = 8:00 AM.")]
    [SerializeField] private float shiftStartInGameHour = 8f;
    [Tooltip("Cuantas horas de juego representa un turno completo.")]
    [SerializeField] private float shiftLengthInGameHours = 8f;

    [Header("Eventos")]
    public FloatEvent onTimeUpdated; // progreso normalizado 0-1, cada frame que corre
    public UnityEvent onShiftEnded;  // se dispara UNA vez cuando se acaba el tiempo

    public float ElapsedRealSeconds { get; private set; } = 0f;
    public bool IsRunning { get; private set; } = false;

    private bool _hasEnded = false;

    public float NormalizedProgress => Mathf.Clamp01(ElapsedRealSeconds / realSecondsPerShift);
    public float CurrentInGameHour => shiftStartInGameHour + (NormalizedProgress * shiftLengthInGameHours);

    /// <summary>Hora actual formateada tipo "2:30 PM", para el HUD.</summary>
    public string CurrentInGameTimeString
    {
        get
        {
            float hour24 = CurrentInGameHour % 24f;
            int hourInt = Mathf.FloorToInt(hour24);
            int minuteInt = Mathf.FloorToInt((hour24 - hourInt) * 60f);

            string period = hourInt >= 12 ? "PM" : "AM";
            int hour12 = hourInt % 12;
            if (hour12 == 0) hour12 = 12;

            return $"{hour12}:{minuteInt:00} {period}";
        }
    }

    void Update()
    {
        if (!IsRunning || _hasEnded) return;

        ElapsedRealSeconds += Time.deltaTime;
        onTimeUpdated?.Invoke(NormalizedProgress);

        if (ElapsedRealSeconds >= realSecondsPerShift)
        {
            _hasEnded = true;
            IsRunning = false;
            onShiftEnded?.Invoke();
        }
    }

    /// <summary>Arranca el reloj desde cero. Llamalo al empezar un turno nuevo.</summary>
    public void StartClock()
    {
        ElapsedRealSeconds = 0f;
        _hasEnded = false;
        IsRunning = true;
    }

    /// <summary>Pausa el reloj sin resetear el progreso (ej. durante un dialogo de Karen).</summary>
    public void PauseClock() => IsRunning = false;

    /// <summary>Reanuda el reloj pausado. No hace nada si el turno ya termino.</summary>
    public void ResumeClock()
    {
        if (!_hasEnded)
            IsRunning = true;
    }

    public void StopClock() => IsRunning = false;
}
