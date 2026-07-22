using UnityEngine;

/// <summary>
/// Orquesta el modo caja registradora completo:
/// - Al entrar: congela el movimiento del jugador y libera el cursor (point-and-click).
/// - Al terminar una transaccion: muestra el panel de "Seguir atendiendo" / "Terminar turno".
/// - Al terminar turno: descongela al jugador, bloquea el cursor y vuelve a tercera persona.
/// </summary>
public class RegisterModeController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RPS_ThirdPersonController playerMovement;
    [SerializeField] private RegisterCameraZone registerCameraZone;
    [SerializeField] private CashRegisterManager registerManager;
    [SerializeField] private CounterItemDragController itemDragController;
    [SerializeField] private RegisterCameraLookController cameraLookController;
    [SerializeField] private RegisterChargeTrigger chargeTrigger;

    [Header("Modelo del jugador")]
    [Tooltip("Renderers del modelo del jugador (torso, cabeza, etc). Se ocultan " +
             "en modo caja porque el cuerpo no rota junto con la camara y se " +
             "terminaria viendo el propio personaje al panear con las flechas.")]
    [SerializeField] private Renderer[] playerModelRenderers;

    [Header("UI de fin de transaccion")]
    [Tooltip("Panel con los botones 'Seguir atendiendo' y 'Terminar turno'.")]
    [SerializeField] private GameObject endOfTransactionPanel;

    void OnEnable()
    {
        if (registerCameraZone != null)
            registerCameraZone.onPlayerEnteredRegister.AddListener(HandleEnteredRegister);

        if (registerManager != null)
            registerManager.onTransactionComplete.AddListener(HandleTransactionComplete);
    }

    void OnDisable()
    {
        if (registerCameraZone != null)
            registerCameraZone.onPlayerEnteredRegister.RemoveListener(HandleEnteredRegister);

        if (registerManager != null)
            registerManager.onTransactionComplete.RemoveListener(HandleTransactionComplete);
    }

    // ===== ENTRAR AL MODO CAJA ======================================================

    void HandleEnteredRegister()
    {
        // Defensivo: garantiza que siempre arranquemos en estado limpio,
        // sin importar el historial previo del CashRegisterManager.
        if (registerManager != null)
            registerManager.ResetTransaction();

        if (cameraLookController != null)
            cameraLookController.ResetLook();

        SetPlayerModelVisible(false);
        LockPlayerMovement(true);
    }

    // ===== FIN DE TRANSACCION =========================================================

    void HandleTransactionComplete()
    {
        if (endOfTransactionPanel != null)
            endOfTransactionPanel.SetActive(true);

        // Identificamos quien estaba siendo atendido ANTES de sacarlo de la
        // fila (LeaveQueue lo remueve de la lista, asi que hay que guardar
        // la referencia primero).
        if (CustomerQueueManager.Instance != null)
        {
            Transform servedCustomer = CustomerQueueManager.Instance.CustomerAtFront;

            if (servedCustomer != null)
            {
                var lifecycle = servedCustomer.GetComponent<CustomerLifecycle>();
                lifecycle?.MarkServed();

                CustomerQueueManager.Instance.LeaveQueue(servedCustomer);
            }
        }
    }

    /// <summary>Conecta esto al boton "Seguir atendiendo".</summary>
    public void ContinueServing()
    {
        if (endOfTransactionPanel != null)
            endOfTransactionPanel.SetActive(false);

        registerManager.ResetTransaction();
        // El jugador se queda en modo caja: movimiento sigue bloqueado,
        // cursor sigue libre, camara sigue en primera persona.
    }

    /// <summary>Conecta esto al boton "Terminar turno".</summary>
    public void LeaveRegister()
    {
        if (endOfTransactionPanel != null)
            endOfTransactionPanel.SetActive(false);

        registerManager.ResetTransaction();
        registerCameraZone.ForceExitRegisterView();
        SetPlayerModelVisible(true);
        LockPlayerMovement(false);
    }

    // ===== MOVIMIENTO Y CURSOR ==========================================================

    void LockPlayerMovement(bool locked)
    {
        if (playerMovement == null) return;

        // Desactivar el componente entero congela tambien salto/sprint/particulas,
        // que es justo lo que queremos mientras el jugador atiende en la caja.
        playerMovement.enabled = !locked;

        // El arrastre point-and-click solo debe funcionar mientras estamos
        // en modo caja. Sin esto, el script corre siempre, incluso caminando
        // por la tienda con el cursor bloqueado al centro de pantalla.
        if (itemDragController != null)
            itemDragController.enabled = locked;

        // Misma logica para el giro de camara por teclado.
        if (cameraLookController != null)
            cameraLookController.enabled = locked;

        // Y para el trigger de cobro (tecla Enter).
        if (chargeTrigger != null)
            chargeTrigger.enabled = locked;

        if (locked)
            playerMovement.UnlockCursor(); // cursor libre para el point-and-click
        else
            playerMovement.LockCursor();   // cursor de vuelta a modo juego normal
    }

    // ===== MODELO DEL JUGADOR ==========================================================

    void SetPlayerModelVisible(bool visible)
    {
        if (playerModelRenderers == null) return;

        foreach (var renderer in playerModelRenderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }
}