using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Se coloca en el Player (junto a RPS_ThirdPersonController).
/// Detecta objetos Pickupable, estantes y basura frente al jugador, muestra
/// un prompt de UI, y maneja recoger/soltar/reabastecer/limpiar con la
/// accion de Input "Interact".
///
/// Los productos "fuera de lugar" (dejados por CreateMessAction) son un
/// Pickupable normal: se recogen como cualquier caja, y se devuelven a su
/// estante mirandolo y presionando Interact mientras se cargan (ver
/// TryReturnHeldProductToShelf).
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Deteccion")]
    [SerializeField] private Transform interactionOrigin; // normalmente la camara; si lo dejas vacio usa Camera.main
    [SerializeField] private float interactionRange = 2.5f;
    [SerializeField] private float interactionRadius = 0.4f; // grosor del SphereCast, da margen para "apuntar"
    [SerializeField] private LayerMask pickupableLayer;       // crea una Layer "Pickupable" y asignala aqui
    [SerializeField] private LayerMask restockableLayer;      // layer de los estantes (ShelfRestockSystem)
    [SerializeField] private LayerMask trashLayer;            // layer de los TrashItem
    [SerializeField] private LayerMask customerLayer;         // layer de los clientes (solo se usa durante Rage Mode)
    [SerializeField] private Animator animator;

    [Header("Cargar objetos")]
    [SerializeField] private Transform holdPoint; // punto (hijo de la camara) donde se sostiene la caja

    [Header("Lanzar / patear")]
    [SerializeField] public float throwForce = 8f;       // intensidad del lanzamiento
    [SerializeField] private float throwUpwardBoost = 0.15f; // le da un poco de arco hacia arriba, no un tiro plano
    [Tooltip("Multiplicador extra al lanzar un CLIENTE (mas pesado/con joints que un objeto normal, necesita mas fuerza para volar igual de lejos).")]
    [SerializeField] private float customerThrowForceMultiplier = 1.5f;

    [Header("Ordenar estante (mantener Interact)")]
    [SerializeField] private float tidyHoldDuration = 5f;
    [SerializeField] private Image tidyProgressBar; // opcional, fillAmount 0-1

    [Header("UI")]
    [SerializeField] private GameObject promptUI; // objeto con el texto "Recoger [E]", se activa/desactiva solo
    [SerializeField] private TMP_Text quantityText; // opcional: muestra cantidad/estado segun el contexto

    private InputAction _interactAction;
    private InputAction _throwAction;

    private Pickupable _lookedAtPickupable;    // lo que estamos mirando ahora (aun no recogido)
    private Pickupable _heldPickupable;        // lo que estamos cargando actualmente
    private ShelfRestockSystem _lookedAtShelf; // estante que estamos mirando ahora (necesita reabastecer o esta desordenado)
    private TrashItem _lookedAtTrash;          // basura que estamos mirando ahora
    private CustomerPickupable _lookedAtCustomer; // cliente agarrable que miramos (solo durante Rage Mode)
    private CustomerPickupable _heldCustomer;      // cliente que estamos cargando actualmente

    private float _tidyHoldTimer = 0f;

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

        if (_interactAction == null)
            Debug.LogWarning("[PlayerInteractor] No se encontro la accion 'Interact'. Agregala en tu Input Actions asset (boton, ej. tecla E).");

        // El Throw se crea a mano en vez de buscarlo en el Input Actions
        // asset: ese asset ha perdido la accion "Throw" varias veces al
        // reabrir el proyecto (problema de guardado de Unity, no de este
        // script) - crearla directo en codigo la hace inmune a eso.
        _throwAction = new InputAction("Throw", binding: "<Mouse>/leftButton");
        _throwAction.Enable();

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

    void OnDestroy()
    {
        _throwAction?.Dispose();
    }

    // ===== UPDATE =============================================================

    void Update()
    {
        // Solo buscamos algo nuevo que recoger si las manos estan libres.
        if (_heldPickupable == null)
            DetectPickupable();
        else
            _lookedAtPickupable = null;

        // Estos se detectan siempre, sin importar si las manos estan ocupadas.
        DetectRestockableShelf();
        DetectTrash();

        // Los clientes solo se pueden agarrar durante Rage Mode, y con las
        // manos libres (no a la vez que una caja/producto normal).
        if (_heldPickupable == null && _heldCustomer == null)
            DetectGrabbableCustomer();
        else
            _lookedAtCustomer = null;

        if (_interactAction != null && _interactAction.WasPressedThisFrame())
            HandleInteractPressed();

        // El "ordenar estante" es un hold, no un click instantaneo - se
        // revisa cada frame mientras se mantiene presionado Interact.
        HandleTidyHold();

        if (_throwAction != null && _throwAction.WasPressedThisFrame())
            HandleThrowPressed();
    }

    // ===== DETECCION ===========================================================

    void DetectPickupable()
    {
        Pickupable foundPickupable = null;

        if (interactionOrigin != null)
        {
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

        _lookedAtPickupable = foundPickupable;
        UpdatePrompt();
    }

    void DetectRestockableShelf()
    {
        ShelfRestockSystem foundShelf = null;

        if (interactionOrigin != null)
        {
            if (Physics.SphereCast(
                    interactionOrigin.position,
                    interactionRadius,
                    interactionOrigin.forward,
                    out RaycastHit hit,
                    interactionRange,
                    restockableLayer))
            {
                var shelf = hit.collider.GetComponentInParent<ShelfRestockSystem>();
                // Nos interesa si hace falta reabastecerlo O si esta desordenado.
                if (shelf != null && (shelf.CanRestock || shelf.NeedsTidying))
                    foundShelf = shelf;
            }
        }

        _lookedAtShelf = foundShelf;
        UpdatePrompt();
    }

    void DetectTrash()
    {
        TrashItem foundTrash = null;

        if (interactionOrigin != null)
        {
            if (Physics.SphereCast(
                    interactionOrigin.position,
                    interactionRadius,
                    interactionOrigin.forward,
                    out RaycastHit hit,
                    interactionRange,
                    trashLayer))
            {
                foundTrash = hit.collider.GetComponentInParent<TrashItem>();
            }
        }

        _lookedAtTrash = foundTrash;
        UpdatePrompt();
    }

    void DetectGrabbableCustomer()
    {
        CustomerPickupable foundCustomer = null;

        // Solo tiene sentido buscar clientes para agarrar durante Rage Mode.
        if (interactionOrigin != null && RageModeController.Instance != null && RageModeController.Instance.IsActive)
        {
            if (Physics.SphereCast(
                    interactionOrigin.position,
                    interactionRadius,
                    interactionOrigin.forward,
                    out RaycastHit hit,
                    interactionRange,
                    customerLayer,
                    QueryTriggerInteraction.Collide)) // el collider raiz del cliente es un Trigger
            {
                var customer = hit.collider.GetComponentInParent<CustomerPickupable>();
                if (customer != null && customer.CanBePickedUp())
                    foundCustomer = customer;
            }
        }

        _lookedAtCustomer = foundCustomer;
        UpdatePrompt();
    }

    // Muestra el prompt de UI y actualiza el texto de cantidad/estado segun el contexto.
    void UpdatePrompt()
    {
        bool hasSomethingToShow = _lookedAtPickupable != null || _lookedAtShelf != null
                                   || _lookedAtTrash != null || _lookedAtCustomer != null;

        if (promptUI != null)
            promptUI.SetActive(hasSomethingToShow);

        if (quantityText == null) return;

        if (_lookedAtCustomer != null)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = "Agarrar cliente [Interact]";
            return;
        }

        StockBox heldBox = _heldPickupable != null ? _heldPickupable.GetComponent<StockBox>() : null;
        ScannableProduct heldProduct = _heldPickupable != null ? _heldPickupable.GetComponent<ScannableProduct>() : null;
        TrashBin heldBin = _heldPickupable != null ? _heldPickupable.GetComponent<TrashBin>() : null;

        if (_lookedAtShelf != null && _lookedAtShelf.NeedsTidying)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = "Estante desordenado — manten Interact para ordenar";
        }
        else if (heldBox != null && _lookedAtShelf != null)
        {
            ShelfSlot slot = _lookedAtShelf.Slot;
            quantityText.gameObject.SetActive(true);
            quantityText.text = $"Caja: {heldBox.Quantity}  |  Estante: {slot.CurrentQuantity}/{slot.MaxQuantity}";
        }
        else if (heldProduct != null && _lookedAtShelf != null && heldProduct.ProductData == _lookedAtShelf.Slot.ProductType)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = "Devolver al estante [Interact]";
        }
        else if (_lookedAtShelf != null)
        {
            ShelfSlot slot = _lookedAtShelf.Slot;
            quantityText.gameObject.SetActive(true);
            quantityText.text = $"Estante: {slot.CurrentQuantity}/{slot.MaxQuantity}";
        }
        else if (heldBin != null && _lookedAtTrash != null)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = $"Bote: {heldBin.CollectedCount} recolectada(s)";
        }
        else if (_lookedAtTrash != null)
        {
            quantityText.gameObject.SetActive(true);
            quantityText.text = "Necesitas un bote de basura para esto";
        }
        else if (_lookedAtPickupable != null)
        {
            StockBox lookedBox = _lookedAtPickupable.GetComponent<StockBox>();

            if (lookedBox != null)
            {
                quantityText.gameObject.SetActive(true);
                quantityText.text = $"Caja: {lookedBox.Quantity}";
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        else
        {
            quantityText.gameObject.SetActive(false);
        }
    }

    // ===== INTERACCION =========================================================

    void HandleInteractPressed()
    {
        if (_heldCustomer != null)
        {
            DropCustomer();
            return;
        }

        if (_heldPickupable != null)
        {
            StockBox heldBox = _heldPickupable.GetComponent<StockBox>();
            ScannableProduct heldProduct = _heldPickupable.GetComponent<ScannableProduct>();
            TrashBin heldBin = _heldPickupable.GetComponent<TrashBin>();

            // Caja de bodega + estante que necesita stock -> reabastecer.
            if (heldBox != null && _lookedAtShelf != null && _lookedAtShelf.CanRestock)
            {
                TryRestockWithHeldBox(heldBox);
                return;
            }

            // Producto suelto (fuera de lugar u otro) + su estante correspondiente -> devolverlo.
            if (heldProduct != null && _lookedAtShelf != null && _lookedAtShelf.CanRestock
                && heldProduct.ProductData == _lookedAtShelf.Slot.ProductType)
            {
                TryReturnHeldProductToShelf(heldProduct);
                return;
            }

            // Bote de basura + basura -> recolectarla.
            if (heldBin != null && _lookedAtTrash != null)
            {
                CollectTrash(heldBin);
                return;
            }

            // Nota: ordenar un estante desordenado es un HOLD, no un click -
            // se maneja en HandleTidyHold(), no aca.

            DropCurrent();
        }
        else if (_lookedAtCustomer != null)
        {
            PickUpCustomer(_lookedAtCustomer);
        }
        else if (_lookedAtPickupable != null)
        {
            PickUp(_lookedAtPickupable);
        }
    }

    void PickUpCustomer(CustomerPickupable customer)
    {
        _heldCustomer = customer;
        customer.OnPickedUp(holdPoint);

        // Misma animacion de "cargando algo" que se usa para cajas -
        // agarrar un cliente durante Rage Mode se ve igual que cargar un objeto.
        animator?.SetBool("PickUp", true);

        if (promptUI != null) promptUI.SetActive(false);
        if (quantityText != null) quantityText.gameObject.SetActive(false);
    }

    void DropCustomer()
    {
        if (_heldCustomer == null) return;

        animator?.SetBool("PickUp", false);

        _heldCustomer.OnDropped();
        _heldCustomer = null;
    }

    void TryRestockWithHeldBox(StockBox box)
    {
        int transferred = _lookedAtShelf.RestockFromBox(box);

        if (transferred > 0)
            Debug.Log($"[PlayerInteractor] Reabastecio {transferred} unidad(es) en '{_lookedAtShelf.name}'.");

        // Si la caja quedo vacia, StockBox.RemoveUnits ya la destruyo solo -
        // liberamos la referencia para que el jugador quede con las manos libres.
        if (box == null || box.Quantity <= 0)
        {
            animator?.SetBool("PickUp", false);
            _heldPickupable = null;
        }
    }

    void TryReturnHeldProductToShelf(ScannableProduct product)
    {
        bool accepted = _lookedAtShelf.ReturnProduct(product);
        if (!accepted) return;

        Debug.Log($"[PlayerInteractor] Devolvio '{product.ProductData?.productName}' a su estante.");

        product.GetComponent<MisplacedProductMarker>()?.MarkCleaned();

        animator?.SetBool("PickUp", false);

        Destroy(product.gameObject);
        _heldPickupable = null;
    }

    void CollectTrash(TrashBin bin)
    {
        if (_lookedAtTrash == null) return;

        bin.CollectTrash(_lookedAtTrash);
        Debug.Log($"[PlayerInteractor] Recogio basura. Total en el bote: {bin.CollectedCount}.");

        _lookedAtTrash = null;
        UpdatePrompt();
    }

    void PickUp(Pickupable pickupable)
    {
        _heldPickupable = pickupable;
        pickupable.OnPickedUp(holdPoint);

        animator?.SetBool("PickUp", true);

        // Mientras cargamos algo, dejamos de mostrar el prompt de "Recoger".
        if (promptUI != null)
            promptUI.SetActive(false);

        if (quantityText != null)
            quantityText.gameObject.SetActive(false);
    }

    void DropCurrent()
    {
        if (_heldPickupable == null) return;

        animator?.SetBool("PickUp", false);

        _heldPickupable.OnDropped();
        _heldPickupable = null;
    }

    // ===== ORDENAR ESTANTE (HOLD) ===============================================

    void HandleTidyHold()
    {
        bool canTidyNow = _interactAction != null && _lookedAtShelf != null && _lookedAtShelf.NeedsTidying;

        if (!canTidyNow || !_interactAction.IsPressed())
        {
            if (_tidyHoldTimer > 0f)
            {
                _tidyHoldTimer = 0f;
                UpdateTidyProgressUI(0f);
            }
            return;
        }

        _tidyHoldTimer += Time.deltaTime;
        UpdateTidyProgressUI(_tidyHoldTimer / tidyHoldDuration);

        if (_tidyHoldTimer >= tidyHoldDuration)
        {
            _lookedAtShelf.Slot.Tidy();
            _tidyHoldTimer = 0f;
            UpdateTidyProgressUI(0f);
            Debug.Log($"[PlayerInteractor] Ordeno el estante '{_lookedAtShelf.name}'.");
        }
    }

    void UpdateTidyProgressUI(float progress01)
    {
        if (tidyProgressBar == null) return;

        tidyProgressBar.gameObject.SetActive(progress01 > 0f);
        tidyProgressBar.fillAmount = Mathf.Clamp01(progress01);
    }

    // ===== LANZAR / PATEAR =======================================================

    void HandleThrowPressed()
    {
        if (_heldCustomer == null && _heldPickupable == null) return;

        ThrowCurrent();
    }

    void ThrowCurrent()
    {
        Vector3 throwDirection = interactionOrigin != null ? interactionOrigin.forward : transform.forward;
        throwDirection += Vector3.up * throwUpwardBoost;

        // El bool de animacion se apaga en los dos casos (cliente u objeto) -
        // ya sea que lo tires o lo lances, dejamos de "cargar" algo.
        animator?.SetBool("PickUp", false);

        if (_heldCustomer != null)
        {
            Vector3 customerForce = throwDirection.normalized * throwForce * customerThrowForceMultiplier;
            _heldCustomer.OnThrown(customerForce);
            _heldCustomer = null;
            return;
        }

        Vector3 force = throwDirection.normalized * throwForce;
        _heldPickupable.OnThrown(force);
        _heldPickupable = null;
    }

    // ===== GIZMOS ==============================================================

    void OnDrawGizmosSelected()
    {
        if (interactionOrigin == null) return;

        bool lookingAtSomething = _lookedAtPickupable != null || _lookedAtShelf != null
                                  || _lookedAtTrash != null || _lookedAtCustomer != null;

        Gizmos.color = lookingAtSomething ? Color.green : Color.yellow;
        Gizmos.DrawLine(interactionOrigin.position, interactionOrigin.position + interactionOrigin.forward * interactionRange);
        Gizmos.DrawWireSphere(interactionOrigin.position + interactionOrigin.forward * interactionRange, interactionRadius);
    }
}