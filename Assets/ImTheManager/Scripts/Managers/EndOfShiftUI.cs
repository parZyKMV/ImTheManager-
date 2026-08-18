using UnityEngine;
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

    [Header("UI")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text moneyEarnedText;
    [SerializeField] private TMP_Text customersServedText;
    [SerializeField] private TMP_Text propsKnockedOverText;

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

    void ShowResults()
    {
        float money = ShiftStatsTracker.Instance != null ? ShiftStatsTracker.Instance.MoneyEarnedThisShift : 0f;
        int customers = ShiftStatsTracker.Instance != null ? ShiftStatsTracker.Instance.CustomersServedThisShift : 0;
        int currentDay = ProgressionData.Instance != null ? ProgressionData.Instance.CurrentDay : 1;

        if (dayText != null)
            dayText.text = $"Fin del Día {currentDay}";

        if (moneyEarnedText != null)
            moneyEarnedText.text = $"Dinero ganado: ${money:F2}";

        if (customersServedText != null)
            customersServedText.text = $"Clientes atendidos: {customers}";

        int propsKnocked = RageModeController.Instance != null ? RageModeController.Instance.TotalPropsKnockedOverThisShift : 0;

        if (propsKnockedOverText != null)
            propsKnockedOverText.text = $"Cosas derribadas: {propsKnocked}";

        // Guarda el resultado en el progreso general de la partida.
        if (ProgressionData.Instance != null)
        {
            var result = new ShiftResult
            {
                day = currentDay,
                moneyEarned = money,
                finalSanity = SanityMeter.Instance != null ? SanityMeter.Instance.CurrentStress : 0f,
                messesCreated = CleaningSystem.Instance != null ? CleaningSystem.Instance.TotalMessesCreated : 0,
                messesCleaned = CleaningSystem.Instance != null ? CleaningSystem.Instance.TotalMessesCleaned : 0,
                propsKnockedOver = propsKnocked
            };

            ProgressionData.Instance.RecordShiftResult(result);
        }

        if (panel != null)
            panel.SetActive(true);

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
        if (panel != null)
            panel.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.LockCursor();
        }

        dayCycleManager?.AdvanceToNextDay();
    }
}