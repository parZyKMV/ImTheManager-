using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>
/// Conecta la variable $sanity de Yarn Spinner con la UI real del Sanity Meter.
/// Coloca este script en el mismo GameObject que tu Dialogue Runner (o cerca),
/// y asigna las referencias en el Inspector.
/// </summary>
public class SanityMeterController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private Slider sanitySlider; // rango 0-1 en el Inspector del Slider

    [Header("Config")]
    [SerializeField] private float maxSanity = 100f;

    /// <summary>
    /// Llamado desde el .yarn con <<refresh_sanity>> cada vez que $sanity cambia.
    /// Lee el valor actual desde el Variable Storage del Dialogue Runner
    /// y actualiza el Slider de la UI.
    /// </summary>
    [YarnCommand("refresh_sanity")]
    public void RefreshSanityUI()
    {
        if (dialogueRunner == null)
        {
            Debug.LogWarning("[SanityMeterController] No hay Dialogue Runner asignado.");
            return;
        }

        if (dialogueRunner.VariableStorage.TryGetValue<float>("$sanity", out float sanityValue))
        {
            if (sanitySlider != null)
                sanitySlider.value = Mathf.Clamp01(sanityValue / maxSanity);

            Debug.Log($"[SanityMeterController] Sanity actual: {sanityValue}/{maxSanity}");
        }
        else
        {
            Debug.LogWarning("[SanityMeterController] No se pudo leer la variable $sanity.");
        }
    }

    /// <summary>
    /// Llamado desde el .yarn con <<game_over>> cuando la cordura llega al limite.
    /// Reemplaza el contenido con tu logica real (cargar escena de Game Over, etc.)
    /// </summary>
    [YarnCommand("game_over")]
    public void TriggerGameOver()
    {
        Debug.Log("[SanityMeterController] GAME OVER: el jugador perdio la cordura frente a Karen.");
        // Ejemplo: SceneManager.LoadScene("GameOverScene");
    }

    /// <summary>
    /// Llamado desde el .yarn con <<karen_leave>> cuando el encuentro termina bien (o regular).
    /// Aqui puedes desactivar al NPC, reanudar el control del jugador, etc.
    /// </summary>
    [YarnCommand("karen_leave")]
    public void KarenLeave()
    {
        Debug.Log("[SanityMeterController] Karen se retira de la tienda.");
        // Ejemplo: gameObject de Karen -> SetActive(false)
        // Ejemplo: reactivar el RPS_ThirdPersonController del jugador
    }
}
