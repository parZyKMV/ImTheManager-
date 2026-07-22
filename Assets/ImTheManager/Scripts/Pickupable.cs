using UnityEngine;

/// <summary>
/// Componente que se agrega a cualquier objeto que el jugador pueda recoger y cargar
/// (cajas, productos, etc). Requiere un Rigidbody y un Collider en el mismo objeto.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Pickupable : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private string interactionPrompt = "Recoger"; // texto para tu UI, por si personalizas por objeto

    private Rigidbody _rigidbody;
    private Transform _originalParent;
    private bool _isHeld = false;

    public bool IsHeld => _isHeld;
    public string InteractionPrompt => interactionPrompt;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _originalParent = transform.parent;
    }

    /// <summary>
    /// Llamado por PlayerInteractor cuando el jugador recoge este objeto.
    /// </summary>
    public void OnPickedUp(Transform holdPoint)
    {
        _isHeld = true;

        // Desactivamos la fisica mientras esta en las manos del jugador,
        // para que no "pelee" contra la posicion que le imponemos al parentarlo.
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Llamado por PlayerInteractor cuando el jugador suelta este objeto suavemente
    /// (sin fuerza, se queda cayendo donde este).
    /// </summary>
    public void OnDropped()
    {
        ReleasePhysics();
    }

    /// <summary>
    /// Llamado por PlayerInteractor cuando el jugador lanza/patea este objeto.
    /// Reactiva la fisica y le aplica la fuerza recibida.
    /// </summary>
    /// <param name="force">Vector de fuerza a aplicar (direccion * intensidad).</param>
    public void OnThrown(Vector3 force)
    {
        ReleasePhysics();
        _rigidbody.AddForce(force, ForceMode.VelocityChange);
    }

    // Logica compartida entre soltar y lanzar: quitarlo del holdPoint
    // y devolverle el control a la fisica normal.
    private void ReleasePhysics()
    {
        _isHeld = false;

        transform.SetParent(_originalParent);

        _rigidbody.useGravity = true;
        _rigidbody.isKinematic = false;
    }
}
