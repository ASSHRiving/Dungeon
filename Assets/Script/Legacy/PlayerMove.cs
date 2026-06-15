using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("移動設定")]
    public float speed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 1.3f;

    [Header("地面偵測")]
    public Transform groundCheck;
    public LayerMask groundMask;

    private bool isGrounded;
    private Vector2 moveInput;
    private Vector3 velocity;
    private CharacterController controller;
    private Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        // 直接讀取 Vector2 (包含 x 和 y)
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // 只有在按下 (started) 且在地面的時候執行
        if (context.started && isGrounded)
        {
            // 使用物理公式計算跳躍力：v = sqrt(h * -2 * g)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Update()
    {
        Move();
        Gravity();
    }
    void Move()
    {
        Vector3 moveVector = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(moveVector * speed * Time.deltaTime);
        //動畫
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveX", moveInput.x, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", moveInput.y, 0.1f, Time.deltaTime);
    }
    void Gravity()
    {
        // 檢查地面
        isGrounded = Physics.CheckBox(
            groundCheck.position,
            new Vector3(0.5f, 0.1f, 0.5f),
            Quaternion.identity,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 保持一點向下壓力確保貼地
        }

        // 應用重力
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
}
/*
void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(
            groundCheck.position,
            new Vector3(1f, 0.2f, 1f) // 這裡要跟你的 CheckBox 一樣
        );
    }
*/