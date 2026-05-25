using UnityEngine;

public abstract class StateActionSO : ScriptableObject
{
    [SerializeField] protected int priority;
    protected Animator _animator;
    protected EnemyCombatSystem _combat;
    protected EnemyMovementSystem _movement;

    //AnimationID
    protected int horizontalID = Animator.StringToHash("Horizontal");
    protected int verticalID = Animator.StringToHash("Vertical");
    protected int runID = Animator.StringToHash("Run");

    public int GetPriority() => priority;

    public virtual void OnEnter(StateMachineSystem stateMachineSystem)
    {
        _animator = stateMachineSystem.GetComponentInChildren<Animator>();
        _combat = stateMachineSystem.GetComponentInChildren<EnemyCombatSystem>();
        _movement = stateMachineSystem.GetComponent<EnemyMovementSystem>();
    }
    public abstract void OnUpdate();
    public virtual void OnExit()
    {
        
    }
}
