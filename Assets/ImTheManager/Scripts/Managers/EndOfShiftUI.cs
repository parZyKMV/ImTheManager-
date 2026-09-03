using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Pantalla de resultados de fin de turno: dinero ganado y clientes
/// atendidos (limpieza/quejas/Rage Mode se pueden sumar despues, cuando
/// haga falta mostrarlos). Guarda un ShiftResult en ProgressionData y
/// da el boton para continuar al siguiente dia.
///
/// IMPORTANTE: este script debe vivir en un GameObject que este SIEMPRE
/// activo (no en el panel que se prende/apaga) - si lo pones en el panel
/// mismo, su OnEnable/Start nunca corre mientras el panel arranca
/// desactivado, y se pierde la suscripcion al evento (mismo bug que ya
/// nos paso una vez con ChangeMinigameController).
/// </summary>
public class EndOfShiftUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField] private RPS_ThirdPersonController playerMovement;
    [SerializeField] private GameObject panel; // el panel visual, este SI puede estar desactivado por defecto
    [SerializeField] private Transform dayStartPoint; // donde aparece el jugador al empezar cada dia nuevo
    [SerializeField] private GameObject HDU;

    [Header("UI")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text moneyEarnedText;
    [SerializeField] private TMP_Text customersServedText;
    [SerializeField] private TMP_Text propsKnockedOverText;

    private bool _isPanelActive = false;

    void Start()
    {
        if (dayCycleManager != null)
            dayCycleManager.onShiftEnded.AddListener(ShowResults);

        if (panel != null)
            panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (dayCycleManager != null)
            dayCycleManager.onShiftEnded.RemoveListener(ShowResults);
    }

    void Update()
    {
        // Atajo de teclado: el mouse a veces no responde en esta pantalla
        // (bug conocido de foco/cursor) - Enter hace lo mismo que
        // clickear "Continuar", como respaldo.
        if (_isPanelActive && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            ContinueToNextDay();
    }

    void ShowResults()
    {
        // Si el jugador estaba en modo caja cuando se acabo el turno, hay
        // que sacarlo de ahi a la fuerza - si no, la camara/UI de la caja
        // se queda pegada mientras lo teletransportamos a Day Start Point.
        RegisterModeController.Instance?.ForceExitForShiftEnd();

        HDU.SetActive(false);

        float money = ShiftStatsTracker.Instance != null ? ShiftStatsTracker.Instance.MoneyEarnedThisShift : 0f;
        int customers = ShiftStatsTracker.Instance != null ? ShiftStatsTracker.Instance.CustomersServedThisShift : 0;
        int currentDay = ProgressionData.Instance != null ? ProgressionData.Instance.CurrentDay : 1;

        if (dayText != null)
            dayText.text = $"End of Day {currentDay}";

        if (moneyEarnedText != null)
            moneyEarnedText.text = $"Money earned: ${money:F2}";

        if (customersServedText != null)
            customersServedText.text = $"Customers served: {customers}";

        int propsKnocked = RageModeController.Instance != null ? RageModeController.Instance.TotalPropsKnockedOverThisShift : 0;

        if (propsKnockedOverText != null)
            propsKnockedOverText.text = $"Things knocked over: {propsKnocked}";

        // Guarda el resultado en el progreso general de la partida.
        if (ProgressionData.Instance != null)
        {
            var result = new ShiftResult
            {
                day = currentDay,
                moneyEarned = money,
                finalSanity = SanityMeter.Instance != null ? SanityMeter.Instance.CurrentStress : 0f,
                stressEvents = SanityMeter.Instance != null ? SanityMeter.Instance.StressEventCountThisShift : 0,
                customersServed = customers,
                messesCreated = CleaningSystem.Instance != null ? CleaningSystem.Instance.TotalMessesCreated : 0,
                messesCleaned = CleaningSystem.Instance != null ? CleaningSystem.Instance.TotalMessesCleaned : 0,
                propsKnockedOver = propsKnocked
            };

            ProgressionData.Instance.RecordShiftResult(result);
        }

        if (panel != null)
            panel.SetActive(true);

        _isPanelActive = true;

        // Sin esto, si el jugador salio de la caja justo antes de que
        // terminara el turno, el cursor queda bloqueado (RegisterModeController
        // lo vuelve a bloquear al salir) y no se puede clickear "Continuar".
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerMovement.UnlockCursor();
        }
    }

    /// <summary>Conecta esto al botón "Continuar al Día X" en el OnClick del panel.</summary>
    public void ContinueToNextDay()
    {
        if (!_isPanelActive) return; // evita doble ejecucion si el click y Enter caen el mismo frame
        _isPanelActive = false;

        if (panel != null)
            panel.SetActive(false);

        HDU.SetActive(true);

        // Limpia el "piso de la tienda" para el dia nuevo: los clientes de
        // ayer desaparecen (no tiene sentido que sigan parados ahi), pero
        // los desordenes (basura, estantes desordenados, productos fuera
        // de lugar) NO se tocan - si no los limpiaste, siguen ahi manana.
        CustomerQueueManager.Instance?.DespawnAllCustomers();

        // Manda al jugador de vuelta al punto de inicio del dia (ej. cerca
        // de la entrada/reloj de fichar), en vez de dejarlo donde haya
        // quedado parado (podria estar en medio de la caja, o quien sabe
        // donde si Rage Mode lo tiro por ahi).
        if (playerMovement != null && dayStartPoint != null)
        {
            var characterController = playerMovement.GetComponent<CharacterController>();

            // Hay que apagar el CharacterController antes de teletransportar -
            // si esta activo, pelea contra el cambio directo de posicion.
            if (characterController != null) characterController.enabled = false;

            playerMovement.transform.SetPositionAndRotation(dayStartPoint.position, dayStartPoint.rotation);

            if (characterController != null) characterController.enabled = true;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.LockCursor();
        }

        dayCycleManager?.AdvanceToNextDay();
    }
}