using UnityEngine;

/// <summary>
/// Marca la posicion del jugador para que los clientes lo puedan buscar
/// (ej. Karen caminando hasta el jugador para quejarse). Ponelo en el
/// GameObject raiz del Player. Singleton simple: un solo jugador en la escena.
/// </summary>
public class PlayerReference : MonoBehaviour
{
    public static Transform Instance { get; private set; }

    void Awake()
    {
        Instance = transform;
    }

    void OnDestroy()
    {
        if (Instance == transform)
            Instance = null;
    }
}
