using UnityEngine;

/// <summary>
/// Componente que PlayerInteractor detecta para reabastecer un estante.
/// Va en el mismo GameObject que ShelfSlot (o lo referencia si esta en otro).
/// Requiere un Collider (no trigger) para que el SphereCast de PlayerInteractor
/// lo detecte, igual que Pickupable.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShelfRestockSystem : MonoBehaviour
{
    [SerializeField] private ShelfSlot shelfSlot;

    public bool CanRestock => shelfSlot != null && !shelfSlot.IsFull && !shelfSlot.IsDisordered;
    public bool NeedsTidying => shelfSlot != null && shelfSlot.IsDisordered;
    public ShelfSlot Slot => shelfSlot;

    void Awake()
    {
        if (shelfSlot == null)
            shelfSlot = GetComponent<ShelfSlot>();

        if (shelfSlot == null)
            Debug.LogWarning("[ShelfRestockSystem] No se encontro un ShelfSlot asignado ni en este GameObject.");
    }

    /// <summary>
    /// Recibe un producto suelto (Pickupable con ScannableProduct) que el
    /// jugador esta devolviendo directo al estante. Devuelve true si se
    /// acepto (coincide el producto y hay espacio).
    /// </summary>
    public bool ReturnProduct(ScannableProduct product)
    {
        if (shelfSlot == null || product == null) return false;
        if (product.ProductData != shelfSlot.ProductType) return false;

        int added = shelfSlot.AddStock(1);
        return added > 0;
    }

    /// <summary>
    /// Reabastece usando el contenido de una StockBox real. Valida que el
    /// producto coincida con el de este estante. Devuelve cuantas unidades
    /// se transfirieron realmente.
    /// </summary>
    public int RestockFromBox(StockBox box)
    {
        if (shelfSlot == null || box == null) return 0;

        if (box.ProductType != shelfSlot.ProductType)
        {
            Debug.LogWarning($"[ShelfRestockSystem] La caja tiene '{box.ProductType?.productName}' pero este estante es de '{shelfSlot.ProductType?.productName}'. No coinciden.");
            return 0;
        }

        int spaceAvailable = shelfSlot.MaxQuantity - shelfSlot.CurrentQuantity;
        int amountToTransfer = Mathf.Min(box.Quantity, spaceAvailable);
        if (amountToTransfer <= 0) return 0;

        int actuallyRemoved = box.RemoveUnits(amountToTransfer);
        shelfSlot.AddStock(actuallyRemoved);

        return actuallyRemoved;
    }
}