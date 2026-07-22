using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;

/// <summary>
/// Va en un Collider (Is Trigger = true) que cubre la zona de atencion de la caja.
/// Cuando el jugador entra, sube la prioridad de la camara de primera persona
/// para que Cinemachine haga un blend automatico hacia ella. Al salir, vuelve
/// a la camara de tercera persona (FreeLook).
/// </summary>
[RequireComponent(typeof(Collider))]
public class RegisterCameraZone : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineCamera registerCamera; // la RegisterFirstPersonCamera
    [SerializeField] private string playerTag = "Player";

    [Header("Prioridad")]
    [Tooltip("Debe ser mayor a la Priority de tu FreeLook para que gane el control.")]
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;

    [Header("Eventos (opcional)")]
    [Tooltip("Util para bloquear movimiento, mostrar UI de 'Presiona E para irte', etc.")]
    public UnityEvent onPlayerEnteredRegister;
    public UnityEvent onPlayerLeftRegister;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[RegisterCameraZone] El collider deberia ser Trigger. Corrigiendo automaticamente.");
            col.isTrigger = true;
        }

        SetCameraPriority(inactivePriority);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        SetCameraPriority(activePriority);
        onPlayerEnteredRegister?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        SetCameraPriority(inactivePriority);
        onPlayerLeftRegister?.Invoke();
    }

    void SetCameraPriority(int value)
    {
        if (registerCamera == null)
        {
            Debug.LogWarning("[RegisterCameraZone] No hay Register Camera asignada.");
            return;
        }

        // En Cinemachine 3.x, Priority es un struct (PrioritySettings), no un int directo.
        registerCamera.Priority = new PrioritySettings { Enabled = true, Value = value };
    }

    /// <summary>
    /// Fuerza la salida del modo caja. Necesario porque, al quedar el jugador
    /// congelado durante la atencion, OnTriggerExit ya no se dispara solo:
    /// la salida ahora es una decision explicita (boton "Terminar turno").
    /// </summary>
    public void ForceExitRegisterView()
    {
        SetCameraPriority(inactivePriority);
        onPlayerLeftRegister?.Invoke();
    }
}
