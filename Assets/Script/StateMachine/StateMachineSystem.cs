using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class StateMachineSystem : MonoBehaviour
{
    [Header("使用的轉換器腳本")]public TransitionSO transition;
    [Header("目前狀態")]public StateActionSO currrentState;

    [Header("Boss 技能設定")]
    public List<AbilityBase> allEnemySkills; // 拖入這隻 Boss 擁有的所有技能 SO
    // 儲存每個技能對應的剩餘 CD 時間 Dictionary
    private Dictionary<AbilityBase, float> skillCooldowns = new Dictionary<AbilityBase, float>();
    // 目前已選定、正在調整距離準備施放的技能
    public AbilityBase currentSkill;
    // 記錄當前技能調整距離花了多少時間（防止地形卡死無限追退）
    [HideInInspector] public float skillAdjustTimer = 0f;

    [HideInInspector] public int randomHorizontal = 1;

    //計時器
    [HideInInspector] public float strafeTimer = 0f;
    [HideInInspector] public float attackTimer = 0f;

    //外部腳本
    [HideInInspector] public Animator animator;
    [HideInInspector] public EnemyCombatSystem combat;
    [HideInInspector] public EnemyMovementSystem movement;
    [HideInInspector] public CharacterHealthBase health;
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public CharacterController characterController;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        combat = GetComponentInChildren<EnemyCombatSystem>();
        movement = GetComponent<EnemyMovementSystem>();
        health = GetComponent<EnemyHealthSystem>();
        agent = GetComponent<NavMeshAgent>();
        characterController = GetComponent<CharacterController>();

        // 初始化每個技能的 CD 時間
        foreach (var skill in allEnemySkills)
        {
            if (skill != null && !skillCooldowns.ContainsKey(skill))
            {
                skillCooldowns.Add(skill, 0.3f);
            }
        }
    }
    private void Start()
    {
        currrentState?.OnEnter(this);
    }
    private void Update()
    {
        StateMachineTick();

        // 倒數所有技能的 CD 時間
        UpdateSkillCooldowns();
    }
    private void StateMachineTick()
    {
        transition?.TryGetApplyCondition(this);
        currrentState.OnUpdate(this);
    }

    private void UpdateSkillCooldowns()
    {
        for (int i = 0; i < allEnemySkills.Count; i++)
        {
            AbilityBase skill = allEnemySkills[i];
            if (skill != null && skillCooldowns.TryGetValue(skill, out float cd) && cd > 0f)
            {
                skillCooldowns[skill] = cd - Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 隨機更換左右徘徊方向 (-1 或 1)
    /// </summary>
    public void UpdateRandomHorizontal()
    {
        randomHorizontal = (Random.value > 0.5f) ? 1 : -1;
    }

    /// <summary>
    /// 檢查技能是否冷卻完畢
    /// </summary>
    public bool IsSkillReady(AbilityBase skill)
    {
        if (skill != null && skillCooldowns.TryGetValue(skill, out float remainingCD))
        {
            return remainingCD <= 0f;
        }
        return false;
    }

    /// <summary>
    /// 重置該技能的 CD 時間
    /// </summary>
    public void EnsureMinSkillCooldown(float minCooldown = 1f)
    {
        for (int i = 0; i < allEnemySkills.Count; i++)
        {
            AbilityBase skill = allEnemySkills[i];
            if (skill != null && skillCooldowns.TryGetValue(skill, out float cd) && cd < minCooldown)
            {
                skillCooldowns[skill] = minCooldown;
            }
        }
    }

    public void ResetSkillCooldown(AbilityBase skill)
    {
        if (skill != null && skillCooldowns.ContainsKey(skill))
        {
            skillCooldowns[skill] = skill.cooldown;
        }
    }
    
    /// <summary>
    /// 選出合適的技能
    /// </summary>
    /// <param name="distance">施放距離</param>
    public AbilityBase SelectReadySkill(float distance)
    {
        List<AbilityBase> candidates = new List<AbilityBase>();
        foreach(var skill in allEnemySkills)
        {
            //檢查CD
            if(skill == null || !IsSkillReady(skill)) continue;
            //檢查距離
            if(distance < skill.minDistance || distance > skill.maxDistance) continue;
            candidates.Add(skill);
        }
        if(candidates.Count == 0) return null;

        int totalWeight = 0;
        foreach(var skill in candidates) totalWeight += skill.weight;

        int randomValue = Random.Range(0, totalWeight);
        int weightSum = 0;
        foreach(var skill in candidates)
        {
            weightSum += skill.weight;
            if(weightSum >= randomValue)
            {
                return skill;
            }
        }
        return candidates[0];
    }

    public void UseSkill(AbilityBase skill)
    {
        if(skill == null) return;

        skill.ExecuteSkill(this);
    }

}
