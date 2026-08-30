using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Maneja la fila frente a la caja: asigna posiciones a los clientes en orden
/// de llegada y sabe quien esta al frente. Version simple v1: fila de tamano
/// fijo (un slot por posicion), sin integrar todavia con CashRegisterManager
/// (eso se conecta cuando armemos la atencion real al cliente).
/// </summary>
public class CustomerQueueManager : MonoBehaviour
{
    public static CustomerQueueManager Instance { get; private set; }

    [Header("Posiciones de la fila, en orden (0 = mas cerca de la caja)")]
    [SerializeField] private Transform[] queueSlots;

    private readonly List<Transform> _customersInQueue = new List<Transform>();

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

    /// <summary>
    /// Intenta anotar al cliente en la fila. Si hay lugar, devuelve true
    /// y el Transform del slot que le toca.
    /// </summary>
    public bool TryJoinQueue(Transform customer, out Transform assignedSlot)
    {
        assignedSlot = null;

        if (queueSlots == null || queueSlots.Length == 0)
        {
            Debug.LogWarning("[CustomerQueueManager] No hay queue slots configurados.");
            return false;
        }

        // Evita duplicados: si el cliente ya esta en la fila, le devolvemos
        // su lugar actual en vez de agregarlo de nuevo.
        int existingIndex = _customersInQueue.IndexOf(customer);
        if (existingIndex != -1)
        {
            assignedSlot = queueSlots[existingIndex];
            return true;
        }

        if (_customersInQueue.Count >= queueSlots.Length)
            return false; // fila llena

        _customersInQueue.Add(customer);
        assignedSlot = queueSlots[_customersInQueue.Count - 1];
        return true;
    }

    public bool IsAtFrontOfQueue(Transform customer)
    {
        return _customersInQueue.Count > 0 && _customersInQueue[0] == customer;
    }

    /// <summary>El cliente que esta actualmente al frente (siendo atendido), o null si la fila esta vacia.</summary>
    public Transform CustomerAtFront => _customersInQueue.Count > 0 ? _customersInQueue[0] : null;

    /// <summary>Devuelve el slot de fila asignado a este cliente, o null si no esta en la fila.</summary>
    public Transform GetSlotForCustomer(Transform customer)
    {
        int index = _customersInQueue.IndexOf(customer);
        if (index == -1 || queueSlots == null || index >= queueSlots.Length)
            return null;

        return queueSlots[index];
    }

    /// <summary>
    /// Llamar cuando el cliente al frente termina de ser atendido y se va.
    /// TODO: conectar esto a CashRegisterManager.onTransactionComplete
    /// cuando armemos la atencion real del cliente en la caja.
    /// </summary>
    public void LeaveQueue(Transform customer)
    {
        _customersInQueue.Remove(customer);
        ShiftQueueForward();
    }

    // Reacomoda a todos los que quedan un lugar hacia adelante en la fila.
    void ShiftQueueForward()
    {
        for (int i = 0; i < _customersInQueue.Count; i++)
        {
            var agent = _customersInQueue[i].GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.SetDestination(queueSlots[i].position);
        }
    }

    /// <summary>
    /// Destruye a TODOS los clientes activos en la escena (esten o no en
    /// la fila - tambien saca a los que estan comprando/explorando) y
    /// vacia la fila. Uso: transicion entre dias, para que el dia nuevo
    /// no arranque con clientes de ayer parados por ahi. Los desordenes
    /// (basura, estantes, productos fuera de lugar) NO se tocan aca -
    /// si no los limpiaste, siguen ahi manana.
    /// </summary>
    public void DespawnAllCustomers()
    {
        var allCustomers = Object.FindObjectsByType<CustomerLifecycle>(FindObjectsSortMode.None);

        foreach (var customer in allCustomers)
        {
            if (customer != null)
                Object.Destroy(customer.gameObject);
        }

        _customersInQueue.Clear();
    }
}