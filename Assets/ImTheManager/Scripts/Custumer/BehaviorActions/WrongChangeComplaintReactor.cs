using UnityEngine;

/// <summary>
/// Reacciona cuando el jugador le da el cambio incorrecto a ESTE cliente
/// mientras esta siendo atendido en la caja. A diferencia de EmptyShelf
/// (que se evalua como parte de la secuencia del Behavior Graph via
/// ShouldComplainCondition/ComplainAction), WrongChange es un evento
/// asincrono que puede pasar en cualquier momento mientras el cliente
/// espera en la caja - por eso sigue siendo reactivo, no un nodo de grafo.
/// </summary>
[RequireComponent(typeof(CustomerLifecycle))]
public class WrongChangeComplaintReactor : MonoBehaviour
{
    private CustomerLifecycle _lifecycle;
    private CustomerProfile _profile;

    void Awake()
    {
        _lifecycle = GetComponent<CustomerLifecycle>();
    }

    void Start()
    {
        _profile = _lifecycle.Profile;

        if (_profile == null)
        {
            Debug.LogWarning($"[WrongChangeComplaintReactor] {name}: no tiene CustomerProfile, se desactiva.");
            enabled = false;
            return;
        }

        if (!ContainsTrigger(ComplaintTrigger.WrongChange))
        {
            enabled = false;
            return;
        }

        if (CashRegisterManager.Instance != null)
            CashRegisterManager.Instance.onChangeResult.AddListener(HandleChangeResult);
    }

    void OnDestroy()
    {
        if (CashRegisterManager.Instance != null)
            CashRegisterManager.Instance.onChangeResult.RemoveListener(HandleChangeResult);
    }

    void HandleChangeResult(bool wasCorrect)
    {
        if (wasCorrect) return;

        // CashRegisterManager es singleton: este evento se dispara para
        // CUALQUIER transaccion, asi que solo reaccionamos si somos el
        // cliente que esta siendo atendido ahora mismo.
        if (CustomerQueueManager.Instance == null) return;
        if (CustomerQueueManager.Instance.CustomerAtFront != transform) return;

        if (Random.value > _profile.complaintChance) return;

        Debug.Log($"[WrongChangeComplaintReactor] {name} se queja por cambio incorrecto.");

        if (SanityMeter.Instance != null)
            SanityMeter.Instance.AddStress(_profile.complaintStressAmount, "Complaint:WrongChange");
    }

    bool ContainsTrigger(ComplaintTrigger trigger)
    {
        if (_profile?.complaintTriggers == null) return false;

        foreach (var t in _profile.complaintTriggers)
            if (t == trigger) return true;

        return false;
    }
}
