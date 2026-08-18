using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager central de limpieza. No sabe nada de los detalles de cada tipo
/// de desorden (basura, estante desordenado, producto fuera de lugar) -
/// cada uno se "auto-registra" con RegisterMess() al crearse y avisa
/// ReportMessCleaned() cuando el jugador lo soluciona.
///
/// Si un desorden se queda sin limpiar mas de 'uncleanedStressDelay'
/// segundos, le agrega estres al SanityMeter UNA vez (no se repite).
/// Tambien lleva contadores simples para el reporte de fin de turno
/// (EndOfShiftUI, cuando exista).
/// </summary>
public class CleaningSystem : MonoBehaviour
{
    public static CleaningSystem Instance { get; private set; }

    [Header("Consecuencia de dejar desorden sin limpiar")]
    [SerializeField] private float uncleanedStressDelay = 15f; // segundos antes de que estrese al jugador
    [SerializeField] private float uncleanedStressAmount = 5f;

    public int TotalMessesCreated { get; private set; } = 0;
    public int TotalMessesCleaned { get; private set; } = 0;
    public int ActiveMessCount => _activeMesses.Count;

    private class ActiveMess
    {
        public float CreatedAt;
        public bool HasAppliedStress;
    }

    private readonly Dictionary<object, ActiveMess> _activeMesses = new Dictionary<object, ActiveMess>();

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

    void Update()
    {
        if (_activeMesses.Count == 0) return;

        foreach (var entry in _activeMesses)
        {
            if (entry.Value.HasAppliedStress) continue;

            if (Time.time - entry.Value.CreatedAt >= uncleanedStressDelay)
            {
                entry.Value.HasAppliedStress = true;

                if (SanityMeter.Instance != null)
                    SanityMeter.Instance.AddStress(uncleanedStressAmount, "UncleanedMess");
            }
        }
    }

    /// <summary>
    /// Registra un desorden nuevo. 'messRef' puede ser cualquier objeto
    /// (el propio MonoBehaviour del desorden) - solo se usa como identificador.
    /// </summary>
    public void RegisterMess(object messRef)
    {
        if (messRef == null || _activeMesses.ContainsKey(messRef)) return;

        _activeMesses[messRef] = new ActiveMess { CreatedAt = Time.time, HasAppliedStress = false };
        TotalMessesCreated++;
    }

    /// <summary>Avisa que un desorden ya fue limpiado/solucionado por el jugador.</summary>
    public void ReportMessCleaned(object messRef)
    {
        if (messRef == null) return;

        if (_activeMesses.Remove(messRef))
            TotalMessesCleaned++;
    }
}