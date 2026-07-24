using UnityEngine;
using UnityEngine.AI;

public class StateMachineSystem : MonoBehaviour
{
    [Header("使用的轉換器腳本")]public TransitionSO transition;
    [Header("目前狀態")]public StateActionSO currrentState;
    public Animator animator;
    public EnemyCombatSystem combat;
    public EnemyMovementSystem movement;
    public NavMeshAgent agent;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        combat = GetComponentInChildren<EnemyCombatSystem>();
        movement = GetComponent<EnemyMovementSystem>();
        agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        currrentState?.OnEnter(this);
    }
    private void Update()
    {
        StateMachineTick();
    }
    private void StateMachineTick()
    {
        transition?.TryGetApplyCondition(this);
        currrentState.OnUpdate(this);
    }

}
