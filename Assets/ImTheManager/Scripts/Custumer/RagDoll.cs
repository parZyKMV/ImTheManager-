using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    [SerializeField] float minImpact = 3f;
    [SerializeField] ParticleSystem hitEffect;

    Rigidbody[] ragdollBodies;
    Collider[] ragdollColliders;
    Animator animator;
    bool isRagdoll = false;

    Collider mainCollider;

    void Start()
    {
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        animator = GetComponent<Animator>();
        mainCollider = GetComponent<Collider>(); // 👈 guarda el collider raíz
        DisableRagdoll();
    }

    void DisableRagdoll()
    {
        foreach (var rb in ragdollBodies)
            rb.isKinematic = true;
        foreach (var col in ragdollColliders)
            col.enabled = false;

        // mantiene el collider raíz activo para detectar el truck
        if (mainCollider != null)
            mainCollider.enabled = true;

        if (animator != null)
            animator.enabled = true;
    }

    void EnableRagdoll(Vector3 force)
    {
        if (isRagdoll) return;
        isRagdoll = true;

        // desactiva el collider raíz ya no lo necesitamos
        if (mainCollider != null)
            mainCollider.enabled = false;

        foreach (var rb in ragdollBodies)
            rb.isKinematic = false;
        foreach (var col in ragdollColliders)
            col.enabled = true;
        if (animator != null)
            animator.enabled = false;

        Rigidbody chest = ragdollBodies[0];
        chest.AddForce(force, ForceMode.Impulse);

        if (hitEffect != null)
        {
            ParticleSystem instance = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(instance.gameObject, 3f);
        }

        Destroy(gameObject, 5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pickuable")) return;
        Debug.Log("Ragdoll: colision con Pickuable: " + other.name);
        // busca el Rigidbody en el objeto o en cualquier padre
        Rigidbody truckRb = other.GetComponentInParent<Rigidbody>();
        Debug.Log("TruckRb: " + (truckRb == null ? "NULL" : truckRb.name));
        if (truckRb == null) return;

        float impact = truckRb.linearVelocity.magnitude;
        Debug.Log("Impacto: " + impact + " minImpact: " + minImpact);
        if (impact < minImpact) return;

        Debug.Log("Activando ragdoll!");
        Vector3 force = truckRb.linearVelocity * 2f;
        force.y = Mathf.Abs(force.y) + 3f;
        EnableRagdoll(force);
    }
}