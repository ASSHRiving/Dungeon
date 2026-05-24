using UnityEngine;

public abstract class StateActionSO : ScriptableObject
{
    [SerializeField] protected int priority;

    public int GetPriority() => priority;

    public virtual void OnEnter(StateMachineSystem stateMachineSystem)
    {
        
    }
    public abstract void OnUpdate();
    public virtual void OnExit()
    {
        
    }
}
