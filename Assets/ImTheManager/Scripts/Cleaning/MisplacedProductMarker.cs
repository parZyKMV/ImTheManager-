using UnityEngine;

/// <summary>
/// Marcador liviano agregado por CreateMessAction a un producto instanciado
/// como "fuera de lugar". Solo existe para que CleaningSystem sepa cuando
/// se creo y cuando se limpio (devolvio a su estante) - no tiene ninguna
/// logica de interaccion, eso lo maneja PlayerInteractor directo via ScannableProduct.
/// </summary>
public class MisplacedProductMarker : MonoBehaviour
{
    void Start()
    {
        CleaningSystem.Instance?.RegisterMess(this);
    }

    /// <summary>Llamado por PlayerInteractor justo antes de destruir el objeto al devolverlo a su estante.</summary>
    public void MarkCleaned()
    {
        CleaningSystem.Instance?.ReportMessCleaned(this);
    }
}
