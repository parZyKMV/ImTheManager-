using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Se coloca en el Player (junto a RPS_ThirdPersonController).
/// Detecta objetos Pickupable frente al jugador, muestra un prompt de UI,
/// y maneja recoger/soltar con la accion de Input "Interact".
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Deteccion")]
    [SerializeField] private Transform interactionOrigin; // normalmente la camara; si lo dejas vacio usa Camera.main
    [SerializeField] private float interactionRange = 2.5f;
    [SerializeField] private float interactionRadius = 0.4f; // grosor del SphereCast, da margen para "apuntar"
    [SerializeField] private LayerMask pickupableLayer;       // crea una Layer "Pickupable" y asignala aqui

    [Header("Cargar objetos")]
    [SerializeField] private Transform holdPoint; // punto (hijo de la camara) donde se sostiene la caja

    [Header("Lanzar / patear")]
    [SerializeField] private float throwForce = 8f;       // intensidad del lanzamiento
    [SerializeField] private float throwUpwardBoost = 0.15f; // le da un poco de arco hacia arriba, no un tiro plano

    [Header("UI")]
    [SerializeField] private GameObject promptUI; // objeto con el texto "Recoger [E]", se activa/desactiva solo

    private InputAction _interactAction;
    private InputAction _throwAction;
    private Pickupable _lookedAtPickupable; // lo que estamos mirando ahora (aun no recogido)
    private Pickupable _heldPickupable;     // lo que estamos cargando actualmente

    // ===== AWAKE =============================================================

    void Awake()
    {
        var actions = InputSystem.actions;

        if (actions == null)
        {
            Debug.LogError("[PlayerInteractor] No se encontro un Input Actions asset asignado como Project-wide Actions.");
            return;
        }

        _interactAction = actions.FindAction("Interact");
        _throwAction = actions.FindAction("Throw");

        if (_interactAction == null)
            Debug.LogWarning("[PlayerInteractor] No se encontro la accion 'Interact'. Agregala en tu Input Actions asset (boton, ej. tecla E).");

        if (_throwAction == null)
            Debug.LogWarning("[PlayerInteractor] No se encontro la accion 'Throw'. Agregala en tu Input Actions asset (boton, ej. click derecho o tecla G).");

        if (interactionOrigin == null && Camera.main != null)
            interactionOrigin = Camera.main.transform;
    }

    void OnEnable()
    {
        _interactAction?.Enable();
        _throwAction?.Enable();
    }

    void OnDisable()
    {
        _interactAction?.Disable();
        _throwAction?.Disable();
    }

    // ===== UPDATE =============================================================

    void Update()
    {
        // Solo buscamos algo nuevo que recoger si las manos estan libres.
        if (_heldPickupable == null)
            DetectPickupable();

        if (_interactAction != null && _interactAction.WasPressedThisFrame())
            HandleInteractPressed();

        if (_throwAction != null && _throwAction.WasPressedThisFrame())
            HandleThrowPressed();
    }

    // ===== DETECCION ===========================================================

    void DetectPickupable()
    {
        Pickupable foundPickupable = null;

        if (interactionOrigin != null)
        {
            // SphereCast en vez de Raycast simple: da margen para "apuntar"
            // a la caja sin tener que estar pixel-perfect sobre ella.
            if (Physics.SphereCast(
                    interactionOrigin.position,
                    interactionRadius,
                    interactionOrigin.forward,
                    out RaycastHit hit,
                    interactionRange,
                    pickupableLayer))
            {
                foundPickupable = hit.collider.GetComponentInParent<Pickupable>();
            }
        }

        //Debug.Log("Interaction detection: " + (foundPickupable != null ? foundPickupable.name : "nothing"));
        _lookedAtPickupable = foundPickupable;

        // Muestra u oculta el prompt de UI segun si hay algo que recoger.
        if (promptUI != null)
            promptUI.SetActive(_lookedAtPickupable != null);
    }

    // ===== INTERACCION =========================================================

    void HandleInteractPressed()
    {
        if (_heldPickupable != null)
            DropCurrent();
        else if (_lookedAtPickupable != null)
            PickUp(_lookedAtPickupable);
    }

    void PickUp(Pickupable pickupable)
    {
        _heldPickupable = pickupable;
        pickupable.OnPickedUp(holdPoint);

        // Mientras cargamos algo, dejamos de mostrar el prompt de "Recoger".
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void DropCurrent()
    {
        if (_heldPickupable == null) return;

        _heldPickupable.OnDropped();
        _heldPickupable = null;
    }

    void HandleThrowPressed()
    {
        // Solo tiene sentido lanzar/patear algo que ya tengamos en manos.
        if (_heldPickupable == null) return;

        ThrowCurrent();
    }

    void ThrowCurrent()
    {
        Vector3 throwDirection = interactionOrigin != null ? interactionOrigin.forward : transform.forward;
        throwDirection += Vector3.up * throwUpwardBoost; // pequeno arco, no un tiro completamente plano

        _heldPickupable.OnThrown(throwDirection.normalized * throwForce);
        _heldPickupable = null;

        // Al lanzar tambien liberamos las manos, asi que el prompt puede
        // volver a activarse en el siguiente frame si miramos otra caja.
    }

    // ===== GIZMOS ==============================================================

    void OnDrawGizmosSelected()
    {
        if (interactionOrigin == null) return;

        Gizmos.color = _lookedAtPickupable != null ? Color.green : Color.yellow;
        Gizmos.DrawLine(interactionOrigin.position, interactionOrigin.position + interactionOrigin.forward * interactionRange);
        Gizmos.DrawWireSphere(interactionOrigin.position + interactionOrigin.forward * interactionRange, interactionRadius);
    }
}
