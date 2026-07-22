using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de arrastre point-and-click para el modo caja registradora.
/// El jugador hace click sobre un producto del mostrador y lo arrastra
/// (con el cursor libre, no con mouse-look) hasta el escaner o la bolsa.
/// Reutiliza ScannableProduct/RegisterScanner: como el producto sigue
/// teniendo su Collider + Rigidbody mientras se arrastra, si pasa por el
/// trigger del escaner se escanea automaticamente, sin logica extra.
/// </summary>
public class CounterItemDragController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera registerCamera; // si lo dejas vacio, usa Camera.main
    [SerializeField] private LayerMask draggableLayer; // layer de los productos sobre el mostrador
    [SerializeField] private float maxRaycastDistance = 10f;

    [Header("Límites del mostrador")]
    [Tooltip("Collider (puede ser Trigger) que marca el area valida del mostrador. " +
             "El producto no se puede arrastrar mas alla de sus bordes en X/Z.")]
    [SerializeField] private Collider dragBounds;

    [Header("Colisión con la registradora")]
    [Tooltip("Layer del cuerpo solido de la registradora (la maquina en si), " +
             "para que el producto no se pueda arrastrar 'a traves' de ella.")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Radio aproximado del producto, usado para el chequeo de colision durante el arrastre.")]
    [SerializeField] private float itemCollisionRadius = 0.15f;

    private ScannableProduct _draggedItem;
    private Rigidbody _draggedRigidbody;
    private float _dragHeight; // altura Y fija durante el arrastre (la del mostrador)

    void Update()
    {
        if (registerCamera == null)
            registerCamera = Camera.main;

        if (registerCamera == null || Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            TryStartDrag();

        if (_draggedItem != null && Mouse.current.leftButton.isPressed)
            UpdateDragPosition();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    // ===== INICIO DEL ARRASTRE ==================================================

    void TryStartDrag()
    {
        Ray ray = registerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, draggableLayer))
            return;

        ScannableProduct scannable = hit.collider.GetComponentInParent<ScannableProduct>();
        if (scannable == null) return;

        _draggedItem = scannable;
        _draggedRigidbody = scannable.GetComponent<Rigidbody>();

        // Kinematic mientras se arrastra, para que la fisica no pelee
        // contra la posicion que le imponemos con el mouse.
        if (_draggedRigidbody != null)
            _draggedRigidbody.isKinematic = true;

        _dragHeight = scannable.transform.position.y;
    }

    // ===== DURANTE EL ARRASTRE ====================================================

    void UpdateDragPosition()
    {
        Ray ray = registerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        // Plano horizontal a la altura del mostrador: el producto se desliza
        // sobre esa superficie siguiendo al cursor. Solo se mueve en X/Z,
        // la altura (Y) queda fija donde estaba al agarrarlo.
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, _dragHeight, 0f));

        if (!dragPlane.Raycast(ray, out float distance))
            return;

        Vector3 targetPoint = ray.GetPoint(distance);

        // 1) No se puede alejar del mostrador: recortamos X/Z contra los
        // bordes del Collider asignado en dragBounds.
        if (dragBounds != null)
        {
            Bounds b = dragBounds.bounds;
            targetPoint.x = Mathf.Clamp(targetPoint.x, b.min.x, b.max.x);
            targetPoint.z = Mathf.Clamp(targetPoint.z, b.min.z, b.max.z);
        }

        // 2) No se puede arrastrar "a traves" del cuerpo de la registradora:
        // si el punto destino se meteria dentro de un obstaculo solido,
        // simplemente no nos movemos hacia ahi (el producto se "traba").
        bool wouldHitObstacle = Physics.CheckSphere(targetPoint, itemCollisionRadius, obstacleLayer);
        if (wouldHitObstacle)
            return;

        _draggedItem.transform.position = targetPoint;
    }

    // ===== FIN DEL ARRASTRE ========================================================

    void EndDrag()
    {
        if (_draggedItem == null) return;

        // Reactiva la fisica: el producto cae naturalmente donde se solto.
        if (_draggedRigidbody != null)
            _draggedRigidbody.isKinematic = false;

        _draggedItem = null;
        _draggedRigidbody = null;
    }
}