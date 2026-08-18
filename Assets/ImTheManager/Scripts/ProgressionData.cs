using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resultado guardado de un turno completo. Version simple v1 - se puede
/// extender cuando EndOfShiftUI exista de verdad (limpieza, quejas, dano de
/// Rage Mode, etc). Por ahora cubre lo minimo que ya podemos medir.
/// </summary>
[System.Serializable]
public class ShiftResult
{
    public int day;
    public float moneyEarned;
    public float finalSanity;
    public int messesCreated;
    public int messesCleaned;
    public int propsKnockedOver;
}

/// <summary>
/// Contenedor persistente de progreso a lo largo de los 10 turnos (5 dias x
/// 2 semanas). Cada turno nuevo arranca fresco en el SanityMeter, pero esto
/// recuerda el rendimiento acumulado de toda la partida.
///
/// Version v1: singleton en memoria (DontDestroyOnLoad), sin guardado en
/// disco todavia. Si mas adelante quieres persistencia real entre sesiones
/// de juego, esto es lo que habria que cambiar a JSON/PlayerPrefs.
/// </summary>
public class ProgressionData : MonoBehaviour
{
    public static ProgressionData Instance { get; private set; }

    public int CurrentDay { get; private set; } = 1;
    public float CumulativeScore { get; private set; } = 0f;
    public List<ShiftResult> History { get; private set; } = new List<ShiftResult>();

    [SerializeField] private int totalDays = 10;

    public int TotalDays => totalDays;
    public bool IsFinalDay => CurrentDay >= totalDays;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // sobrevive si el turno recarga escena
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Guarda el resultado de un turno recien terminado. Llamalo desde EndOfShiftUI.</summary>
    public void RecordShiftResult(ShiftResult result)
    {
        if (result == null) return;

        History.Add(result);
        CumulativeScore += result.moneyEarned; // scoring simple v1, ajustar cuando haya mas metricas
    }

    /// <summary>Avanza al siguiente dia. No hace nada si ya es el ultimo dia.</summary>
    public void AdvanceDay()
    {
        if (IsFinalDay) return;
        CurrentDay++;
    }

    /// <summary>Reinicia todo el progreso. Util para debug o "Nueva Partida".</summary>
    public void ResetProgression()
    {
        CurrentDay = 1;
        CumulativeScore = 0f;
        History.Clear();
    }
}
