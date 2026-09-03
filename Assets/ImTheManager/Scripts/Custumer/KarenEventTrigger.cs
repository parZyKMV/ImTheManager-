using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Dispara un encuentro con Karen: bloquea el movimiento del jugador,
/// libera el cursor (para elegir opciones de dialogo), y arranca el nodo
/// correspondiente en el Dialogue Runner. Al terminar el dialogo (evento
/// nativo de Yarn Spinner onDialogueComplete), restaura todo automaticamente.
/// </summary>
public class KarenEventTrigger : MonoBehaviour
{
    public static KarenEventTrigger Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private RPS_ThirdPersonController playerMovement;

    [Header("Dialogo")]
    [SerializeField] private string yarnStartNode = "Karen_Encounter";

    public bool IsActive { get; private set; } = false;

    private bool _didLockPlayer = false;

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

        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
    }

    void OnEnable()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(HandleDialogueComplete);
    }

    void OnDisable()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
    }

    /// <summary>Arranca el encuentro usando el nodo por defecto configurado en el Inspector.</summary>
    public void TriggerEncounter()
    {
        TriggerEncounter(yarnStartNode);
    }

    /// <summary>Arranca el encuentro con un nodo especifico (para variantes de Karen mas adelante).</summary>
    public void TriggerEncounter(string startNode)
    {
        Debug.Log($"[KarenEventTrigger] TriggerEncounter('{startNode}') llamado. IsActive actual={IsActive}, dialogueRunner={(dialogueRunner != null ? "asignado" : "NULL")}.");

        if (IsActive)
        {
            Debug.LogWarning("[KarenEventTrigger] Ya hay un encuentro activo, se ignora el nuevo trigger.");
            return;
        }

        if (dialogueRunner == null)
        {
            Debug.LogError("[KarenEventTrigger] No hay Dialogue Runner asignado.");
            return;
        }

        if (!NodeExists(startNode))
        {
            Debug.LogError($"[KarenEventTrigger] El nodo '{startNode}' no existe en el Yarn Project asignado. Revisa que el .yarn este incluido en el proyecto y que el nombre coincida EXACTO con el 'title:' de adentro (no el nombre del archivo).");
            return;
        }

        IsActive = true;
        _didLockPlayer = true;
        LockPlayer(true);

        dialogueRunner.StartDialogue(startNode);
    }

    /// <summary>
    /// Igual que TriggerEncounter, pero SIN bloquear/desbloquear al jugador -
    /// para contextos donde el jugador ya esta congelado por otro sistema
    /// (ej. RegisterModeController mientras cobra). Usalo para banter en
    /// la caja: el cliente ya esta ahi, no hace falta tocar el lock/cursor.
    /// </summary>
    public void TriggerEncounterWithoutLocking(string startNode)
    {
        if (IsActive)
        {
            Debug.LogWarning("[KarenEventTrigger] Ya hay un encuentro activo, se ignora el nuevo trigger.");
            return;
        }

        if (dialogueRunner == null)
        {
            Debug.LogError("[KarenEventTrigger] No hay Dialogue Runner asignado.");
            return;
        }

        if (!NodeExists(startNode))
        {
            Debug.LogError($"[KarenEventTrigger] El nodo '{startNode}' no existe en el Yarn Project asignado. Revisa el nombre exacto del 'title:' dentro del .yarn.");
            return;
        }

        IsActive = true;
        _didLockPlayer = false;

        dialogueRunner.StartDialogue(startNode);
    }

    // DialogueRunner.NodeExists() ya no existe en esta version de Yarn
    // Spinner (la API async movio esa consulta al YarnProject). Chequeamos
    // directo contra YarnProject.NodeNames, que es donde vive ahora.
    bool NodeExists(string nodeName)
    {
        if (dialogueRunner.YarnProject == null)
        {
            Debug.LogError("[KarenEventTrigger] El Dialogue Runner no tiene un Yarn Project asignado.");
            return false;
        }

        foreach (var name in dialogueRunner.YarnProject.NodeNames)
        {
            if (name == nodeName)
                return true;
        }

        return false;
    }

    void HandleDialogueComplete()
    {
        IsActive = false;

        // Solo restauramos el movimiento/cursor si FUIMOS nosotros quienes
        // lo bloqueamos - si el dialogo arranco con TriggerEncounterWithoutLocking,
        // el estado del jugador lo sigue manejando quien corresponda (ej. RegisterModeController).
        if (_didLockPlayer)
            LockPlayer(false);
    }

    void LockPlayer(bool locked)
    {
        if (playerMovement == null) return;

        playerMovement.enabled = !locked;

        if (locked)
            playerMovement.UnlockCursor();
        else
            playerMovement.LockCursor();
    }
}