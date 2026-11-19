using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 單段連擊動作資料（資料驅動，以 0~1 動畫進度比例控制各取消視窗）
/// </summary>
[Serializable]
public class ComboStepData
{
    [Tooltip("Animator 狀態路徑（例如 Sword.Attack1、Katana.Attack1、Hammer.GreatSword_Attack01）")]
    public string animatorStatePath = "Sword.Attack1";

    [Tooltip("CrossFadeInFixedTime 切換融合時間（秒）")]
    [Min(0f)] public float crossFadeDuration = 0.08f;

    [Tooltip("移動取消後搖回到 Motion 的 CrossFade 融合時間（秒）")]
    [Min(0f)] public float moveCancelCrossFadeDuration = 0.25f;

    [Header("取消視窗（0~1 動畫進度比例）")]
    [Tooltip("超過此進度才允許翻滾取消（前搖＋揮砍判定期間鎖定）")]
    [Range(0f, 1f)] public float rollCancelTime = 0.31f;

    [Tooltip("超過此進度才允許接下一段連擊")]
    [Range(0f, 1f)] public float comboCancelTime = 0.31f;

    [Tooltip("超過此進度才允許推方向鍵移動取消後搖")]
    [Range(0f, 1f)] public float moveCancelTime = 0.34f;

    [Tooltip("自然播完切回 Locomotion 的進度（建議對齊 Animator Attack -> Motion 的 ExitTime）")]
    [Range(0.1f, 1f)] public float endNormalizedTime = 0.88f;

    [Tooltip("招式自然結束或被移動取消後，多久內再按攻擊還能接續下一段連擊（秒）")]
    [Min(0f)] public float comboHoldDuration = 0.65f;

    private string cachedFullPath;
    private int cachedFullPathHash;

    public int GetFullPathHash()
    {
        if (string.IsNullOrEmpty(animatorStatePath)) return 0;

        string fullPath = animatorStatePath.StartsWith("Base Layer.", StringComparison.Ordinal)
            ? animatorStatePath
            : "Base Layer." + animatorStatePath;

        if (cachedFullPath != fullPath || cachedFullPathHash == 0)
        {
            cachedFullPath = fullPath;
            cachedFullPathHash = Animator.StringToHash(fullPath);
        }

        return cachedFullPathHash;
    }
}

/// <summary>
/// 武器連段組資產（同類型武器可共用同一份 ComboSetSO，特殊武器也可獨立配置）
/// </summary>
[CreateAssetMenu(fileName = "ComboSet_", menuName = "Dungeon/Combat/Combo Set")]
public class ComboSetSO : ScriptableObject
{
    public List<ComboStepData> steps = new List<ComboStepData>();

    public int StepCount => steps != null ? steps.Count : 0;

    public ComboStepData GetStep(int index)
    {
        if (steps == null || steps.Count == 0) return null;
        int safeIndex = ((index % steps.Count) + steps.Count) % steps.Count;
        return steps[safeIndex];
    }
}
