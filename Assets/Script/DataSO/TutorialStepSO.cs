using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Tooltip("若為 2DVector 複合綁定（如 Movement），合併顯示方向按鍵（如 WASD）")]
    public bool isCompositeGroup = false;
}

/// <summary>
/// 單一步驟教學狀態機基底 ScriptableObject。
/// 所有教學（無論順序型或隨時可觸發型）皆放入同一份清單，實作 OnEnter / OnUpdate / OnExit，
/// 並在條件達成時呼叫 <see cref="ShowCard"/> 顯示卡片與 <see cref="AddProgress"/> 累計進度。
/// </summary>
public abstract class TutorialStepSO : ScriptableObject
{
    private const string SaveEpochPrefKey = "Tutorial_SaveEpoch";

    [Header("步驟識別與顯示文案")]
    public string stepId;

    [Tooltip("顯示於教學卡片頂部的步驟標題（留空則隱藏標題列）")]
    public string title = "";

    [TextArea(2, 4)]
    [Tooltip("支援 {Placeholder} 動態替換為當前按鍵名稱")]
    public string descriptionTemplate = "按 {Movement} 移動";

    [Header("動態按鍵綁定對應")]
    public List<TutorialActionPlaceholder> actionBindings = new List<TutorialActionPlaceholder>();

    [Header("完成目標與顯示設定")]
    [Min(0.1f)]
    public float targetProgress = 1.0f;

    [Tooltip("是否在 HUD 卡片上顯示進度條與數值文字")]
    public bool showProgressBar = true;

    [Tooltip("是否為時間累積型條件（true 顯示秒數如 0.8/1.2s；false 顯示整數次數如 1/2）")]
    public bool isDurationCondition = false;

    [Header("觸發順序設定")]
    [Tooltip("未勾選時依清單順序逐一啟動（如 1~6 主線）；勾選後會於背景隨時待命，條件滿足就搶先跳出（如商店、菁英房、寶箱房）")]
    public bool canTriggerAnytime = false;

    protected TutorialManager Manager { get; private set; }
    public float CurrentProgress { get; private set; }
    public bool IsCardShown { get; private set; }
    public bool IsCompleted => CurrentProgress >= targetProgress;

    #region 步驟完成存檔（統一以 SaveEpoch 隔離重置版本）
    private static int CurrentSaveEpoch => PlayerPrefs.GetInt(SaveEpochPrefKey, 0);
    private string SavePrefKey => $"Tutorial_{CurrentSaveEpoch}_{(!string.IsNullOrEmpty(stepId) ? stepId : name)}";

    public bool IsSavedCompleted => PlayerPrefs.GetInt(SavePrefKey, 0) == 1;

    public void MarkSavedCompleted()
    {
        PlayerPrefs.SetInt(SavePrefKey, 1);
        PlayerPrefs.Save();
    }

    public static void ResetAllSavedStates()
    {
        PlayerPrefs.SetInt(SaveEpochPrefKey, CurrentSaveEpoch + 1);
        PlayerPrefs.Save();
    }
    #endregion

    /// <summary>【由 TutorialManager 呼叫】進入此教學狀態。</summary>
    public void EnterStep(TutorialManager manager)
    {
        CurrentProgress = 0f;
        IsCardShown = false;
        Manager = manager;
        OnEnter();
    }

    /// <summary>【由 TutorialManager 呼叫】離開此教學狀態並清理監聽。</summary>
    public void ExitStep()
    {
        IsCardShown = false;
        OnExit();
        Manager = null;
    }

    /// <summary>【子類別呼叫】申請將教學卡片顯示於 HUD（同一時間只顯示一張卡片）。</summary>
    protected void ShowCard()
    {
        if (IsCardShown || Manager == null)
        {
            return;
        }

        if (!Manager.TryClaimCardDisplay(this))
        {
            return;
        }

        IsCardShown = true;
        Manager.ShowStepCard(this);
    }

    /// <summary>【子類別呼叫】推進完成進度（僅在卡片已顯示且未開啟避讓彈窗時累計）。</summary>
    protected void AddProgress(float amount)
    {
        if (amount <= 0f || !IsCardShown)
        {
            return;
        }

        if (Manager != null && Manager.IsSuppressed)
        {
            return;
        }

        CurrentProgress += amount;
        Manager?.OnStepProgressChanged(this);
    }

    /// <summary>進入此教學步驟時執行一次。</summary>
    protected virtual void OnEnter() { }

    /// <summary>停留在此教學步驟期間每幀執行。</summary>
    public virtual void OnUpdate() { }

    /// <summary>完成、略過或中止離開此教學步驟時執行一次。</summary>
    protected virtual void OnExit() { }
}
