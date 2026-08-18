using UnityEngine;

/// <summary>
/// Va en cada prop que se puede tirar/empujar durante Rage Mode. Detecta
/// cuando el prop se "cayo" (se inclino mas de un umbral) y le avisa a
/// RageModeController para el conteo de dano del turno.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class KnockableProp : MonoBehaviour
{
    [SerializeField] private float tiltThresholdDegrees = 45f;

    private Rigidbody _rigidbody;
    private bool _hasReported = false;

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (RageModeController.Instance != null)
            RageModeController.Instance.onRageModeStarted.AddListener(ResetReportFlag);
    }

    void OnDisable()
    {
        if (RageModeController.Instance != null)
            RageModeController.Instance.onRageModeStarted.RemoveListener(ResetReportFlag);
    }

    void Update()
    {
        if (_hasReported || RageModeController.Instance == null || !RageModeController.Instance.IsActive)
            return;

        float tilt = Vector3.Angle(transform.up, Vector3.up);
        if (tilt >= tiltThresholdDegrees)
        {
            _hasReported = true;
            RageModeController.Instance.ReportPropKnocked(_rigidbody);
        }
    }

    // Se resetea cada vez que arranca un Rage Mode nuevo, para poder
    // contar el mismo prop otra vez en un turno futuro.
    void ResetReportFlag()
    {
        _hasReported = false;
    }
}
