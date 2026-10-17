using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIController : MonoBehaviour
{
    [Header("快捷鍵對應面板 (若未指定會自動搜尋)")]
    [SerializeField] private EscController escController;
    [SerializeField] private PlayerStatsController playerStatsController;

    private Transform player;
    private Transform cameraTransform;
    private PlayerInput inputSystem;

    // 維護當前開啟的面板堆疊（List 尾端為最上層面板）
    private readonly List<UIPanelBase> activePanels = new List<UIPanelBase>();

    /// <summary>
    /// 目前是否有任何彈窗或選單處於開啟狀態。
    /// </summary>
    public bool HasOpenPanels => activePanels.Count > 0;

    /// <summary>
    /// 取得目前位於最上層的面板（若無則回傳 null）。
    /// </summary>
    public UIPanelBase TopPanel => activePanels.Count > 0 ? activePanels[activePanels.Count - 1] : null;

    private void Awake()
    {
        if (escController == null)
        {
            escController = GetComponentInChildren<EscController>(true) ?? FindFirstObjectByType<EscController>();
        }
        if (playerStatsController == null)
        {
            playerStatsController = GetComponentInChildren<PlayerStatsController>(true) ?? FindFirstObjectByType<PlayerStatsController>();
        }
    }

    private void Update()
    {
        PlayerInput input = GetPlayerInput();
        if (input == null || input.actions == null) return;

        // 1. 統一處理 Esc 鍵：優先關閉最上層面板；若無開啟面板則打開 Esc 選單
        InputAction escAction = input.actions.FindAction("Esc");
        if (escAction != null && escAction.triggered)
        {
            HandleEscKeyPressed();
            return;
        }

        // 2. 統一處理 Character 鍵：僅在無其他面板開啟時打開角色面板，或當角色面板在最上層時將其關閉
        InputAction characterAction = input.actions.FindAction("Character");
        if (characterAction != null && characterAction.triggered)
        {
            HandleCharacterKeyPressed();
        }
    }

    private void HandleEscKeyPressed()
    {
        CleanupInvalidPanels();

        if (activePanels.Count > 0)
        {
            UIPanelBase topPanel = activePanels[activePanels.Count - 1];
            if (topPanel != null && topPanel.CanCloseWithEsc)
            {
                topPanel.OnEscPressed();
            }
        }
        else if (escController != null)
        {
            escController.OpenEsc();
        }
    }

    private void HandleCharacterKeyPressed()
    {
        if (playerStatsController == null) return;

        CleanupInvalidPanels();

        if (playerStatsController.IsOpen)
        {
            // 只有當角色狀態面板在最上層時，按快捷鍵才直接關閉
            if (TopPanel == playerStatsController)
            {
                playerStatsController.ClosePanel();
            }
        }
        else if (activePanels.Count == 0)
        {
            // 沒有其他彈窗擋住時才允許打開角色面板
            playerStatsController.OpenPanel();
        }
    }

    /// <summary>
    /// 由 UIPanelBase.Open() 呼叫，將面板推入開啟堆疊並更新全域狀態。
    /// </summary>
    public void RegisterOpenPanel(UIPanelBase panel)
    {
        if (panel == null) return;

        // 若已在堆疊中，先移除再放到最上層
        activePanels.Remove(panel);
        activePanels.Add(panel);

        RefreshUIState();
    }

    /// <summary>
    /// 由 UIPanelBase.Close() / HideImmediate() 呼叫，從堆疊移除面板並更新全域狀態。
    /// </summary>
    public void UnregisterPanel(UIPanelBase panel)
    {
        if (panel == null) return;

        if (activePanels.Remove(panel))
        {
            RefreshUIState();
        }
    }

    /// <summary>
    /// 根據當前開啟的面板堆疊，統一計算並套用 TimeScale、ActionMap 與滑鼠游標狀態。
    /// </summary>
    public void RefreshUIState()
    {
        CleanupInvalidPanels();

        bool hasOpenPanel = activePanels.Count > 0;
        bool shouldPause = false;

        for (int i = 0; i < activePanels.Count; i++)
        {
            if (activePanels[i].PauseGameWhenOpen)
            {
                shouldPause = true;
                break;
            }
        }

        Time.timeScale = shouldPause ? 0f : 1f;

        PlayerInput input = GetPlayerInput();
        if (input != null)
        {
            string targetActionMap = hasOpenPanel ? "UI" : "Player";
            if (input.currentActionMap == null || input.currentActionMap.name != targetActionMap)
            {
                input.SwitchCurrentActionMap(targetActionMap);
            }
        }

        Cursor.lockState = hasOpenPanel ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = hasOpenPanel;
    }

    private void CleanupInvalidPanels()
    {
        activePanels.RemoveAll(p => p == null || !p.IsOpen);
    }

    public PlayerInput GetPlayerInput()
    {
        if (inputSystem == null)
        {
            inputSystem = FindFirstObjectByType<PlayerInput>();
        }
        return inputSystem;
    }

    public Transform GetPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        return player;
    }

    public Transform GetCameraTransform()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        return cameraTransform;
    }
}

