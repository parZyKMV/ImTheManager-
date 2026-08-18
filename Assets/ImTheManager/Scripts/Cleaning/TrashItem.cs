using UnityEngine;

/// <summary>
/// Marca un pedazo de basura tirado en el piso por un cliente. El jugador
/// necesita estar cargando un TrashBin para poder recolectarla (ver
/// PlayerInteractor + TrashBin).
/// </summary>
public class TrashItem : MonoBehaviour
{
    public bool IsCollected { get; private set; } = false;

    void Start()
    {
        CleaningSystem.Instance?.RegisterMess(this);
    }

    public void Collect()
    {
        if (IsCollected) return;
        IsCollected = true;

        CleaningSystem.Instance?.ReportMessCleaned(this);

        Destroy(gameObject);
    }
}