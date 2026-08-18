using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;

/// <summary>
/// Ragdoll para clientes: al recibir un golpe fuerte de un Pickupable
/// lanzado, el cliente cae en ragdoll. A diferencia del script original:
/// - NO se destruye el GameObject.
/// - Despues de caer, el cuerpo se "congela" (deja de moverse/temblar) en
///   vez de seguir simulando fisica todo el tiempo que esta tirado.
/// - Se levanta solo despues de un rato y retoma su comportamiento normal
///   (NavMeshAgent + Behavior Graph vuelven a activarse).
/// TODO: enganchar dialogo de reaccion en GetUp() cuando lo armemos.
/// </summary>
public class CustomerRagdoll : MonoBehaviour
{
    [Header("Impacto")]
    [SerializeField] private float minImpact = 3f;
    [SerializeField] private ParticleSystem hitEffect;

    [Header("Recuperación")]
    [Tooltip("Velocidad por debajo de la cual consideramos que el cuerpo ya aterrizo/dejo de moverse.")]
    [SerializeField] private float settleVelocityThreshold = 0.5f;
    [Tooltip("Cuanto tiempo debe mantenerse quieto antes de congelarse de verdad (evita congelar en medio de un rebote).")]
    [SerializeField] private float settleGracePeriod = 0.3f;
    [Tooltip("Tiempo maximo esperando que aterrice antes de forzar el congelado de todas formas (red de seguridad por si queda volando por un bug de fisica).")]
    [SerializeField] private float maxAirborneTime = 5f;
    [Tooltip("Cuanto tiempo total se queda tirado (congelado) antes de levantarse solo, contado desde que aterriza.")]
    [SerializeField] private float downDuration = 3f;

    [Header("Esqueleto")]
    [Tooltip("El GameObject del MODELO (el que tiene el Animator y los huesos), NO el objeto raiz con la capsula de navegacion. Si lo dejas vacio, busca el primer Animator en los hijos.")]
    [SerializeField] private Transform skeletonRoot;

    private Rigidbody[] _ragdollBodies;
    private Collider[] _ragdollColliders;
    private Animator _animator;
    private NavMeshAgent _navAgent;
    private BehaviorGraphAgent _behaviorAgent;
    private Collider _mainCollider;
    private Rigidbody _mainRagdollBody; // referencia de "donde quedo tirado" para pararse ahi

    private bool _isRagdoll = false;
    private bool _hasSettled = false;
    private float _downTimer = 0f;
    private float _settleGraceTimer = 0f;
    private float _airborneTimer = 0f;

    /// <summary>True mientras el cliente esta en ragdoll (tirado, cargado, o recien lanzado).</summary>
    public bool IsRagdollActive => _isRagdoll;

    /// <summary>El Rigidbody principal del ragdoll (normalmente Hips/Pelvis) - usado por CustomerPickupable para cargar/lanzar.</summary>
    public Rigidbody MainBody => _mainRagdollBody;

    /// <summary>
    /// True mientras CustomerPickupable esta cargando a este cliente en la
    /// mano - pausa el timer de "se congela y se levanta solo" para que no
    /// intente pararse mientras el jugador todavia lo tiene agarrado.
    /// </summary>
    public bool IsBeingHeld { get; set; } = false;

    void Start()
    {
        if (skeletonRoot == null)
        {
            _animator = GetComponentInChildren<Animator>();
            skeletonRoot = _animator != null ? _animator.transform : null;
        }
        else
        {
            _animator = skeletonRoot.GetComponent<Animator>();
        }

        if (skeletonRoot == null)
        {
            Debug.LogError("[CustomerRagdoll] No se encontro el modelo del esqueleto (Animator). Asigna 'Skeleton Root' a mano.");
        }
        else
        {
            // IMPORTANTE: buscamos SOLO dentro del modelo/esqueleto, no desde
            // este objeto raiz - si buscaramos desde aca, agarrariamos
            // tambien el Rigidbody de la capsula de navegacion (root), que
            // NO es un hueso y arruina el ragdoll (la capsula se queda
            // "parada" bajo gravedad en vez de que el esqueleto caiga).
            _ragdollBodies = skeletonRoot.GetComponentsInChildren<Rigidbody>();
            _ragdollColliders = skeletonRoot.GetComponentsInChildren<Collider>();
        }

        _navAgent = GetComponent<NavMeshAgent>();
        _behaviorAgent = GetComponent<BehaviorGraphAgent>();
        _mainCollider = GetComponent<Collider>();

        if (_ragdollBodies != null && _ragdollBodies.Length > 0)
            _mainRagdollBody = _ragdollBodies[0]; // normalmente el hueso Hips/Pelvis - ajusta el orden si hace falta

        DisableRagdoll();
    }

    void Update()
    {
        if (!_isRagdoll || IsBeingHeld) return;

        if (!_hasSettled)
        {
            // No congelamos con un timer fijo (podria caer a mitad de vuelo
            // si lo lanzaron con fuerza) - esperamos a que el cuerpo
            // principal realmente se frene, y le damos una pequena "gracia"
            // para no congelarlo en medio de un rebote.
            bool isMovingFast = _mainRagdollBody != null
                && _mainRagdollBody.linearVelocity.sqrMagnitude >= settleVelocityThreshold * settleVelocityThreshold;

            _airborneTimer += Time.deltaTime;
            _settleGraceTimer = isMovingFast ? 0f : _settleGraceTimer + Time.deltaTime;

            bool hasSettledNaturally = _settleGraceTimer >= settleGracePeriod;
            bool hitSafetyTimeout = _airborneTimer >= maxAirborneTime;

            if (hasSettledNaturally || hitSafetyTimeout)
            {
                FreezeRagdoll();
                _hasSettled = true;
                _downTimer = 0f; // el tiempo "tirado" empieza a contar desde que aterriza, no desde que se lanzo
            }

            return;
        }

        _downTimer += Time.deltaTime;
        if (_downTimer >= downDuration)
            GetUp();
    }

    void DisableRagdoll()
    {
        foreach (var rb in _ragdollBodies)
            rb.isKinematic = true;

        foreach (var col in _ragdollColliders)
            col.enabled = false;

        // Mantiene el collider raiz activo para detectar el golpe.
        if (_mainCollider != null)
            _mainCollider.enabled = true;

        if (_animator != null)
            _animator.enabled = true;
    }

    /// <summary>
    /// Version publica de EnableRagdoll, para que otros sistemas (ej.
    /// CustomerPickupable al agarrar al cliente durante Rage Mode) puedan
    /// activar el ragdoll sin necesitar un impacto de Pickupable real.
    /// </summary>
    public void ForceRagdoll(Vector3 force)
    {
        EnableRagdoll(force);
    }

    /// <summary>Reinicia el timer de caida/recuperacion - usalo al lanzar un cliente para que tenga una "caida" completa nueva.</summary>
    public void ResetDownTimer()
    {
        _downTimer = 0f;
        _hasSettled = false;
        _settleGraceTimer = 0f;
        _airborneTimer = 0f;
    }

    /// <summary>
    /// Aplica la misma fuerza a TODOS los huesos del ragdoll a la vez, no
    /// solo al principal. Necesario al lanzar un cliente completo: si solo
    /// se empuja un hueso (ej. Hips), los Joints que lo conectan al resto
    /// del esqueleto absorben gran parte del impulso, haciendo que se
    /// sienta como que "se suelta" en vez de salir volando.
    /// </summary>
    public void ApplyForceToAllBodies(Vector3 force, ForceMode mode = ForceMode.VelocityChange)
    {
        if (_ragdollBodies == null) return;

        foreach (var rb in _ragdollBodies)
        {
            if (rb != null && !rb.isKinematic)
                rb.AddForce(force, mode);
        }
    }

    void EnableRagdoll(Vector3 force)
    {
        if (_isRagdoll) return;

        Debug.Log($"[CustomerRagdoll] EnableRagdoll: {_ragdollBodies.Length} rigidbodies, {_ragdollColliders.Length} colliders encontrados.");

        _isRagdoll = true;
        _hasSettled = false;
        _downTimer = 0f;
        _settleGraceTimer = 0f;
        _airborneTimer = 0f;

        // Pausa la IA mientras esta en ragdoll - si no, el NavMeshAgent
        // pelea contra la fisica intentando volver a su ruta original.
        if (_navAgent != null)
            _navAgent.enabled = false;

        if (_behaviorAgent != null)
            _behaviorAgent.enabled = false;

        if (_mainCollider != null)
            _mainCollider.enabled = false;

        foreach (var rb in _ragdollBodies)
            rb.isKinematic = false;

        foreach (var col in _ragdollColliders)
            col.enabled = true;

        if (_animator != null)
            _animator.enabled = false;

        if (_ragdollBodies.Length > 0)
        {
            _ragdollBodies[0].AddForce(force, ForceMode.Impulse);
            Debug.Log($"[CustomerRagdoll] Fuerza aplicada a '{_ragdollBodies[0].name}': {force}.");
        }
        else
        {
            Debug.LogWarning("[CustomerRagdoll] No hay ningun Rigidbody en _ragdollBodies para aplicarle fuerza.");
        }

        if (hitEffect != null)
        {
            ParticleSystem instance = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(instance.gameObject, 3f);
        }

        // Ya NO se destruye el GameObject aca - se levanta solo (ver GetUp).
    }

    // Congela el ragdoll en la pose donde cayo, para que deje de temblar/
    // deslizarse por fisica residual mientras espera a levantarse.
    void FreezeRagdoll()
    {
        foreach (var rb in _ragdollBodies)
            rb.isKinematic = true;
    }

    void GetUp()
    {
        _isRagdoll = false;

        // Reposiciona la raiz del objeto donde quedo el "torso" del ragdoll,
        // para que al reactivar el Animator no aparezca teletransportado
        // a donde estaba antes de caer.
        if (_mainRagdollBody != null)
        {
            transform.position = _mainRagdollBody.position;
            transform.rotation = Quaternion.Euler(0f, _mainRagdollBody.rotation.eulerAngles.y, 0f);
        }

        DisableRagdoll();

        // Vuelve a poner al agente sobre el NavMesh en su nueva posicion.
        // Sin Warp(), el NavMeshAgent puede quedar "perdido" si la fisica
        // lo tiro fuera de donde estaba parado.
        if (_navAgent != null)
        {
            _navAgent.enabled = true;
            _navAgent.Warp(transform.position);
        }

        if (_behaviorAgent != null)
            _behaviorAgent.enabled = true;

        // TODO: aca enganchamos el dialogo de reaccion (ej. "¡Oye, eso dolió!")
        // cuando lo armemos - probablemente via KarenEventTrigger.TriggerEncounterWithoutLocking().
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[CustomerRagdoll] OnTriggerEnter con '{other.name}' (layer: {LayerMask.LayerToName(other.gameObject.layer)}).");

        if (_isRagdoll)
        {
            Debug.Log("[CustomerRagdoll] Ya esta en ragdoll, se ignora.");
            return;
        }

        // Usamos el componente Pickupable en vez de un Tag - mas confiable,
        // no depende de que el Tag este bien escrito/asignado en cada prefab.
        Pickupable pickupable = other.GetComponentInParent<Pickupable>();
        if (pickupable == null)
        {
            Debug.Log($"[CustomerRagdoll] '{other.name}' no tiene componente Pickupable. Se ignora.");
            return;
        }

        Rigidbody thrownRb = other.GetComponentInParent<Rigidbody>();
        if (thrownRb == null)
        {
            Debug.LogWarning($"[CustomerRagdoll] '{other.name}' tiene Pickupable pero no Rigidbody.");
            return;
        }

        float impact = thrownRb.linearVelocity.magnitude;
        Debug.Log($"[CustomerRagdoll] Impacto: {impact:F2} (minimo requerido: {minImpact}).");

        if (impact < minImpact)
        {
            Debug.Log("[CustomerRagdoll] Impacto insuficiente, no se activa el ragdoll.");
            return;
        }

        Debug.Log("[CustomerRagdoll] Activando ragdoll!");

        Vector3 force = thrownRb.linearVelocity * 2f;
        force.y = Mathf.Abs(force.y) + 3f;
        EnableRagdoll(force);
    }
}