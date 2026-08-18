using UnityEngine;

/// <summary>
/// Punto en la bodega que mantiene siempre una caja disponible para recoger.
/// Cuando la caja actual desaparece (se vacio) o el jugador se la lleva,
/// spawnea una nueva despues de un tiempo.
/// </summary>
public class WarehouseStockSpawner : MonoBehaviour
{
    [SerializeField] private GameObject boxPrefab; // prefab con Pickupable + StockBox
    [SerializeField] private float respawnDelay = 3f;

    private GameObject _currentBox;
    private float _respawnTimer;
    private bool _waitingToRespawn;

    void Start()
    {
        SpawnBox();
    }

    void Update()
    {
        // La caja "se fue" si se destruyo (se vacio) o si el jugador la
        // recogio (Pickupable la parentea al holdPoint, asi que ya tiene padre).
        bool boxIsGone = _currentBox == null || _currentBox.transform.parent != null;

        if (boxIsGone && !_waitingToRespawn)
        {
            _waitingToRespawn = true;
            _respawnTimer = respawnDelay;
            _currentBox = null;
        }

        if (_waitingToRespawn)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                SpawnBox();
                _waitingToRespawn = false;
            }
        }
    }

    void SpawnBox()
    {
        if (boxPrefab == null)
        {
            Debug.LogWarning("[WarehouseStockSpawner] No hay Box Prefab asignado.");
            return;
        }

        _currentBox = Instantiate(boxPrefab, transform.position, transform.rotation);
    }
}
