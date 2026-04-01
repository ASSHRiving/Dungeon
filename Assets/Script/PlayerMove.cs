using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float speed = 6f;
    public float gravity = -50f;
    public float jumpHeight = 1f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    Vector3 velocity;
    bool isGrounded;

    CharacterController controller;
    Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 檢查是否在地面
        isGrounded = Physics.CheckBox(
            groundCheck.position,
            new Vector3(0.5f, 0.1f, 0.5f),
            Quaternion.identity,
            groundMask
        );

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        animator.SetFloat("MoveX", x);
        animator.SetFloat("MoveY", z);

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        // 跳躍
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 重力
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireCube(
            groundCheck.position,
            new Vector3(1f, 0.2f, 1f) // 這裡要跟你的 CheckBox 一樣
        );
    }
}