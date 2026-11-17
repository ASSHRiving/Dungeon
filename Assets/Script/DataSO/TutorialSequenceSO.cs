using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 互動式新手教學序列設定，依序定義各教學步驟。
/// </summary>
[CreateAssetMenu(fileName = "TutorialSequence_", menuName = "Dungeon/Tutorial/Tutorial Sequence")]
public class TutorialSequenceSO : ScriptableObject
{
    [Header("教學步驟清單（依執行順序排列）")]
    public List<TutorialStepSO> steps = new List<TutorialStepSO>();

    public int StepCount => steps != null ? steps.Count : 0;

    public TutorialStepSO GetStep(int index)
    {
        if (steps == null || index < 0 || index >= steps.Count) return null;
        return steps[index];
    }

    /// <summary>
    /// 從指定索引之後尋找第一個符合目標觸發時機的步驟索引；找不到回傳 -1。
    /// </summary>
    public int FindNextStepIndexByTrigger(int afterIndex, TutorialTriggerMode triggerMode)
    {
        if (steps == null) return -1;

        int start = Mathf.Max(0, afterIndex + 1);
        for (int i = start; i < steps.Count; i++)
        {
            if (steps[i] != null && steps[i].triggerMode == triggerMode)
            {
                return i;
            }
        }
        return -1;
    }
}
