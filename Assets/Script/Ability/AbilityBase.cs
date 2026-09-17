using UnityEngine;

public abstract class AbilityBase : ScriptableObject
{
    [Header("技能基本資訊")]
    public string skillName;
    public string animTriggerName;  // Animator 的 Trigger 名稱
    public float minDistance = 0f;  // 技能最低使用距離
    public float maxDistance = 5f;  // 技能最高使用距離
    public float cooldown = 5f;     // 冷卻時間
    public int weight = 1;          // 隨機權重 (機率)

    // 執行技能
    public abstract void ExecuteSkill(StateMachineSystem system);

}
