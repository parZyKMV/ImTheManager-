using UnityEngine;

/// <summary>
/// Permite agarrar y lanzar CLIENTES durante Rage Mode. A diferencia de
/// Pickupable normal, un cliente es un esqueleto completo con ragdoll -
/// esto se engancha directo con CustomerRagdoll en vez de tener su propio
/// Rigidbody/Collider simple.
/// </summary>
[RequireComponent(typeof(CustomerRagdoll))]
public class CustomerPickupable : MonoBehaviour
{
    public bool IsHeld { get; private set; } = false;

    private CustomerRagdoll _ragdoll;
    private CustomerAudio _audio;
    private Rigidbody _mainBody;
    private Transform _originalParent; // el padre real dentro del esqueleto, NO holdPoint

    void Awake()
    {
        _ragdoll = GetComponent<CustomerRagdoll>();
        _audio = GetComponent<CustomerAudio>();
    }

    /// <summary>Solo se puede agarrar durante Rage Mode y si no esta ya cargado.</summary>
    public bool CanBePickedUp()
    {
        return RageModeController.Instance != null && RageModeController.Instance.IsActive && !IsHeld;
    }

    public void OnPickedUp(Transform holdPoint)
    {
        if (!CanBePickedUp()) return;

        IsHeld = true;

        // Activa el ragdoll (cuerpo flojo, IA/animator apagados) si todavia
        // no estaba activo - sin fuerza de impacto, solo lo "prepara".
        if (!_ragdoll.IsRagdollActive)
            _ragdoll.ForceRagdoll(Vector3.zero);

        _ragdoll.IsBeingHeld = true;
        _mainBody = _ragdoll.MainBody;

        if (_mainBody != null)
        {
            _originalParent = _mainBody.transform.parent; // lo recordamos para poder devolverlo despues
            _mainBody.isKinematic = true; // mientras esta en la mano, no cae solo por gravedad
            _mainBody.transform.SetParent(holdPoint);
            _mainBody.transform.localPosition = Vector3.zero;
            _mainBody.transform.localRotation = Quaternion.identity;
        }
    }

    public void OnDropped()
    {
        if (!IsHeld) return;
        ReleaseWithForce(Vector3.zero);
    }

    public void OnThrown(Vector3 force)
    {
        if (!IsHeld) return;
        ReleaseWithForce(force);
    }

    void ReleaseWithForce(Vector3 force)
    {
        IsHeld = false;

        if (_mainBody != null)
        {
            // IMPORTANTE: devolvemos al padre original dentro del esqueleto,
            // NUNCA a null - SetParent(null) desconectaria este hueso de todo
            // el rig para siempre, dejando un "cuerpo fantasma" congelado
            // mientras el resto del cliente (NavMeshAgent/Behavior Graph en
            // 'root') sigue funcionando por su lado, sin conexion.
            _mainBody.transform.SetParent(_originalParent);
            _mainBody.isKinematic = false;
        }

        // La fuerza se aplica a TODO el esqueleto a la vez (no solo a
        // _mainBody) - si no, los Joints que conectan los huesos absorben
        // el impulso y se siente como que lo sueltas en vez de lanzarlo.
        if (force != Vector3.zero)
        {
            _ragdoll.ApplyForceToAllBodies(force);

            // El grito solo suena si de verdad lo LANZASTE (fuerza real),
            // no si solo lo soltaste parado (OnDropped pasa force=Vector3.zero).
            // Se corta solo cuando CustomerRagdoll detecta que aterrizo.
            _audio?.PlayScream();
        }

        _ragdoll.IsBeingHeld = false;
        _ragdoll.ResetDownTimer(); // le da una caida/recuperacion completa nueva desde este momento
    }
}