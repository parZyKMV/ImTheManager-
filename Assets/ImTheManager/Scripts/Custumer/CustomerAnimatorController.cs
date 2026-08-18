using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Alimenta el parametro "Speed" del Animator del cliente segun la
/// velocidad real del NavMeshAgent, para que el Blend Tree de Idle/Caminar
/// reaccione solo. Respeta CustomerRagdoll: si el Animator esta desactivado
/// (durante el golpe/caida), no hace nada.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class CustomerAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator animator; // si lo dejas vacio, busca uno en los hijos

    private NavMeshAgent _navAgent;

    void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null || !animator.enabled) return; // ej. durante el ragdoll
        if (_navAgent == null || !_navAgent.enabled) return; // ej. tambien durante el ragdoll

        float speed = _navAgent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
    }
}
