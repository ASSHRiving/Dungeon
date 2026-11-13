using UnityEngine;

public abstract class StateActionSO : ScriptableObject
{
    [SerializeField] protected int priority;

    //AnimationID
    protected int horizontalID = Animator.StringToHash("Horizontal");
    protected int verticalID = Animator.StringToHash("Vertical");
    protected int runID = Animator.StringToHash("Run");
    protected int lockOnID = Animator.StringToHash("LockOn");

    public int GetPriority() => priority;

    public virtual void OnEnter(StateMachineSystem stateMachineSystem)
    {
    }

    public abstract void OnUpdate(StateMachineSystem stateMachineSystem);
    public virtual void OnExit(StateMachineSystem stateMachineSystem)
    {
        
    }
}
