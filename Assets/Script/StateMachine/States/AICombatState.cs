using UnityEngine;

[CreateAssetMenu(fileName = "AICombat", menuName ="StateMachine/States/AICombat")]
public class AICombatState : StateActionSO
{
    private int randomHorizontal;

    
    public override void OnEnter(StateMachineSystem stateMachineSystem)
    {
        Animator _animator = stateMachineSystem.GetComponentInChildren<Animator>();
        _animator.Play("Ready");
    }

    public override void OnUpdate(StateMachineSystem stateMachineSystem)
    {
        NoCombatMove(stateMachineSystem);
    }

    private void NoCombatMove(StateMachineSystem stateMachineSystem)
    {
        Animator _animator = stateMachineSystem.GetComponentInChildren<Animator>();
        EnemyCombatSystem _combat = stateMachineSystem.GetComponentInChildren<EnemyCombatSystem>();
        EnemyMovementSystem _movement = stateMachineSystem.GetComponent<EnemyMovementSystem>();

        //非戰鬥時邏輯
        if (_animator.CheckAnimationTag("Motion"))
        {
            if(_combat.GetCurrentTargetDistance() < 2.5f + 0.1f)
            {
                //太進後退
                _movement.CharacterMoveInterface(-_movement.transform.forward, 1.4f, true);
                _animator.SetFloat(verticalID, -1, 0.25f, Time.deltaTime);
                _animator.SetFloat(horizontalID, 0, 0.25f, Time.deltaTime);

                randomHorizontal = GetRandomHorizontal();

                if(_combat.GetCurrentTargetDistance() < 1.5 + 0.05f)
                {
                    _animator.Play("AutoAttack_1", 0, 0);
                    randomHorizontal = GetRandomHorizontal();
                }
            }else if(_combat.GetCurrentTargetDistance() > 2.5f + 0.1f && _combat.GetCurrentTargetDistance() < 6.1f + 0.5f)
            {
                _movement.CharacterMoveInterface(_movement.transform.right * ((randomHorizontal == 0)? 1 : randomHorizontal), 1.4f, true);
                _animator.SetFloat(verticalID, 0, 0.25f, Time.deltaTime);
                _animator.SetFloat(horizontalID, ((randomHorizontal == 0)? 1 : randomHorizontal), 0.25f, Time.deltaTime);
            }else if(_combat.GetCurrentTargetDistance() > 6.1f + 0.5f)
            {
                _movement.CharacterMoveInterface(_movement.transform.forward, 1.4f, true);
                _animator.SetFloat(verticalID, 1, 0.25f, Time.deltaTime);
                _animator.SetFloat(horizontalID, 0, 0.25f, Time.deltaTime);

                randomHorizontal = GetRandomHorizontal();
            }
        }
        else
        {
            _animator.SetFloat(verticalID, 0);
            _animator.SetFloat(horizontalID, 0);
            _animator.SetFloat(runID, 0f);
        }
    }

    private int GetRandomHorizontal() => Random.Range(-1, 2);
}
