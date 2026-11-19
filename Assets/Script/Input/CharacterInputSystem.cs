using UnityEngine;
using UnityEngine.InputSystem;

public enum BufferedAction
{
    None,
    Attack,
    Roll
}

[DefaultExecutionOrder(-100)] // 確保 Input Buffer 永遠在 Movement / Combat 的 Update 之前更新
[RequireComponent(typeof(PlayerInput))] // 🔑 強制要求物件要有 PlayerInput 組件
public class CharacterInputSystem : MonoBehaviour
{
    private PlayerInput playerInput;

    [Header("Input Buffer 設定")]
    [SerializeField, Min(0f)] private float attackBufferTime = 0.30f;
    [SerializeField, Min(0f)] private float rollBufferTime = 0.28f;

    private BufferedAction bufferedAction = BufferedAction.None;
    private float bufferExpireTime = 0f;

    // 🔑 直接從 PlayerInput 讀取 Action
    public Vector2 playerMovement => playerInput.actions["Movement"].ReadValue<Vector2>();
    public Vector2 playerCamera   => playerInput.actions["CameraLook"].ReadValue<Vector2>();
    
    public bool playerInteract => playerInput.actions["Interact"].triggered;
    public bool playerPause    => playerInput.actions["Esc"].triggered;
    public bool playerLAtk     => playerInput.actions["LAtk"].triggered;

    public bool playerRoll  => playerInput.actions["RAtk"].triggered;
    public bool playerDefen => playerInput.actions["Defen"].phase == InputActionPhase.Performed;
    public bool playerRun   => playerInput.actions["Run"].phase == InputActionPhase.Performed;

    public BufferedAction CurrentBufferedAction => HasAnyBuffered() ? bufferedAction : BufferedAction.None;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnDisable()
    {
        ClearBuffer();
    }

    private void Update()
    {
        // 暫停或切換至 UI ActionMap 時立即清空緩衝，避免關閉選單後殘留舊輸入
        if (playerInput == null || Time.timeScale <= 0f ||
            playerInput.currentActionMap == null || playerInput.currentActionMap.name != "Player")
        {
            ClearBuffer();
            return;
        }

        // 檢查緩衝是否過期（使用 Time.time，讓 Hitstop 頓幀期間不會白白消耗緩衝）
        if (bufferedAction != BufferedAction.None && Time.time > bufferExpireTime)
        {
            ClearBuffer();
        }

        // 單一最新意圖槽（Single-Slot）：Roll 優先權高於 Attack，避免緊急翻滾被連點普攻蓋掉
        if (playerRoll)
        {
            BufferAction(BufferedAction.Roll, rollBufferTime);
        }
        else if (playerLAtk)
        {
            if (bufferedAction != BufferedAction.Roll)
            {
                BufferAction(BufferedAction.Attack, attackBufferTime);
            }
        }
    }

    private void BufferAction(BufferedAction action, float duration)
    {
        bufferedAction = action;
        bufferExpireTime = Time.time + Mathf.Max(0f, duration);
    }

    public bool HasAnyBuffered()
    {
        return bufferedAction != BufferedAction.None && Time.time <= bufferExpireTime;
    }

    public bool HasBuffered(BufferedAction action)
    {
        return action != BufferedAction.None && bufferedAction == action && Time.time <= bufferExpireTime;
    }

    public bool TryConsumeBuffer(BufferedAction action)
    {
        if (!HasBuffered(action)) return false;

        ClearBuffer();
        return true;
    }

    public void ClearBuffer()
    {
        bufferedAction = BufferedAction.None;
        bufferExpireTime = 0f;
    }

    public void ClearBuffer(BufferedAction action)
    {
        if (bufferedAction == action)
        {
            ClearBuffer();
        }
    }
}