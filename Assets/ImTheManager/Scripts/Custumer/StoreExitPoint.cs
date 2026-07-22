using UnityEngine;

/// <summary>
/// Marca el punto de salida de la tienda. Ponelo en un GameObject cerca
/// de la puerta. Singleton simple: asume una sola salida en la escena.
/// </summary>
public class StoreExitPoint : MonoBehaviour
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
