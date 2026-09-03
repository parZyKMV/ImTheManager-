using TMPro;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Logica especial SOLO para el cierre del dia 10: dispara el evento
/// narrativo final. Version v1: un evento fijo (dialogo opcional de Yarn +
/// pantalla de fin de juego), no un sistema de multiples finales -
/// ampliar despues si hay tiempo, tal como recomienda el doc de diseno.
/// </summary>
public class FinalDayController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DayCycleManager dayCycleManager;
    [SerializeField] private DialogueRunner dialogueRunner; // opcional, dejalo vacio si no usas dialogo de cierre
    [SerializeField] private string finalYarnNode; // ej. "Ending_Node" - vacio = salta directo al panel

    [Header("UI")]
    [SerializeField] private GameObject endGamePanel; // pantalla final ("Sobreviviste tus ultimas 2 semanas...")
    [SerializeField] private TMP_Text summaryText; // resumen con los totales y el pago final

    [Header("Fórmula de pago final")]
    [Tooltip("Bono extra por cada cliente atendido en TODA la partida.")]
    [SerializeField] private float bonusPerCustomerServed = 0.5f;
    [Tooltip("Se descuenta por cada VEZ que te estresaste (no por la magnitud, por la cantidad de incidentes).")]
    [SerializeField] private float penaltyPerStressEvent = 1f;
    [Tooltip("Se descuenta por cada desorden que quedo SIN limpiar al final (messesCreated - messesCleaned, sumado de todos los dias).")]
    [SerializeField] private float penaltyPerUnclearnedMess = 2f;

    void Start()
    {
        if (dayCycleManager != null)
            dayCycleManager.onFinalDayReached.AddListener(TriggerEnding);

        if (endGamePanel != null)
            endGamePanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (dayCycleManager != null)
            dayCycleManager.onFinalDayReached.RemoveListener(TriggerEnding);
    }

    void TriggerEnding()
    {
        Debug.Log("[FinalDayController] Dia 10 completado - disparando el cierre.");

        bool hasValidDialogue = !string.IsNullOrEmpty(finalYarnNode)
            && dialogueRunner != null
            && NodeExists(finalYarnNode);

        if (hasValidDialogue)
        {
            dialogueRunner.onDialogueComplete.AddListener(ShowEndGamePanel);
            dialogueRunner.StartDialogue(finalYarnNode);
        }
        else
        {
            ShowEndGamePanel();
        }
    }

    // DialogueRunner.NodeExists() ya no existe en esta version de Yarn
    // Spinner (la API async movio esa consulta al YarnProject). Chequeamos
    // directo contra YarnProject.NodeNames, que es donde vive ahora.
    bool NodeExists(string nodeName)
    {
        if (dialogueRunner == null || dialogueRunner.YarnProject == null)
            return false;

        foreach (var name in dialogueRunner.YarnProject.NodeNames)
        {
            if (name == nodeName)
                return true;
        }

        return false;
    }

    void ShowEndGamePanel()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(ShowEndGamePanel);

        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        if (summaryText != null)
            summaryText.text = BuildSummaryText();

        // Cursor libre para poder clickear los botones del panel final.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Suma el desempeno de TODOS los dias guardados en ProgressionData.History
    // y calcula el pago final: dinero base + bono por clientes - penalizacion
    // por estres - penalizacion por desorden sin limpiar.
    string BuildSummaryText()
    {
        if (ProgressionData.Instance == null) return "Sin datos de progreso.";

        var totals = ProgressionData.Instance.GetAggregatedTotals();
        int uncleanedMesses = Mathf.Max(0, totals.totalMessesCreated - totals.totalMessesCleaned);

        float customerBonus = totals.totalCustomers * bonusPerCustomerServed;
        float stressPenalty = totals.totalStressEvents * penaltyPerStressEvent;
        float messPenalty = uncleanedMesses * penaltyPerUnclearnedMess;

        float finalPay = totals.totalMoney + customerBonus - stressPenalty - messPenalty;
        finalPay = Mathf.Max(0f, finalPay); // el pago nunca es negativo

        return
            $"Total sales: ${totals.totalMoney:F2}\n" +
            $"Customers served: {totals.totalCustomers} (+${customerBonus:F2})\n" +
            $"Times you got stressed: {totals.totalStressEvents} (-${stressPenalty:F2})\n" +
            $"Unclean messes: {uncleanedMesses} (-${messPenalty:F2})\n" +
            $"Props knocked over in Rage Mode: {totals.totalPropsKnocked}\n" +
            $"\nFINAL PAY: ${finalPay:F2}";
    }

    /// <summary>Conecta esto al boton "Volver al menu" del panel final.</summary>
    public void ReturnToMainMenu()
    {
        if (ProgressionData.Instance != null)
            ProgressionData.Instance.ResetProgression();

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); // ajusta al nombre real de tu escena de menu
    }
}