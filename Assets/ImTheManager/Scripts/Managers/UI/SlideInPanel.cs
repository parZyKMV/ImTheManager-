using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Hace que un panel de UI se deslice desde abajo hacia su posicion normal
/// al activarse (en vez del SetActive(true) instantaneo de siempre), y
/// opcionalmente de vuelta hacia abajo al ocultarse via Hide().
///
/// Uso tipico: ponelo directo en el panel (el mismo GameObject que ya
/// prenden/apagan tus scripts con SetActive). El slide de ENTRADA funciona
/// automatico, sin tocar nada mas - se dispara solo en OnEnable(). El
/// slide de SALIDA es opcional: si querés que tambien se deslice al
/// cerrarse, cambia el SetActive(false) de tu script por Hide().
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SlideInPanel : MonoBehaviour
{
    [Header("Animación")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float offscreenOffset = 300f; // cuanto mas abajo arranca, en pixeles
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform _rect;
    private Vector2 _shownPosition; // la posicion "normal" que ya tenias configurada en el editor
    private Vector2 _hiddenPosition;
    private Coroutine _activeAnimation;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _shownPosition = _rect.anchoredPosition;
        _hiddenPosition = _shownPosition + new Vector2(0f, -offscreenOffset);
    }

    void OnEnable()
    {
        // Arranca ya abajo del todo y anima hacia la posicion normal -
        // esto pasa automatico cada vez que algo hace SetActive(true).
        _rect.anchoredPosition = _hiddenPosition;
        StartSlide(_hiddenPosition, _shownPosition, null);
    }

    void OnDisable()
    {
        if (_activeAnimation != null)
            StopCoroutine(_activeAnimation);
    }

    /// <summary>
    /// Oculta el panel deslizandolo hacia abajo, y RECIEN AHI lo desactiva.
    /// Usalo en vez de gameObject.SetActive(false) directo, si tambien
    /// queres animacion de salida (opcional).
    /// </summary>
    public void Hide()
    {
        StartSlide(_rect.anchoredPosition, _hiddenPosition, () => gameObject.SetActive(false));
    }

    void StartSlide(Vector2 from, Vector2 to, Action onComplete)
    {
        if (_activeAnimation != null)
            StopCoroutine(_activeAnimation);

        _activeAnimation = StartCoroutine(SlideRoutine(from, to, onComplete));
    }

    IEnumerator SlideRoutine(Vector2 from, Vector2 to, Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            // unscaledDeltaTime: sigue animando aunque el juego este en
            // pausa (Time.timeScale = 0), util si algun panel de estos
            // puede aparecer justo al pausar.
            elapsed += Time.unscaledDeltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            _rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        _rect.anchoredPosition = to;
        onComplete?.Invoke();
    }
}
