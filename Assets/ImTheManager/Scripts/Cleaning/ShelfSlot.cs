using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable] public class IntEvent : UnityEvent<int> { }

/// <summary>
/// Datos y estado de un slot individual de estante: que producto vende,
/// cuanto stock tiene ahora, el maximo, y desde que cantidad se considera
/// "vacio". Tambien maneja la parte visual: instancia/destruye productos
/// fisicos en el estante para que coincidan con la cantidad actual.
/// </summary>
public class ShelfSlot : MonoBehaviour
{
    [Header("Producto")]
    [SerializeField] private ProductData productType;
    [SerializeField] private int maxQuantity = 10;
    [SerializeField] private int emptyThreshold = 0;

    [Header("Visual (opcional)")]
    [Tooltip("Puntos donde aparecen fisicamente los productos en el estante. Uno por unidad visible.")]
    [SerializeField] private Transform[] displayPoints;

    [Header("Eventos")]
    public IntEvent onStockChanged; // cantidad actual
    public UnityEvent onBecameEmpty;
    public UnityEvent onRestocked;
    public UnityEvent onDisordered; // un cliente dejo el estante desordenado
    public UnityEvent onTidied;     // el jugador lo ordeno

    public ProductData ProductType => productType;
    public int CurrentQuantity { get; private set; }
    public int MaxQuantity => maxQuantity;
    public bool IsEmpty => CurrentQuantity <= emptyThreshold;
    public bool IsFull => CurrentQuantity >= maxQuantity;
    public bool IsDisordered { get; private set; } = false;

    private readonly List<GameObject> _displayedInstances = new List<GameObject>();
    private bool _hasFiredEmptyEvent = false;

    // ===== DEBUG TESTER (temporal) ==========================================
    // Tecla T sobre este estante especifico: le quita una unidad, para poder
    // probar el flujo de vaciar/reabastecer sin depender de que un cliente
    // real camine hasta aca. Quitar cuando ya no haga falta.
    private InputAction _debugTakeOneAction;
    private InputAction _debugRestockAction;
    private InputAction _debugDisorderedAction;

    void Awake()
    {
        _debugTakeOneAction = new InputAction("DebugTakeOne", binding: "<Keyboard>/t");
        _debugTakeOneAction.performed += OnDebugTakeOnePerformed;
        _debugTakeOneAction.Enable();

        Debug.Log($"[ShelfSlot] '{name}': DebugTakeOne activo. Presiona 'T' para quitarle una unidad.");

        _debugRestockAction = new InputAction("DebugRestock", binding: "<Keyboard>/r");
        _debugRestockAction.performed += ctx => Restock();
        _debugRestockAction.Enable();

        _debugDisorderedAction = new InputAction("DebugDisordered", binding: "<Keyboard>/y");
        _debugDisorderedAction.performed += ctx => MakeDisordered();
        _debugDisorderedAction.Enable();
    }

    void OnDestroy()
    {
        if (_debugTakeOneAction == null) return;

        _debugTakeOneAction.performed -= OnDebugTakeOnePerformed;
        _debugTakeOneAction.Disable();
        _debugTakeOneAction.Dispose();
    }

    void OnDebugTakeOnePerformed(InputAction.CallbackContext ctx) => TakeOne();

    void Start()
    {
        CurrentQuantity = maxQuantity;
        RefreshVisuals();
    }

    /// <summary>Quita una unidad del estante (ej. un cliente la agarra). No hace nada si ya esta vacio.</summary>
    public void TakeOne()
    {
        if (CurrentQuantity <= 0) return;

        CurrentQuantity--;
        onStockChanged?.Invoke(CurrentQuantity);
        RefreshVisuals();

        if (IsEmpty && !_hasFiredEmptyEvent)
        {
            _hasFiredEmptyEvent = true;
            onBecameEmpty?.Invoke();
        }
    }

    /// <summary>Rellena el estante al maximo instantaneamente. Uso: debug/cheat, no gameplay normal.</summary>
    public void Restock()
    {
        CurrentQuantity = maxQuantity;
        _hasFiredEmptyEvent = false;

        onStockChanged?.Invoke(CurrentQuantity);
        onRestocked?.Invoke();
        RefreshVisuals();
    }

    /// <summary>
    /// Transfiere hasta 'amount' unidades al estante, respetando el maximo.
    /// Devuelve cuantas se agregaron realmente (puede ser menos de lo pedido
    /// si el estante ya casi esta lleno). Esto es lo que usa el reabastecimiento
    /// real desde una StockBox.
    /// </summary>
    public int AddStock(int amount)
    {
        int spaceAvailable = maxQuantity - CurrentQuantity;
        int amountToAdd = Mathf.Min(amount, spaceAvailable);
        if (amountToAdd <= 0) return 0;

        CurrentQuantity += amountToAdd;
        _hasFiredEmptyEvent = false;

        onStockChanged?.Invoke(CurrentQuantity);
        onRestocked?.Invoke();
        RefreshVisuals();

        return amountToAdd;
    }

    /// <summary>Un cliente dejo el estante desordenado (item incorrecto o rotado). No afecta el stock.</summary>
    public void MakeDisordered()
    {
        if (IsDisordered) return;

        IsDisordered = true;
        onDisordered?.Invoke();
        DisorderVisuals();

        CleaningSystem.Instance?.RegisterMess(this);
    }

    /// <summary>El jugador termino de ordenar el estante (ver ShelfTidyInteraction).</summary>
    public void Tidy()
    {
        if (!IsDisordered) return;

        IsDisordered = false;
        onTidied?.Invoke();
        TidyVisuals();

        CleaningSystem.Instance?.ReportMessCleaned(this);
    }

    // Rota cada producto visible al azar, simulando que quedaron chuecos/mal puestos.
    void DisorderVisuals()
    {
        foreach (var instance in _displayedInstances)
        {
            if (instance == null) continue;
            float randomY = Random.Range(0f, 360f);
            instance.transform.rotation = Quaternion.Euler(0f, randomY, 0f);
            
        }
    }

    // Devuelve cada producto visible a la rotacion original de su Display Point.
    void TidyVisuals()
    {
        int count = Mathf.Min(_displayedInstances.Count, displayPoints.Length);
        for (int i = 0; i < count; i++)
        {
            if (_displayedInstances[i] == null) continue;
            _displayedInstances[i].transform.rotation = displayPoints[i].rotation;
        }
    }

    // Instancia/destruye productos visuales para que coincidan con CurrentQuantity.
    void RefreshVisuals()
    {
        if (displayPoints == null || displayPoints.Length == 0 || productType == null || productType.prefab == null)
            return;

        int visibleCount = Mathf.Min(CurrentQuantity, displayPoints.Length);

        while (_displayedInstances.Count < visibleCount)
        {
            int index = _displayedInstances.Count;
            GameObject instance = Instantiate(productType.prefab, displayPoints[index].position, displayPoints[index].rotation, transform);
            MakeDisplayOnly(instance);
            _displayedInstances.Add(instance);
        }

        while (_displayedInstances.Count > visibleCount)
        {
            int lastIndex = _displayedInstances.Count - 1;
            if (_displayedInstances[lastIndex] != null)
                Destroy(_displayedInstances[lastIndex]);
            _displayedInstances.RemoveAt(lastIndex);
        }
    }

    // Los productos que se ven en el estante son puramente decorativos:
    // sin fisica real y sin poder recogerlos directo (eso rompería el loop
    // de comprar/reabastecer). El mismo prefab se sigue usando para que se
    // vea identico, solo se desactivan las partes "funcionales".
    void MakeDisplayOnly(GameObject instance)
    {
        var pickupable = instance.GetComponent<Pickupable>();
        if (pickupable != null)
            pickupable.enabled = false;

        var rigidbody = instance.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        var collider = instance.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
    }
}