using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Comandos y funciones custom de Yarn Spinner que conectan los archivos
/// .yarn con el estado real del juego. Version actual: solo SanityMeter.
/// Puede vivir en cualquier GameObject de la escena (los comandos son
/// static, per la doc de Yarn Spinner esto es valido incluso sin MonoBehaviour,
/// pero lo dejamos como MonoBehaviour por si mas adelante agregamos comandos
/// de instancia como shake_camera con una referencia real a la camara).
/// </summary>
public class YarnCommands : MonoBehaviour
{
    [Header("Camera shake (opcional, placeholder)")]
    [SerializeField] private float shakeIntensity = 0.15f;
    [SerializeField] private float shakeDuration = 0.3f;

    /// <summary>
    /// <<add_stress N>> - le agrega N de estres al SanityMeter. Usar numeros
    /// negativos para calmar (ej. cuando el jugador maneja bien la situacion).
    /// </summary>
    [YarnCommand("add_stress")]
    public static void AddStress(float amount)
    {
        if (SanityMeter.Instance == null)
        {
            Debug.LogWarning("[YarnCommands] No hay SanityMeter en la escena, se ignora add_stress.");
            return;
        }

        SanityMeter.Instance.AddStress(amount, "Karen");
    }

    /// <summary>
    /// get_sanity() - funcion que devuelve el estres actual (0-100), para
    /// usar en condicionales dentro del .yarn: <<if get_sanity() >= 50>>
    /// </summary>
    [YarnFunction("get_sanity")]
    public static float GetSanity()
    {
        return SanityMeter.Instance != null ? SanityMeter.Instance.CurrentStress : 0f;
    }

    /// <summary>
    /// <<shake_camera>> - placeholder. Reemplazar el cuerpo cuando se arme
    /// un sistema de camera shake real (ej. Cinemachine Impulse).
    /// </summary>
    [YarnCommand("shake_camera")]
    public void ShakeCamera()
    {
        Debug.Log($"[YarnCommands] shake_camera (placeholder) - intensidad {shakeIntensity}, duracion {shakeDuration}. TODO: conectar a Cinemachine Impulse o similar.");
    }
}
