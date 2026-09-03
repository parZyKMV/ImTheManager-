using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Colores")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color hoverColor = new Color(1f, 0.8f, 0f);
    [SerializeField] Color pressedColor = new Color(1f, 0.4f, 0f);

    [Header("Escala")]
    [SerializeField] float hoverScale = 1.15f;
    [SerializeField] float pressedScale = 0.95f;
    [SerializeField] float scaleSpeed = 8f;

    [Header("Otros botones")]
    [SerializeField] MenuButton[] otherButtons;
    [SerializeField] float dimAlpha = 0.4f;
    [SerializeField] float dimSpeed = 5f;

    TextMeshProUGUI text;
    Image image;
    Vector3 targetScale;
    Color targetColor;
    float targetAlpha = 1f;
    CanvasGroup canvasGroup;

    void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        targetScale = Vector3.one;
        targetColor = normalColor;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        if (text != null)
            text.color = Color.Lerp(text.color, targetColor, Time.deltaTime * scaleSpeed);
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * dimSpeed);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        targetScale = Vector3.one * hoverScale;
        targetColor = hoverColor;

        // dim otros botones
        foreach (var btn in otherButtons)
            btn.SetDimmed(true);
    }

    public void OnPointerExit(PointerEventData e)
    {
        targetScale = Vector3.one;
        targetColor = normalColor;

        foreach (var btn in otherButtons)
            btn.SetDimmed(false);
    }

    public void OnPointerDown(PointerEventData e)
    {
        targetScale = Vector3.one * pressedScale;
        targetColor = pressedColor;
    }

    public void OnPointerUp(PointerEventData e)
    {
        targetScale = Vector3.one * hoverScale;
        targetColor = hoverColor;
    }

    public void SetDimmed(bool dimmed)
    {
        targetAlpha = dimmed ? dimAlpha : 1f;
    }
}