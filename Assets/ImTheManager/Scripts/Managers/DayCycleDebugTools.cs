using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Herramienta SOLO de debug: teclas rapidas para probar el ciclo de dias
/// sin esperar los 12 minutos reales completos. Quitar o desactivar antes
/// de una build final.
///
/// F9  -> termina el turno actual de inmediato (como si se acabara el tiempo)
/// F10 -> avanza directo al siguiente dia (salta la pantalla de resultados)
/// F11 -> fuerza Rage Mode ya mismo, sin esperar a que el SanityMeter llegue al maximo
/// </summary>
public class DayCycleDebugTools : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            if (DayCycleManager.Instance != null)
            {
                DayCycleManager.Instance.EndDay();
                Debug.Log("[DayCycleDebugTools] Turno terminado manualmente (F9).");
            }
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            if (DayCycleManager.Instance != null)
            {
                DayCycleManager.Instance.AdvanceToNextDay();
                Debug.Log("[DayCycleDebugTools] Avanzo al siguiente dia (F10).");
            }
        }

        if (Keyboard.current.f11Key.wasPressedThisFrame)
        {
            if (RageModeController.Instance != null)
            {
                RageModeController.Instance.StartRageMode();
                Debug.Log("[DayCycleDebugTools] Rage Mode forzado (F11).");
            }
        }
    }
}