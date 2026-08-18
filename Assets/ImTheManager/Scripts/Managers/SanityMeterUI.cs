using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI simple del SanityMeter: una barra (Slider o Image con fillAmount) mas
/// un texto opcional. Se suscribe solo a SanityMeter.onSanityChanged, no
/// necesita que nada mas la llame.
/// </summary>
public class SanityMeterUI : MonoBehaviour
{
    [Header("Referencias (deja vacio lo que no uses)")]
    [SerializeField] private Slider sanitySlider;  // opcion 1: un Slider normal
    [SerializeField] private Image fillImage;       // opcion 2: una Image con Type = Filled
    [SerializeField] private TMP_Text label;        // opcional, ej. "42/100"

    void Start()
    {
        // Start() en vez de Awake(): garantiza que SanityMeter.Awake() (que
        // asigna su Instance) ya corrio, sin importar el orden de ejecucion
        // entre distintos GameObjects de la escena.
        if (SanityMeter.Instance == null)
        {
            Debug.LogWarning("[SanityMeterUI] No hay SanityMeter en la escena.");
            return;
        }

        SanityMeter.Instance.onSanityChanged.AddListener(UpdateUI);

        // Inicializa con el valor actual, por si algo ya le agrego estres
        // antes de que esta UI se activara.
        UpdateUI(SanityMeter.Instance.CurrentStress);
    }

    void OnDestroy()
    {
        if (SanityMeter.Instance != null)
            SanityMeter.Instance.onSanityChanged.RemoveListener(UpdateUI);
    }

    void UpdateUI(float currentStress)
    {
        float normalized = SanityMeter.Instance != null ? SanityMeter.Instance.NormalizedStress : 0f;

        if (sanitySlider != null)
            sanitySlider.value = normalized;

        if (fillImage != null)
            fillImage.fillAmount = normalized;

        if (label != null)
            label.text = $"{Mathf.RoundToInt(currentStress)}/100";
    }
}
