using UnityEngine;

/// <summary>
/// Lee el dia actual de ProgressionData y expone valores de escalado
/// (velocidad de spawn de clientes, "dificultad" general) para que otros
/// sistemas los consuman. Version v1: curva simple/lineal, suficiente para
/// la vertical slice - ajustar despues si hace falta mas escalada real.
/// </summary>
public class DifficultyCurve : MonoBehaviour
{
    public static DifficultyCurve Instance { get; private set; }

    [Header("Spawn de clientes")]
    [Tooltip("Segundos entre clientes en el dia 1.")]
    [SerializeField] private float baseSpawnInterval = 10f;
    [Tooltip("Segundos entre clientes en el ultimo dia (mas bajo = clientes mas seguido).")]
    [SerializeField] private float finalDaySpawnInterval = 4f;

    [Header("Complicaciones")]
    [Tooltip("Multiplicador de complaintChance/messChance en el dia 1.")]
    [SerializeField] private float baseDifficultyMultiplier = 1f;
    [Tooltip("Multiplicador de complaintChance/messChance en el ultimo dia.")]
    [SerializeField] private float finalDifficultyMultiplier = 2f;

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

    // Progreso 0-1 a lo largo de los 10 dias (dia 1 = 0, dia 10 = 1).
    float NormalizedDayProgress
    {
        get
        {
            if (ProgressionData.Instance == null) return 0f;

            int totalDays = Mathf.Max(2, ProgressionData.Instance.TotalDays); // evita division por 0 si totalDays=1
            return Mathf.Clamp01((ProgressionData.Instance.CurrentDay - 1) / (float)(totalDays - 1));
        }
    }

    /// <summary>Cuantos segundos deberia esperar el spawner entre clientes, segun el dia actual.</summary>
    public float GetCustomerSpawnInterval()
    {
        return Mathf.Lerp(baseSpawnInterval, finalDaySpawnInterval, NormalizedDayProgress);
    }

    /// <summary>Multiplicador general de "dificultad" (quejas, desorden, etc.) segun el dia actual.</summary>
    public float GetDifficultyMultiplier()
    {
        return Mathf.Lerp(baseDifficultyMultiplier, finalDifficultyMultiplier, NormalizedDayProgress);
    }
}
