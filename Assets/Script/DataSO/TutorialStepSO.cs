using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教學步驟進場時機。
/// </summary>
public enum TutorialTriggerMode
{
    Immediate,
    OnEnterFightingRoom,
    OnInteractableDetected,
    OnEnemyAttackInCombat,
    OnEnterShopRoom
}

/// <summary>
/// 教學步驟完成條件類型。
/// </summary>
public enum TutorialConditionType
{
    MoveDuration,
    RunDuration,
    RollCount,
    AttackCount,
    KillEnemyCount,
    InteractCount,
    CameraLookAmount,
    OpenCharacterPanel,
    PerfectDodgeInBulletTime,
    ShopInteractCount,
}

/// <summary>
/// 文案佔位符與 InputAction 路徑的對應設定。
/// </summary>
[Serializable]
public class TutorialActionPlaceholder
{
    [Tooltip("文案中的佔位符，例如 {Movement} 或 {LAtk}")]
    public string placeholder = "{LAtk}";

    [Tooltip("InputAction 路徑，例如 Player/Movement 或 Player/LAtk")]
    public string actionPath = "Player/LAtk";

    [Tooltip("若為 2DVector 複合綁定（如 Movement），合併顯示方向按鍵")]
    public bool isCompositeGroup = false;
}

/// <summary>
/// 單一步驟教學設定 ScriptableObject。
/// </summary>
[CreateAssetMenu(fileName = "TutorialStep_", menuName = "Dungeon/Tutorial/Tutorial Step")]
public class TutorialStepSO : ScriptableObject
{
    [Header("步驟識別與顯示文案")]
    public string stepId;
    public string phaseLabel = "階段一 · 基礎操作";
    public string title = "基礎移動";

    [TextArea(2, 4)]
    [Tooltip("支援 {Placeholder} 動態替換為當前按鍵名稱")]
    public string descriptionTemplate = "使用 {Movement} 移動角色";

    [Header("動態按鍵綁定對應")]
    public List<TutorialActionPlaceholder> actionBindings = new List<TutorialActionPlaceholder>();

    [Header("觸發與完成判定")]
    public TutorialTriggerMode triggerMode = TutorialTriggerMode.Immediate;
    public TutorialConditionType conditionType = TutorialConditionType.MoveDuration;

    [Min(0.1f)]
    public float targetProgress = 1.2f;

    [Tooltip("是否顯示進度條與進度文字")]
    public bool showProgressBar = true;

    [Tooltip("是否為時間累積型（顯示秒數進度）；否則顯示整數次數")]
    public bool isDurationCondition = false;

    [Header("彈性推進 (容錯機制)")]
    [Tooltip("允許玩家提前進入後續房間情境時自動推進至此步驟之前置後繼步驟")]
    public bool allowElasticAdvance = true;
}
