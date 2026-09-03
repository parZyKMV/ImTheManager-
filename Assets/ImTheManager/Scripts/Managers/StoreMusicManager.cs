using System.Collections;
using UnityEngine;

/// <summary>
/// Musica de fondo de la tienda: reproduce una playlist en orden, y al
/// terminar una cancion pasa automaticamente a la siguiente (vuelve al
/// principio de la lista despues de la ultima). Reusable/simple: agrega
/// canciones al arreglo desde el Inspector, sin tocar codigo.
/// </summary>
public class StoreMusicManager : MonoBehaviour
{
    public static StoreMusicManager Instance { get; private set; }

    [Header("Playlist")]
    [Tooltip("Canciones en orden. Se reproducen una tras otra, volviendo al principio al llegar al final.")]
    [SerializeField] private AudioClip[] playlist;
    [SerializeField] private bool shufflePlaylist = false;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;

    private int _currentTrackIndex = 0;
    private Coroutine _playbackRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        if (playlist == null || playlist.Length == 0)
        {
            Debug.LogWarning("[StoreMusicManager] No hay canciones en la playlist.");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[StoreMusicManager] No hay AudioSource asignado.");
            return;
        }

        audioSource.volume = volume;
        audioSource.loop = false; // el "loop" de playlist lo manejamos nosotros, cancion por cancion

        if (shufflePlaylist)
            ShufflePlaylist();

        _currentTrackIndex = 0;
        PlayCurrentTrack();
    }

    void PlayCurrentTrack()
    {
        if (_playbackRoutine != null)
            StopCoroutine(_playbackRoutine);

        AudioClip clip = playlist[_currentTrackIndex];

        if (clip == null)
        {
            Debug.LogWarning($"[StoreMusicManager] El slot {_currentTrackIndex} de la playlist esta vacio, saltando a la siguiente.");
            PlayNextTrack();
            return;
        }

        audioSource.clip = clip;
        audioSource.Play();

        _playbackRoutine = StartCoroutine(WaitAndPlayNext(clip.length));
    }

    IEnumerator WaitAndPlayNext(float trackLength)
    {
        yield return new WaitForSeconds(trackLength);
        PlayNextTrack();
    }

    /// <summary>Pasa a la siguiente cancion de la lista (vuelve al principio si era la ultima).</summary>
    public void PlayNextTrack()
    {
        _currentTrackIndex = (_currentTrackIndex + 1) % playlist.Length;

        // Si dio toda la vuelta y esta en modo shuffle, volvemos a barajar
        // para que la proxima vuelta no repita el mismo orden.
        if (_currentTrackIndex == 0 && shufflePlaylist)
            ShufflePlaylist();

        PlayCurrentTrack();
    }

    /// <summary>Vuelve a la cancion anterior de la lista.</summary>
    public void PlayPreviousTrack()
    {
        _currentTrackIndex = (_currentTrackIndex - 1 + playlist.Length) % playlist.Length;
        PlayCurrentTrack();
    }

    void ShufflePlaylist()
    {
        for (int i = playlist.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (playlist[i], playlist[j]) = (playlist[j], playlist[i]);
        }
    }
}
