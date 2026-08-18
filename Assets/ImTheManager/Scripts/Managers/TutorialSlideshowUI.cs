using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TutorialSlide
{
    public string title;
    [TextArea] public string description;
    public Sprite image; // opcional, ej. un icono del control que explica
}

/// <summary>
/// Panel de diapositivas del "entrenamiento de seguridad anual" - el
/// tutorial de controles del juego, dentro del universo (video corporativo
/// obligatorio). Siguiente/Anterior/Cerrar, nada mas.
/// </summary>
public class TutorialSlideshowUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image slideImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button closeButton;

    [Header("Contenido")]
    [SerializeField] private TutorialSlide[] slides;

    private int _currentIndex = 0;
    private System.Action _onClosed;

    void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(NextSlide);
        if (previousButton != null) previousButton.onClick.AddListener(PreviousSlide);
        if (closeButton != null) closeButton.onClick.AddListener(Close);

        if (panel != null) panel.SetActive(false);
    }

    /// <summary>Abre el slideshow desde el principio. onClosed se llama al cerrarlo.</summary>
    public void Open(System.Action onClosed)
    {
        _onClosed = onClosed;
        _currentIndex = 0;

        if (panel != null) panel.SetActive(true);
        ShowCurrentSlide();
    }

    void ShowCurrentSlide()
    {
        if (slides == null || slides.Length == 0) return;

        var slide = slides[_currentIndex];

        if (titleText != null) titleText.text = slide.title;
        if (descriptionText != null) descriptionText.text = slide.description;

        if (slideImage != null)
        {
            slideImage.sprite = slide.image;
            slideImage.gameObject.SetActive(slide.image != null);
        }

        if (previousButton != null) previousButton.interactable = _currentIndex > 0;
        if (nextButton != null) nextButton.interactable = _currentIndex < slides.Length - 1;
    }

    void NextSlide()
    {
        if (slides == null || _currentIndex >= slides.Length - 1) return;
        _currentIndex++;
        ShowCurrentSlide();
    }

    void PreviousSlide()
    {
        if (_currentIndex <= 0) return;
        _currentIndex--;
        ShowCurrentSlide();
    }

    void Close()
    {
        if (panel != null) panel.SetActive(false);

        var callback = _onClosed;
        _onClosed = null;
        callback?.Invoke();
    }
}
