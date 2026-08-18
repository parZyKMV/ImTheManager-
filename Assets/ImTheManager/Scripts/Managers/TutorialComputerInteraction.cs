using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Zona de interaccion con la computadora de "entrenamiento de seguridad
/// anual". El jugador entra al trigger, ve el prompt, presiona Interact,
/// y se abre el slideshow (bloqueando movimiento y liberando el cursor
/// mientras dura, igual que otros paneles del juego).
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialComputerInteraction : MonoBehaviour
{
    [SerializeField] private RPS_ThirdPersonController playerMovement;
    [SerializeField] private TutorialSlideshowUI slideshowUI;
    [SerializeField] private GameObject promptUI; // ej. "Presiona E para ver el entrenamiento"
    [SerializeField] private string playerTag = "Player";

    private InputAction _interactAction;
    private bool _playerInRange = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[TutorialComputerInteraction] El collider deberia ser Trigger. Corrigiendo automaticamente.");
            col.isTrigger = true;
        }

        var actions = InputSystem.actions;
        _interactAction = actions?.FindAction("Interact");

        if (_interactAction == null)
            Debug.LogWarning("[TutorialComputerInteraction] No se encontro la accion 'Interact'.");
    }

    void OnEnable() => _interactAction?.Enable();
    void OnDisable() => _interactAction?.Disable();

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInRange = true;
        if (promptUI != null) promptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void Update()
    {
        if (!_playerInRange) return;
        if (_interactAction != null && _interactAction.WasPressedThisFrame())
            OpenSlideshow();
    }

    void OpenSlideshow()
    {
        if (slideshowUI == null)
        {
            Debug.LogWarning("[TutorialComputerInteraction] No hay Slideshow UI asignada.");
            return;
        }

        if (promptUI != null) promptUI.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerMovement.UnlockCursor();
        }

        slideshowUI.Open(HandleSlideshowClosed);
    }

    void HandleSlideshowClosed()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
            playerMovement.LockCursor();
        }
    }
}
