using UnityEngine;

/// <summary>
/// Puntos fisicos sobre el mostrador donde aparecen los productos de un
/// cliente cuando llega al frente de la fila.
/// </summary>
public class CounterDropPointManager : MonoBehaviour
{
    public static CounterDropPointManager Instance { get; private set; }

    [SerializeField] private Transform[] dropPoints;

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

    public Transform GetDropPoint(int index)
    {
        if (dropPoints == null || dropPoints.Length == 0) return null;
        return dropPoints[index % dropPoints.Length];
    }
}