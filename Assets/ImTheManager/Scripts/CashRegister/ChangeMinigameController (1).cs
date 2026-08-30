using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el panel de UI donde el jugador arma el cambio exacto
/// tocando billetes/monedas. Se abre cuando CashRegisterManager pide cambio,
/// y se cierra al confirmar.
/// </summary>
public class ChangeMinigameController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CashRegisterManager registerManager;
    [SerializeField] private GameObject panel; // el GameObject raiz de este mini-juego (se activa/desactiva)
    [SerializeField] private DenominationButton[] denominationButtons;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button clearButton; // boton para reiniciar la seleccion si el jugador se equivoca

    [Header("UI de texto")]
    [SerializeField] private TMP_Text changeOwedText;   // cuanto cambio hay que dar
    [SerializeField] private TMP_Text selectedAmountText; // cuanto lleva seleccionado el jugador

    private float _selectedAmount = 0f;

    void Awake()
    {
        foreach (var button in denominationButtons)
            button.OnClicked += HandleDenominationClicked;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmChange);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearSelection);

        if (panel != null)
            panel.SetActive(false);
    }

    void OnEnable()
    {
        if (registerManager != null)
            registerManager.onChangeRequested.AddListener(OpenMinigame);
    }

    void OnDisable()
    {
        if (registerManager != null)
            registerManager.onChangeRequested.RemoveListener(OpenMinigame);
    }

    // ===== ABRIR EL MINI-JUEGO ==================================================

    // Se llama automaticamente via el evento onChangeRequested del CashRegisterManager.
    void OpenMinigame(float changeOwed)
    {
        _selectedAmount = 0f;

        if (panel != null)
            panel.SetActive(true);

        UpdateTexts(changeOwed);
    }

    // ===== INTERACCION ===========================================================

    void HandleDenominationClicked(float value)
    {
        _selectedAmount += value;
        UpdateTexts(registerManager.ChangeOwed);
    }

    void ClearSelection()
    {
        _selectedAmount = 0f;
        UpdateTexts(registerManager.ChangeOwed);
    }

    void ConfirmChange()
    {
        registerManager.SubmitChange(_selectedAmount);

        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Cierra el panel a la fuerza, sin confirmar ningun cambio. Uso:
    /// cuando el turno termina de golpe (RegisterModeController.LeaveRegister)
    /// mientras el jugador estaba a mitad de dar el cambio - sin esto, el
    /// panel se queda pegado en pantalla para siempre.
    /// </summary>
    public void Close()
    {
        _selectedAmount = 0f;

        if (panel != null)
            panel.SetActive(false);
    }

    // ===== UI =====================================================================

    void UpdateTexts(float changeOwed)
    {
        if (changeOwedText != null)
            changeOwedText.text = $"Cambio a dar: ${changeOwed:F2}";

        if (selectedAmountText != null)
            selectedAmountText.text = $"Seleccionado: ${_selectedAmount:F2}";
    }
}