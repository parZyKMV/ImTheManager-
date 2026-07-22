using UnityEngine;

[CreateAssetMenu(fileName = "NewProduct", menuName = "I'm The Manager/Product")]
public class ProductData : ScriptableObject
{
    [Header("Info del producto")]
    public string productName = "Producto sin nombre";
    public Sprite icon;

    [Header("Precio")]
    [Min(0f)] public float price = 1.0f;

    [Header("Prefab físico")]
    [Tooltip("El prefab con Rigidbody + Collider + ScannableProduct que representa a este producto en el mundo.")]
    public GameObject prefab;
}