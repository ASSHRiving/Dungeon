using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CharacterSelector : MonoBehaviour
{
    [Header("相機與輸入綁定")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private InputActionReference pointAction;
    [SerializeField] private InputActionReference clickAction;

    [Header("模型旋轉設定")]
    [Tooltip("旋轉靈敏度 (度/像素)")]
    [SerializeField] private float rotationSpeed = 0.4f;

    [Tooltip("是否反轉旋轉方向")]
    [SerializeField] private bool invertRotation = false;

    [Tooltip("是否開啟放開滑鼠時的慣性滑行")]
    [SerializeField] private bool enableInertia = true;

    [Tooltip("慣性阻尼速度 (數值愈大停止愈快)")]
    [SerializeField] private float inertiaDamping = 8f;

    [Tooltip("判斷為拖曳的最小位移像素 (低於此數值視為單擊選角)")]
    [SerializeField] private float dragThreshold = 5f;

    [Tooltip("是否限制只能在點擊角色模型時拖曳 (預設為 false，點擊非 UI 空白處亦可拖曳)")]
    [SerializeField] private bool dragOnCharacterOnly = false;

    private bool isPointerHeld;
    private bool pointerDownOverUI;
    private bool isDragging;
    private bool canDragCurrent;
    private Vector2 pointerDownPos;
    private Vector2 lastPointerPos;
    private float totalDragDistance;
    private float currentAngularVelocity;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (pointAction != null && pointAction.action != null)
            pointAction.action.Enable();

        if (clickAction != null && clickAction.action != null)
            clickAction.action.Enable();
    }

    private void OnDisable()
    {
        if (pointAction != null && pointAction.action != null)
            pointAction.action.Disable();

        if (clickAction != null && clickAction.action != null)
            clickAction.action.Disable();

        isPointerHeld = false;
        isDragging = false;
        canDragCurrent = false;
        pointerDownOverUI = false;
        currentAngularVelocity = 0f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            isPointerHeld = false;
            isDragging = false;
            canDragCurrent = false;
            pointerDownOverUI = false;
            currentAngularVelocity = 0f;
        }
    }

    private void Update()
    {
        bool pointerCurrentlyPressed = IsPointerPressed();
        Vector2 currentPointerPos = GetPointerPosition();

        // 1. 指標按下瞬間 (Pointer Down)
        if (pointerCurrentlyPressed && !isPointerHeld)
        {
            isPointerHeld = true;
            pointerDownPos = currentPointerPos;
            lastPointerPos = currentPointerPos;
            totalDragDistance = 0f;
            currentAngularVelocity = 0f;

            // 檢查是否點擊在 UI 上，若在 UI 上則不觸發旋轉與 3D 選角
            pointerDownOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (!pointerDownOverUI)
            {
                if (LobbyManager.Instance != null && !LobbyManager.Instance.HasSelectedCharacter)
                {
                    // 全景大廳狀態：若點中角色則立即選定，並允許接續拖曳旋轉
                    if (Physics.Raycast(mainCamera.ScreenPointToRay(pointerDownPos), out RaycastHit hit))
                    {
                        CharacterSelect charSelect = hit.collider.GetComponentInParent<CharacterSelect>();
                        if (charSelect != null)
                        {
                            charSelect.Select();
                            canDragCurrent = true;
                            isDragging = true;
                        }
                    }
                }
                else if (LobbyManager.Instance != null && LobbyManager.Instance.HasSelectedCharacter)
                {
                    // 已聚焦選定角色狀態：判斷是否允許拖曳旋轉
                    if (dragOnCharacterOnly)
                    {
                        if (Physics.Raycast(mainCamera.ScreenPointToRay(pointerDownPos), out RaycastHit hit))
                        {
                            CharacterSelect charSelect = hit.collider.GetComponentInParent<CharacterSelect>();
                            canDragCurrent = (charSelect == LobbyManager.Instance.CurrentCharacter);
                        }
                        else
                        {
                            canDragCurrent = false;
                        }
                    }
                    else
                    {
                        canDragCurrent = true;
                    }

                    if (canDragCurrent)
                    {
                        isDragging = true;
                    }
                }
            }
        }
        // 2. 指標按住拖曳中 (Pointer Held)
        else if (pointerCurrentlyPressed && isPointerHeld)
        {
            if (!pointerDownOverUI && isDragging)
            {
                Vector2 delta = currentPointerPos - lastPointerPos;
                totalDragDistance += delta.magnitude;
                lastPointerPos = currentPointerPos;

                if (LobbyManager.Instance != null && LobbyManager.Instance.HasSelectedCharacter)
                {
                    CharacterSelect current = LobbyManager.Instance.CurrentCharacter;
                    if (current != null && (totalDragDistance >= dragThreshold || Mathf.Abs(delta.x) > 0.01f))
                    {
                        float deltaX = delta.x;
                        float dir = invertRotation ? 1f : -1f;
                        float angleDelta = deltaX * rotationSpeed * dir;

                        current.RotateModel(angleDelta);

                        float dt = Time.unscaledDeltaTime;
                        if (dt > 0f)
                        {
                            currentAngularVelocity = angleDelta / dt;
                        }
                    }
                }
            }
            else
            {
                lastPointerPos = currentPointerPos;
            }
        }
        // 3. 指標放開瞬間 (Pointer Up)
        else if (!pointerCurrentlyPressed && isPointerHeld)
        {
            isPointerHeld = false;

            if (!pointerDownOverUI)
            {
                // 若位移極小，視為單擊選角 (Click)
                if (totalDragDistance < dragThreshold)
                {
                    currentAngularVelocity = 0f;

                    if (Physics.Raycast(mainCamera.ScreenPointToRay(currentPointerPos), out RaycastHit hit))
                    {
                        CharacterSelect charSelect = hit.collider.GetComponentInParent<CharacterSelect>();
                        if (charSelect != null)
                        {
                            charSelect.Select();
                        }
                    }
                }
            }

            isDragging = false;
            canDragCurrent = false;
            pointerDownOverUI = false;
        }

        // 4. 慣性滑動處理 (Inertia Damping when released)
        if (!isPointerHeld && enableInertia && Mathf.Abs(currentAngularVelocity) > 0.1f)
        {
            if (LobbyManager.Instance != null && LobbyManager.Instance.HasSelectedCharacter)
            {
                CharacterSelect current = LobbyManager.Instance.CurrentCharacter;
                if (current != null)
                {
                    float dt = Time.unscaledDeltaTime;
                    current.RotateModel(currentAngularVelocity * dt);
                    currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, 0f, inertiaDamping * dt);

                    if (Mathf.Abs(currentAngularVelocity) < 0.2f)
                    {
                        currentAngularVelocity = 0f;
                    }
                }
                else
                {
                    currentAngularVelocity = 0f;
                }
            }
            else
            {
                currentAngularVelocity = 0f;
            }
        }
    }

    /// <summary>
    /// 獲取當前指針在螢幕上的位置 (像素座標)
    /// </summary>
    private Vector2 GetPointerPosition()
    {
        if (pointAction != null && pointAction.action != null && pointAction.action.enabled)
        {
            Vector2 val = pointAction.action.ReadValue<Vector2>();
            if (val != Vector2.zero)
                return val;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif

        return (Vector2)Input.mousePosition;
    }

    /// <summary>
    /// 檢查是否有滑鼠按鍵或點擊輸入按住 (相容 New Input System 與 Legacy Input)
    /// </summary>
    private bool IsPointerPressed()
    {
        if (clickAction != null && clickAction.action != null && clickAction.action.enabled)
        {
            if (clickAction.action.IsPressed())
                return true;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed)
                return true;
        }
#endif

        return Input.GetMouseButton(0) || Input.GetMouseButton(1);
    }
}
