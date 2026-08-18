using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Estacion de "clock in": el jugador tiene que fichar aca para arrancar
/// el turno, en vez de que arranque solo. Requiere que DayCycleManager
/// tenga 'Auto Start First Day' desactivado (ver DayCycleManager.cs).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClockInStation : MonoBehaviour
{
    [SerializeField] private GameObject promptUI; // ej. "Presiona E para fichar"
    [SerializeField] private string playerTag = "Player";

    private InputAction _interactAction;
    private bool _playerInRange = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[ClockInStation] El collider deberia ser Trigger. Corrigiendo automaticamente.");
            col.isTrigger = true;
        }

        var actions = InputSystem.actions;
        _interactAction = actions?.FindAction("Interact");
    }

    void OnEnable() => _interactAction?.Enable();
    void OnDisable() => _interactAction?.Disable();

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (DayCycleManager.Instance == null || DayCycleManager.Instance.HasShiftStarted) return;

        _playerInRange = true;
        if (promptUI != null) promptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        if (!_playerInRange) return;
        if (_interactAction != null && _interactAction.WasPressedThisFrame())
            ClockIn();
    }

    void ClockIn()
    {
        if (DayCycleManager.Instance == null || DayCycleManager.Instance.HasShiftStarted) return;

        int day = ProgressionData.Instance != null ? ProgressionData.Instance.CurrentDay : 1;
        DayCycleManager.Instance.StartDay(day);

        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }
}
