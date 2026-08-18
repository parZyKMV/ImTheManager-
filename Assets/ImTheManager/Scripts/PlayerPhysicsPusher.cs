using UnityEngine;

/// <summary>
/// El CharacterController de Unity, por diseno, NO empuja los Rigidbody con
/// los que choca (a diferencia de un Rigidbody normal). Este script
/// intercepta esas colisiones y les aplica fuerza manualmente - necesario
/// para poder empujar puertas con HingeJoint, cajas sueltas en el piso, etc.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerPhysicsPusher : MonoBehaviour
{
    [SerializeField] private float pushForce = 2f;
    [Tooltip("Ignora objetos mas pesados que esto (ej. props grandes) para que no se sientan livianos al empujarlos sin querer.")]
    [SerializeField] private float maxPushableMass = 20f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.rigidbody;

        // Nada que empujar, o esta kinematico (ej. los productos de adorno
        // en los estantes, que a proposito no deben moverse), o pesa demasiado.
        if (rb == null || rb.isKinematic || rb.mass > maxPushableMass) return;

        // Solo empuja en el plano horizontal - evita "levantar" cosas raro
        // al caminar sobre ellas o rozarlas por abajo/arriba.
        Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);

        rb.AddForceAtPosition(pushDirection * pushForce, hit.point, ForceMode.Impulse);
    }
}
