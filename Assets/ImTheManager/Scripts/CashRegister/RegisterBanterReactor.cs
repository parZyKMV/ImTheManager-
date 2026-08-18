using UnityEngine;

/// <summary>
/// Comentarios del cliente MIENTRAS esta siendo atendido en la caja - no
/// necesita caminar (ya esta ahi) y NO toca el lock del jugador (ya lo
/// maneja RegisterModeController mientras cobra). Dos disparadores
/// independientes:
/// - Reaccion negativa si el jugador da el cambio incorrecto.
/// - Un chiste random sin relacion con el escaneo, al llegar al frente de la fila.
/// </summary>
[RequireComponent(typeof(CustomerLifecycle))]
public class RegisterBanterReactor : MonoBehaviour
{
    [Header("Reacción al cambio incorrecto")]
    [SerializeField] private string wrongChangeDialogueNode = "WrongChange_Reaction";

    [Header("Chiste random al llegar a la caja")]
    [Range(0f, 1f)][SerializeField] private float jokeChance = 0.3f;
    [SerializeField] private string jokeDialogueNode = "ScanJoke_Encounter";

    private bool _hasTriedJoke = false;
    private bool _hasPendingWrongChangeReaction = false;

    private CustomerLifecycle _lifecycle;

    void Awake()
    {
        _lifecycle = GetComponent<CustomerLifecycle>();
    }

    void Start()
    {
        if (CashRegisterManager.Instance != null)
            CashRegisterManager.Instance.onChangeResult.AddListener(HandleChangeResult);
    }

    void OnDestroy()
    {
        if (CashRegisterManager.Instance != null)
            CashRegisterManager.Instance.onChangeResult.RemoveListener(HandleChangeResult);
    }

    void Update()
    {
        // El panel de fin de transaccion tiene prioridad absoluta - nunca
        // superponemos dialogo de cliente mientras esta en pantalla.
        if (RegisterModeController.IsEndOfTransactionPanelActive)
        {
            // Si justo se nos venia la reaccion al cambio, la descartamos
            // en vez de dejarla esperando (el cliente ya se esta yendo).
            _hasPendingWrongChangeReaction = false;
            return;
        }

        if (_hasPendingWrongChangeReaction)
        {
            _hasPendingWrongChangeReaction = false;
            TryReactToWrongChange();
            return; // no tira el chiste el mismo frame que la reaccion al cambio
        }

        // Tira el chiste una sola vez, pero solo cuando el cliente YA
        // llego fisicamente al mostrador Y el jugador tambien esta ahi
        // (en modo caja) - si el cliente llega primero, esperamos a que
        // el jugador entre antes de disparar el dialogo.
        if (_hasTriedJoke) return;
        if (_lifecycle == null || !_lifecycle.HasPlacedProducts) return;
        if (!RegisterModeController.IsPlayerInRegisterMode) return;

        _hasTriedJoke = true;
        TryTellJoke();
    }

    void TryTellJoke()
    {
        if (Random.value > jokeChance) return;
        if (KarenEventTrigger.Instance == null || KarenEventTrigger.Instance.IsActive) return;

        KarenEventTrigger.Instance.TriggerEncounterWithoutLocking(jokeDialogueNode);
    }

    void HandleChangeResult(bool wasCorrect)
    {
        if (wasCorrect) return;

        // CashRegisterManager es singleton: este evento se dispara para
        // CUALQUIER transaccion, solo reaccionamos si somos el cliente
        // que esta siendo atendido ahora mismo.
        if (CustomerQueueManager.Instance == null) return;
        if (CustomerQueueManager.Instance.CustomerAtFront != transform) return;

        // No disparamos el dialogo aca mismo: este evento se dispara ANTES
        // de que RegisterModeController muestre el panel de fin de
        // transaccion (mismo frame). Lo marcamos como pendiente y lo
        // evaluamos en el siguiente Update(), cuando ya sabemos si el
        // panel esta activo o no.
        _hasPendingWrongChangeReaction = true;
    }

    void TryReactToWrongChange()
    {
        if (KarenEventTrigger.Instance == null || KarenEventTrigger.Instance.IsActive) return;

        KarenEventTrigger.Instance.TriggerEncounterWithoutLocking(wrongChangeDialogueNode);
    }
}