using UnityEngine;
using UnityEngine.AI;

public class StateMachineSystem : MonoBehaviour
{
    [Header("使用的轉換器腳本")]public TransitionSO transition;
    [Header("目前狀態")]public StateActionSO currrentState;

    [HideInInspector] public int randomHorizontal = 1;
    [HideInInspector] public float strafeTimer = 0f;

    public Animator animator;
    public EnemyCombatSystem combat;
    public EnemyMovementSystem movement;
    public CharacterHealthBase health;
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

    /// <summary>
    /// 隨機更換左右徘徊方向 (-1 或 1)
    /// </summary>
    public void UpdateRandomHorizontal()
    {
        randomHorizontal = (Random.value > 0.5f) ? 1 : -1;
    }

}
