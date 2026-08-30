using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Orquesta el modo caja registradora completo:
/// - Al entrar: congela el movimiento del jugador y libera el cursor (point-and-click).
/// - Al terminar una transaccion: muestra el panel de "Seguir atendiendo" / "Terminar turno".
/// - Al terminar turno: descongela al jugador, bloquea el cursor y vuelve a tercera persona.
/// </summary>
public class RegisterModeController : MonoBehaviour
{
    public static RegisterModeController Instance { get; private set; }

    /// <summary>
    /// True mientras el jugador esta en modo caja (movimiento congelado,
    /// point-and-click). Cualquier sistema puede consultarlo - por ejemplo,
    /// SeekPlayerAndTalkAction lo chequea antes de ir a buscar al jugador,
    /// para no interrumpirlo ni bugear la escena mientras cobra.
    /// </summary>
    public static bool IsPlayerInRegisterMode { get; private set; } = false;

    /// <summary>
    /// True mientras el panel de "Seguir atendiendo / Terminar turno" esta
    /// en pantalla. RegisterBanterReactor lo chequea para no superponer
    /// dialogo de cliente sobre ese panel.
    /// </summary>
    public static bool IsEndOfTransactionPanelActive { get; private set; } = false;

    [Header("Referencias")]
    [SerializeField] private RPS_ThirdPersonController playerMovement;
    [SerializeField] private RegisterCameraZone registerCameraZone;
    [SerializeField] private CashRegisterManager registerManager;
    [SerializeField] private CounterItemDragController itemDragController;
    [SerializeField] private RegisterCameraLookController cameraLookController;
    [SerializeField] private RegisterChargeTrigger chargeTrigger;
    [SerializeField] private ChangeMinigameController changeMinigame;

    [Header("Modelo del jugador")]
    [Tooltip("Renderers del modelo del jugador (torso, cabeza, etc). Se ocultan " +
             "en modo caja porque el cuerpo no rota junto con la camara y se " +
             "terminaria viendo el propio personaje al panear con las flechas.")]
    [SerializeField] private Renderer[] playerModelRenderers;

    [Header("UI de fin de transaccion")]
    [Tooltip("Panel con los botones 'Seguir atendiendo' y 'Terminar turno'.")]
    [SerializeField] private GameObject endOfTransactionPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsPlayerInRegisterMode) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        TryEmergencyLeave();
    }

    // Salida de emergencia por si el jugador se queda atorado en modo caja
    // sin ningun cliente (ej. se olvido de presionar "Terminar turno" y no
    // hay nadie mas a quien atender). Solo funciona si NO hay un cliente
    // esperando/siendo atendido ahora mismo, para no romper una transaccion
    // a medias.
    void TryEmergencyLeave()
    {
        bool hasCustomerWaiting = CustomerQueueManager.Instance != null
            && CustomerQueueManager.Instance.CustomerAtFront != null;

        bool hasActiveTransaction = registerManager != null
            && registerManager.CurrentState != CashRegisterManager.RegisterState.Scanning;

        if (hasCustomerWaiting || hasActiveTransaction)
        {
            Debug.Log("[RegisterModeController] No puedes salir con ESC mientras hay un cliente siendo atendido.");
            return;
        }

        Debug.Log("[RegisterModeController] Salida de emergencia por ESC (sin clientes esperando).");
        LeaveRegister();
    }

    /// <summary>
    /// Salida FORZADA del modo caja, sin importar si hay transaccion en
    /// curso o cliente esperando - a diferencia de la salida por ESC (que
    /// respeta una transaccion activa), esta se usa SOLO cuando el turno
    /// termina de golpe (fin de dia + teletransporte) y hay que garantizar
    /// que el jugador salga del modo caja pase lo que pase, para que la
    /// camara/UI de la caja no se quede pegada mientras lo mandamos a
    /// otro lado de la tienda.
    /// </summary>
    public void ForceExitForShiftEnd()
    {
        if (!IsPlayerInRegisterMode) return;

        Debug.Log("[RegisterModeController] Salida forzada del modo caja (fin de turno).");
        LeaveRegister();
    }

    // ===== ENTRAR AL MODO CAJA ======================================================

    void HandleEnteredRegister()
    {
        IsPlayerInRegisterMode = true;

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

        IsEndOfTransactionPanelActive = true;

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

        IsEndOfTransactionPanelActive = false;

        registerManager.ResetTransaction();
        // El jugador se queda en modo caja: movimiento sigue bloqueado,
        // cursor sigue libre, camara sigue en primera persona.
    }

    /// <summary>Conecta esto al boton "Terminar turno".</summary>
    public void LeaveRegister()
    {
        IsPlayerInRegisterMode = false;
        IsEndOfTransactionPanelActive = false;

        if (endOfTransactionPanel != null)
            endOfTransactionPanel.SetActive(false);

        // Por si el jugador estaba a mitad de dar el cambio (panel de
        // billetes/monedas abierto) cuando se forzo la salida - sin esto,
        // ese panel se queda pegado en pantalla para siempre.
        if (changeMinigame != null)
            changeMinigame.Close();

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