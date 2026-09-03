using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Estacion de "clock in": el jugador tiene que fichar aca para arrancar
/// el turno, en vez de que arranque solo. Requiere que DayCycleManager
/// tenga 'Auto Start First Day' desactivado (ver DayCycleManager.cs).
///
/// Ahora tambien exige que la tienda este limpia (sin desordenes activos
/// en CleaningSystem) antes de poder fichar - deja el desorden de un dia
/// sin resolver y no vas a poder arrancar el siguiente hasta limpiarlo.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClockInStation : MonoBehaviour
{
    [SerializeField] private GameObject promptUI; // el objeto que se prende/apaga
    [SerializeField] private TMP_Text promptText;  // opcional: texto dentro del prompt, cambia segun si se puede fichar o no
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Cuantos desordenes activos se toleran antes de fichar. No exigimos 0 exacto - " +
             "si algo se rompe (ej. un producto lanzado se pierde/desaparece por un bug) y queda " +
             "contando para siempre, esto evita que el jugador quede bloqueado sin poder avanzar.")]
    [SerializeField] private int maxTolerableMesses = 5;

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
        UpdatePromptText();
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

        // El texto se actualiza en vivo por si limpias justo mientras
        // estas parado en el trigger - asi ves el cambio sin tener que
        // salir y volver a entrar.
        UpdatePromptText();

        if (_interactAction != null && _interactAction.WasPressedThisFrame())
            ClockIn();
    }

    bool CanClockIn(out int activeMesses)
    {
        activeMesses = CleaningSystem.Instance != null ? CleaningSystem.Instance.ActiveMessCount : 0;
        return activeMesses <= maxTolerableMesses;
    }

    void UpdatePromptText()
    {
        if (promptText == null) return;

        bool canClockIn = CanClockIn(out int activeMesses);

        promptText.text = canClockIn
            ? "Press E to clock in"
            : $"Clean the store first ({activeMesses}/{maxTolerableMesses} messes tolerated)";
    }

    void ClockIn()
    {
        if (DayCycleManager.Instance == null || DayCycleManager.Instance.HasShiftStarted) return;

        if (!CanClockIn(out int activeMesses))
        {
            Debug.Log($"[ClockInStation] No puedes fichar todavia - quedan {activeMesses} desorden(es) sin limpiar.");
            return;
        }

        int day = ProgressionData.Instance != null ? ProgressionData.Instance.CurrentDay : 1;
        DayCycleManager.Instance.StartDay(day);

        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }
}