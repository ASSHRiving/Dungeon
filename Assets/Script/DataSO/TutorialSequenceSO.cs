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
}
