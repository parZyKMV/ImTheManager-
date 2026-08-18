using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

/// <summary>
/// Nodo Action: el cliente interrumpe lo que esta haciendo, camina hasta el
/// jugador, y al llegar arranca el nodo de Yarn configurado en su
/// CustomerProfile (dialogueStartNode). Se queda en Running hasta que el
/// dialogo termina. Reusable para cualquier arquetipo que necesite
/// "buscar al jugador y hablarle" - Karen quejandose, un cliente
/// preguntando algo tonto, etc. La diferencia es 100% que .yarn se asigna
/// en el profile, no el codigo.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Seek Player And Talk",
    story: "[Agent] walks up to the player and talks",
    category: "Action/Customer",
    id: "7f8091223344556677889900aabbccdd")]
public partial class SeekPlayerAndTalkAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<float> StoppingDistance = new(1.5f);

    private NavMeshAgent _navAgent;
    private bool _hasStartedDialogue;

    protected override Status OnStart()
    {
        if (Agent?.Value == null || PlayerReference.Instance == null)
        {
            Debug.LogWarning("[SeekPlayerAndTalkAction] Falta el Agent o no hay PlayerReference en la escena.");
            return Status.Failure;
        }

        _navAgent = Agent.Value.GetComponent<NavMeshAgent>();
        if (_navAgent == null)
        {
            Debug.LogError("[SeekPlayerAndTalkAction] El Agent no tiene NavMeshAgent.");
            return Status.Failure;
        }

        _navAgent.stoppingDistance = StoppingDistance.Value;
        _navAgent.SetDestination(PlayerReference.Instance.position);

        _hasStartedDialogue = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_navAgent == null || PlayerReference.Instance == null)
            return Status.Failure;

        // Si el jugador esta en modo caja, no lo vamos a buscar ni a
        // interrumpir - nos quedamos quietos esperando a que salga.
        if (RegisterModeController.IsPlayerInRegisterMode)
        {
            if (!_navAgent.isStopped)
                _navAgent.isStopped = true;

            return Status.Running;
        }

        if (_navAgent.isStopped)
            _navAgent.isStopped = false;

        if (!_hasStartedDialogue)
        {
            // Sigue actualizando el destino por si el jugador se mueve
            // mientras el cliente camina hacia el.
            _navAgent.SetDestination(PlayerReference.Instance.position);

            if (_navAgent.pathPending)
                return Status.Running;

            bool arrived = _navAgent.remainingDistance <= _navAgent.stoppingDistance
                          && (!_navAgent.hasPath || _navAgent.velocity.sqrMagnitude < 0.01f);

            if (!arrived)
                return Status.Running;

            Debug.Log($"[SeekPlayerAndTalkAction] {Agent.Value.name} llego al jugador.");

            var lifecycle = Agent.Value.GetComponent<CustomerLifecycle>();
            string node = lifecycle?.Profile?.dialogueStartNode;

            Debug.Log($"[SeekPlayerAndTalkAction] {Agent.Value.name}: profile='{lifecycle?.Profile?.name}', dialogueStartNode='{node}'.");

            if (string.IsNullOrEmpty(node))
            {
                Debug.LogWarning($"[SeekPlayerAndTalkAction] {Agent.Value.name}: su CustomerProfile no tiene 'Dialogue Start Node' configurado.");
                return Status.Failure;
            }

            if (KarenEventTrigger.Instance == null)
            {
                Debug.LogWarning("[SeekPlayerAndTalkAction] No hay KarenEventTrigger en la escena.");
                return Status.Failure;
            }

            Debug.Log($"[SeekPlayerAndTalkAction] Llamando TriggerEncounter('{node}'). KarenEventTrigger.IsActive={KarenEventTrigger.Instance.IsActive}");
            KarenEventTrigger.Instance.TriggerEncounter(node);
            _hasStartedDialogue = true;
            return Status.Running;
        }

        // Ya arranco el dialogo: esperamos a que termine.
        return KarenEventTrigger.Instance.IsActive ? Status.Running : Status.Success;
    }
}