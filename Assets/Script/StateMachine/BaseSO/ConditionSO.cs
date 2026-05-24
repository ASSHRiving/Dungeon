using UnityEngine;

public abstract class ConditionSO : ScriptableObject
{
    [SerializeField] protected int priority;
    protected EnemyCombatSystem _combat;

    public virtual void Init(StateMachineSystem stateMachineSystem)
    {
        _combat = stateMachineSystem.GetComponentInChildren<EnemyCombatSystem>();
    }
    public abstract bool ConditionSetUp();

    public int GetPriority() => priority;
}
