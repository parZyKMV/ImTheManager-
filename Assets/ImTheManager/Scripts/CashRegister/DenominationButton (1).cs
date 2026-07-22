using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Va en cada boton de billete/moneda de la UI de cambio.
/// Le asignas el valor (ej. 20, 10, 5, 1, 0.25, 0.10, 0.05, 0.01) en el Inspector.
/// </summary>
[RequireComponent(typeof(Button))]
public class DenominationButton : MonoBehaviour
{
    [SerializeField] private float value = 1f;

    public float Value => value;

    // ChangeMinigameController se suscribe a esto en vez de usar UnityEvents,
    // asi no hay que configurar el OnClick manualmente en cada boton del Inspector.
    public System.Action<float> OnClicked;

    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }

    void HandleClick()
    {
        OnClicked?.Invoke(value);
    }
}
