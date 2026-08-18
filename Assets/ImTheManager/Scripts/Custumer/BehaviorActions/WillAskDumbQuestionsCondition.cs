using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "WillAskDumbQuestions", story: "[Self] will go and ask DumbQuestions", category: "Conditions", id: "d5b96468c3c0e185cc52a784661cae89")]
public partial class WillAskDumbQuestionsCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    public override bool IsTrue()
    {
        if (Self?.Value == null) return false;

        var lifecycle = Self.Value.GetComponent<CustomerLifecycle>();
        var profile = lifecycle?.Profile;
        if (profile == null) return false;

        return UnityEngine.Random.value <= profile.askQuestionChance;
    }

    public override void OnStart()
    {
    }

    public override void OnEnd()
    {
    }
}
