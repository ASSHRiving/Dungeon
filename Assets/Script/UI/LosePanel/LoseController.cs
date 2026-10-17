using UnityEngine;

public class LoseController : UIPanelBase
{
    [SerializeField] private GameObject losePanel;

    protected override GameObject PanelRoot => losePanel;

    // 死亡結算面板不可透過 Esc 鍵關閉，且在堆疊中會阻止 Esc 選單與角色面板開啟
    public override bool CanCloseWithEsc => false;
    public override bool PauseGameWhenOpen => false;

    private void OnEnable()
    {
        UIEvents.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        UIEvents.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        Open();
    }

    public void QuitGame()
    {
        if (GameAssets.Instance != null) GameAssets.Instance.PlayUIClickSound();
        HideImmediate();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}

