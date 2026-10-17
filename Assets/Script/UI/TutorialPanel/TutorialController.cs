using UnityEngine;

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
    protected override bool HideOnAwake => false;

    public override bool PauseGameWhenOpen => false;

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentLevel == 1)
        {
            Debug.Log("Tutorial Open");
            OpenTutorial();
        }
        else
        {
            HideImmediate();
        }
    }

    public void Confirm()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        Close();
    }

    public void OpenTutorial()
    {
        gameObject.SetActive(true);
        Open();
    }
}

