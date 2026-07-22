using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Detecta si hay una pared justo detras del jugador, en linea con la camara,
/// y si es asi, sube gradualmente el angulo vertical del Orbital Follow
/// para evitar que la camara quede aplastada contra la pared (y el jitter que eso causa).
/// Colocar en cualquier GameObject de la escena (ej. el Player o un Manager).
/// </summary>
public class CameraWallProximityBoost : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineCamera freeLookCamera; // arrastra tu Cinemachine Camera (FreeLook) aqui
    [SerializeField] private Transform player;                 // el transform del jugador (el target de la camara)

    [Header("Deteccion de pared")]
    [SerializeField] private float wallCheckDistance = 1.5f; // que tan cerca del jugador cuenta como "pegado"
    [SerializeField] private LayerMask wallLayer;             // usa la misma layer "Environment" del Deoccluder

    [Header("Ajuste vertical")]
    [SerializeField] private float raisedVerticalAngle = 60f; // angulo objetivo (grados) cuando esta pegado a pared
    [SerializeField] private float raiseSpeed = 90f;           // grados por segundo al subir

    private CinemachineOrbitalFollow _orbitalFollow;

    void Awake()
    {
        if (freeLookCamera != null)
            _orbitalFollow = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();

        if (_orbitalFollow == null)
            Debug.LogWarning("[CameraWallProximityBoost] No se encontro CinemachineOrbitalFollow en la camara asignada.");
    }

    // LateUpdate para actuar despues de que el jugador se movio y la camara leyo su input,
    // pero antes de que CinemachineBrain aplique la posicion final del frame.
    void LateUpdate()
    {
        if (_orbitalFollow == null || player == null || freeLookCamera == null) return;

        if (IsWallBehindPlayer())
        {
            // Empujamos el eje vertical hacia arriba, gradualmente.
            // No tocamos nada si NO hay pared: asi el jugador recupera
            // control normal del mouse apenas se aleja.
            _orbitalFollow.VerticalAxis.Value = Mathf.MoveTowards(
                _orbitalFollow.VerticalAxis.Value,
                raisedVerticalAngle,
                raiseSpeed * Time.deltaTime
            );
        }
    }

    // Lanza un rayo desde el jugador, en la misma direccion que va de la camara hacia el jugador
    // (o sea, "continuando" esa linea hacia atras). Si pega con una pared muy cerca,
    // significa que la camara esta a punto de quedar aplastada contra ella.
    bool IsWallBehindPlayer()
    {
        Vector3 cameraToPlayer = (player.position - freeLookCamera.transform.position).normalized;
        Debug.DrawRay(player.position, cameraToPlayer * wallCheckDistance, Color.yellow);
        //Debug.Log($"[CameraWallProximityBoost] Raycast desde {player.position} hacia {cameraToPlayer * wallCheckDistance}");
        Vector3 origin = player.position + Vector3.up * 1f; // sube el origen para no chocar con el suelo

        return Physics.Raycast(origin, cameraToPlayer, wallCheckDistance, wallLayer);
    }

    void OnDrawGizmosSelected()
    {
        if (player == null || freeLookCamera == null) return;

        Vector3 cameraToPlayer = (player.position - freeLookCamera.transform.position).normalized;
        Vector3 origin = player.position + Vector3.up * 1f;

        Gizmos.color = IsWallBehindPlayer() ? Color.red : Color.cyan;
        Gizmos.DrawLine(origin, origin + cameraToPlayer * wallCheckDistance);
    }
}
