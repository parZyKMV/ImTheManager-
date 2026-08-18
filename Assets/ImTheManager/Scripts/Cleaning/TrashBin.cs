using UnityEngine;

/// <summary>
/// Va junto con Pickupable en el bote de basura. Mientras el jugador lo
/// carga, puede recolectar TrashItems con Interact (ver PlayerInteractor).
/// Capacidad infinita por ahora - simplificacion consciente para la v1.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class TrashBin : MonoBehaviour
{
    public int CollectedCount { get; private set; } = 0;

    /// <summary>Llamado por PlayerInteractor cuando el jugador recolecta un TrashItem.</summary>
    public void CollectTrash(TrashItem trash)
    {
        if (trash == null) return;

        trash.Collect();
        CollectedCount++;
    }
}
