using UnityEngine;

[CreateAssetMenu(fileName = "AIIdle", menuName ="StateMachine/States/AIIdle")]
public class AIIdleState : StateActionSO
{
    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        Animator _animator = stateMachineSystem.GetComponentInChildren<Animator>();
        _animator.SetFloat(verticalID, 0, 0.25f, Time.deltaTime);
        _animator.SetFloat(horizontalID, 0, 0.25f, Time.deltaTime);
    }
}
