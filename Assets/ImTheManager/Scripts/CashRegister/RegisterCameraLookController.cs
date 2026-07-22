using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Controla el giro (izquierda/derecha) e inclinacion (arriba/abajo) de la
/// camara de la caja usando flechas o Q/E, dejando el mouse 100% libre
/// para el point-and-click de CounterItemDragController.
///
/// Requiere un componente CinemachinePanTilt en la Register Camera. Por
/// defecto ese componente lo maneja un Input Axis Controller (mouse), pero
/// tambien acepta ser manejado directamente desde script, que es lo que
/// hacemos aqui.
/// </summary>
public class RegisterCameraLookController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CinemachineCamera registerCamera;

    [Header("Velocidad de giro (grados por segundo)")]
    [SerializeField] private float panSpeed = 90f;  // izquierda / derecha
    [SerializeField] private float tiltSpeed = 60f; // arriba / abajo

    private CinemachinePanTilt _panTilt;

    void Awake()
    {
        if (registerCamera != null)
            _panTilt = registerCamera.GetComponent<CinemachinePanTilt>();

        if (_panTilt == null)
            Debug.LogWarning("[RegisterCameraLookController] No se encontro CinemachinePanTilt en la Register Camera. Agregalo como componente de Aim.");
    }

    void Update()
    {
        if (_panTilt == null || Keyboard.current == null) return;

        float panInput = 0f;
        float tiltInput = 0f;

        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.qKey.isPressed)
            panInput -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.eKey.isPressed)
            panInput += 1f;

        if (Keyboard.current.upArrowKey.isPressed)
            tiltInput -= 1f; // convencion de Cinemachine: tilt negativo = mirar hacia arriba
        if (Keyboard.current.downArrowKey.isPressed)
            tiltInput += 1f;

        _panTilt.PanAxis.Value += panInput * panSpeed * Time.deltaTime;
        _panTilt.TiltAxis.Value += tiltInput * tiltSpeed * Time.deltaTime;

        // Al asignar Value directamente, Cinemachine NO clampea solo contra
        // el Range configurado en el Inspector - hay que hacerlo a mano.
        ClampOrWrap(ref _panTilt.PanAxis);
        ClampOrWrap(ref _panTilt.TiltAxis);
    }

    // Respeta el Range del eje. Si el eje tiene Wrap activado (para poder
    // girar 360 grados sin trabarse en un limite), envuelve en vez de cortar.
    void ClampOrWrap(ref InputAxis axis)
    {
        if (axis.Wrap)
        {
            float range = axis.Range.y - axis.Range.x;
            if (range <= 0f) return;

            while (axis.Value < axis.Range.x) axis.Value += range;
            while (axis.Value > axis.Range.y) axis.Value -= range;
        }
        else
        {
            axis.Value = Mathf.Clamp(axis.Value, axis.Range.x, axis.Range.y);
        }
    }

    /// <summary>
    /// Vuelve la mira al centro. Llamalo al entrar al modo caja para que
    /// siempre arranque con el mismo encuadre, sin importar como quedo
    /// la ultima vez.
    /// </summary>
    public void ResetLook()
    {
        if (_panTilt == null) return;

        _panTilt.PanAxis.Value = 0f;
        _panTilt.TiltAxis.Value = 0f;
    }
}
