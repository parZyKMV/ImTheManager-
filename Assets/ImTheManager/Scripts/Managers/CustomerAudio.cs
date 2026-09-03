using UnityEngine;

/// <summary>
/// Sonidos del cliente. Por ahora: el grito al ser lanzado durante Rage
/// Mode - se reproduce SOLO mientras esta en el aire (justo al lanzarlo) y
/// se corta apenas aterriza/se congela (ver CustomerRagdoll.FreezeRagdoll,
/// que llama StopScream() en el momento exacto que detecta el aterrizaje).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CustomerAudio : MonoBehaviour
{
    [Header("Grito al ser lanzado")]
    [Tooltip("Varios clips posibles - se elige uno al azar cada vez.")]
    [SerializeField] private AudioClip[] screamClips;

    [Header("Golpe / impacto")]
    [Tooltip("Se reproduce cuando un objeto lanzado golpea al cliente con suficiente fuerza (ver CustomerRagdoll.EnableRagdoll).")]
    [SerializeField] private AudioClip[] hitClips;

    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>Llamado por CustomerPickupable justo al lanzar (no al soltar sin fuerza).</summary>
    public void PlayScream()
    {
        if (screamClips == null || screamClips.Length == 0 || _audioSource == null) return;

        AudioClip clip = screamClips[Random.Range(0, screamClips.Length)];
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    /// <summary>Llamado por CustomerRagdoll apenas aterriza/se congela - corta el grito ahi mismo.</summary>
    public void StopScream()
    {
        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.Stop();
    }

    /// <summary>
    /// Llamado por CustomerRagdoll en el momento exacto del impacto (un
    /// objeto lanzado lo golpeo con suficiente fuerza). Usa PlayOneShot en
    /// vez de Play() para no cortar el grito si suenan casi al mismo tiempo.
    /// </summary>
    public void PlayHit()
    {
        if (hitClips == null || hitClips.Length == 0 || _audioSource == null) return;

        AudioClip clip = hitClips[Random.Range(0, hitClips.Length)];
        _audioSource.PlayOneShot(clip);
    }
}