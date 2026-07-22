using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dispara el cobro (FinishScanningAndCharge) con una tecla mientras el
/// jugador esta en modo caja. Se activa/desactiva junto con el resto de
/// RegisterModeController (igual que CounterItemDragController).
/// Solucion rapida para poder probar el flujo completo sin armar un boton de UI.
/// </summary>
public class RegisterChargeTrigger : MonoBehaviour
{
    [SerializeField] private Key chargeKey = Key.Enter;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[chargeKey].wasPressedThisFrame)
        {
            if (CashRegisterManager.Instance != null)
                CashRegisterManager.Instance.FinishScanningAndCharge();
            else
                Debug.LogWarning("[RegisterChargeTrigger] No hay CashRegisterManager en la escena.");
        }
    }
}
