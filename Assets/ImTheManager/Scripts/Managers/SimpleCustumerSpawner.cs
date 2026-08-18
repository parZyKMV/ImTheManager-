using UnityEngine;

public class SimpleCustumerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] custumerPrefab;
    public float tiempoDeEspera = 10.0f; // Segundos entre cada spawn

    [Header("Conexion al ciclo de turno")]
    [SerializeField] private ShiftClock shiftClock;
    [Tooltip("Deja de spawnear clientes nuevos cuando el turno llegue a este progreso (0-1), " +
             "para darle tiempo a los ultimos clientes de ser atendidos antes de que se acabe el tiempo.")]
    [Range(0f, 1f)][SerializeField] private float stopSpawningAtProgress = 0.85f;

    private float cronometro;

    void Update()
    {
        if (!CanSpawnRightNow()) return;

        cronometro += Time.deltaTime; // Suma el tiempo transcurrido por frame
        if (cronometro >= tiempoDeEspera)
        {
            SpawnearObjeto();
            cronometro = 0f; // Reinicia el temporizador
        }
    }

    // Solo spawnea mientras el turno esta activo de verdad (ya se ficho,
    // no esta en la pantalla de fin de turno) y todavia no pasamos el
    // punto de corte cerca del final del turno.
    bool CanSpawnRightNow()
    {
        if (DayCycleManager.Instance == null) return false;
        if (!DayCycleManager.Instance.HasShiftStarted) return false;
        if (DayCycleManager.Instance.CurrentPhase != DayCyclePhase.Shift) return false;

        if (shiftClock != null && shiftClock.NormalizedProgress >= stopSpawningAtProgress)
            return false;

        return true;
    }

    void SpawnearObjeto()
    {
        Instantiate(custumerPrefab[Random.Range(0, custumerPrefab.Length)], transform.position, transform.rotation);
    }
}