using UnityEngine;

/// <summary>
/// 舊版彈窗式新手教學（已由 TutorialManager + TutorialHUDController 互動式教學取代）。
/// 保留類別以避免場景殘留參照報錯，但強制停用開局暫停與彈窗行為。
/// </summary>
[System.Obsolete("Replaced by TutorialManager and TutorialHUDController.")]
public class TutorialController : UIPanelBase
{
    [Header("UI 組件引用")]
    [SerializeField] private CanvasGroup overlayCanvasGroup;   // 背景遮罩的透明度控制
    [SerializeField] private RectTransform windowRect;         // 彈窗本體 RectTransform
    [SerializeField] private CanvasGroup windowCanvasGroup;    // 彈窗本體的透明度控制
    [SerializeField] private GameObject tutorialPanel;

    protected override GameObject PanelRoot => tutorialPanel;
    protected override CanvasGroup WindowCanvasGroup => windowCanvasGroup;
    protected override RectTransform WindowRect => windowRect;
    protected override CanvasGroup OverlayCanvasGroup => overlayCanvasGroup;
    protected override bool HideOnAwake => true;

    public override bool PauseGameWhenOpen => false;

    private void Start()
    {
        HideImmediate();
    }

    public void Confirm()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        Close();
    }

    public void OpenTutorial()
    {
        HideImmediate();
    }
}

