using UnityEngine;

public abstract class ConditionSO : ScriptableObject
{
    [SerializeField] protected int priority;

    public abstract bool ConditionSetUp(StateMachineSystem stateMachineSystem);

    public int GetPriority() => priority;
}
