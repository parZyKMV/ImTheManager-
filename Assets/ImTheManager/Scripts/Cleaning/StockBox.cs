using UnityEngine;

/// <summary>
/// Va junto con Pickupable en las cajas de la bodega. Indica que producto
/// contiene y cuantas unidades quedan. PlayerInteractor la usa para
/// reabastecer estantes al cargarla y mirar un ShelfSlot vacio.
/// </summary>
[RequireComponent(typeof(Pickupable))]
public class StockBox : MonoBehaviour
{
    [SerializeField] private ProductData productType;
    [SerializeField] private int quantity = 10;

    public ProductData ProductType => productType;
    public int Quantity => quantity;

    /// <summary>Quita hasta 'amount' unidades. Devuelve cuantas se quitaron realmente.
    /// Si la caja queda vacia, se destruye sola.</summary>
    public int RemoveUnits(int amount)
    {
        int actualAmount = Mathf.Min(amount, quantity);
        quantity -= actualAmount;

        if (quantity <= 0)
            Destroy(gameObject);

        return actualAmount;
    }
}
