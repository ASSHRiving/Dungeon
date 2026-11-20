using UnityEngine;
public abstract class CharacterMovementBase : MonoBehaviour
{
    //參考腳本
    protected Animator animator;
    protected CharacterController control;
    protected CharacterInputSystem inputSystem;
    protected CharacterCombatBase combat;
    protected AnimationEventHelper animationEventHelper;
    //==========================================================================

    [SerializeField] protected float characterGravity = -9.8f;
    [SerializeField] protected float characterCurrentMoveSpeed;
    protected float characterFallTime = 0.1f;
    protected float characterFallOutDeltaTime;
    protected float verticalSpeed; 
    protected float maxVerticalSpeed = 53f;
    [Header("音效")]
    [SerializeField] private SoundAssetsType footstepSoundType;
    [SerializeField] private GameSoundSO soundAssets;

    [SerializeField, Header("地面檢測")] protected LayerMask whatIsGround;
    [SerializeField] protected float groundedStickSpeed = -5f;
    
    [SerializeField] protected bool isOnGround;
    public bool IsGrounded => isOnGround;
    private bool hitGroundThisFrame;

    //AnimationID
    protected int animationMoveID = Animator.StringToHash("AnimationMove");
    protected int speedID = Animator.StringToHash("Speed");
    protected int horizontalID = Animator.StringToHash("Horizontal");
    protected int verticalID = Animator.StringToHash("Vertical");
    protected int runID = Animator.StringToHash("Run");
    protected int rollId = Animator.StringToHash("Roll");
    protected int isGroundID = Animator.StringToHash("IsGround");


    //移動向量
    protected Vector3 movementDirection;
    protected Vector3 verticalDirection;

    //閃避
    public bool immune { get; protected set; }
    protected float immuneTime = 0.4f;

    [Header("音源設定")]
    [SerializeField] private AudioSource audioSource;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        control = GetComponent<CharacterController>();
        inputSystem = GetComponent<CharacterInputSystem>();
        combat = GetComponentInChildren<CharacterCombatBase>();
        animationEventHelper = GetComponentInChildren<AnimationEventHelper>();
        if(animationEventHelper != null)
        {
            animationEventHelper.OnFootstepEvent += PlayFootstepSound;
        }
    }
    protected virtual void Start()
    {
        characterFallOutDeltaTime = characterFallTime;
    }
    protected virtual void Update()
    {
        bool groundedThisFrame = ConsumeGroundContact();
        CharacterGravity(groundedThisFrame);
    }

    /// <summary>
    /// 結算上一幀 CharacterController 碰撞累積的落地結果，並重置給本幀使用
    /// </summary>
    private bool ConsumeGroundContact()
    {
        bool grounded = hitGroundThisFrame;
        hitGroundThisFrame = false;
        return grounded;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (control == null || !control.enabled) return;
        if (((1 << hit.gameObject.layer) & whatIsGround.value) == 0) return;

        float maxSlopeAngle = Mathf.Clamp(control.slopeLimit + 0.5f, 0f, 89f);
        float minGroundNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
        if (hit.normal.y >= minGroundNormalY)
        {
            hitGroundThisFrame = true;
        }
    }
    private void PlayFootstepSound()
    {
        if(audioSource != null )
        {
            AudioClip clip = soundAssets.GetClipAssets(footstepSoundType);
            if(clip != null)
            {
                audioSource.PlayOneShot(clip, 2f);
            }
            else
            {
                Debug.LogWarning($"Footstep sound clip for {footstepSoundType} is not assigned in SoundAssets.");
            }
            //GameAssets.Instance.PlaySoundEffect(audioSource, footstepSoundType);
        }
    }

    private void CharacterGravity(bool groundedThisFrame)
    {
        if (groundedThisFrame)
        {
            characterFallOutDeltaTime = characterFallTime;
            isOnGround = true;

            if (verticalSpeed < 0.0f)
            {
                verticalSpeed = groundedStickSpeed;
            }

            if (animator != null)
            {
                animator.SetBool(isGroundID, true);
            }
        }
        else
        {
            if (characterFallOutDeltaTime > 0.0f)  //土狼時間
            {
                characterFallOutDeltaTime = Mathf.Max(0.0f, characterFallOutDeltaTime - Time.deltaTime);
                isOnGround = characterFallOutDeltaTime > 0.0f;
                if (!isOnGround && animator != null)
                {
                    animator.SetBool(isGroundID, false);
                }
            }
            else
            {
                isOnGround = false;
                if (animator != null)
                {
                    animator.SetBool(isGroundID, false);
                }
            }
        }

        if (verticalSpeed > -maxVerticalSpeed)
        {
            verticalSpeed += characterGravity * Time.deltaTime;
        }
    }

    /*
    [SerializeField] protected float slopRayExtent;
    protected Vector3 ResetMoveDirectionOnSlop(Vector3 dir)
    {
        if (Physics.Raycast(transform.position, -Vector3.up, out var hit, slopRayExtent))
        {
            float newAnle = Vector3.Dot(Vector3.up, hit.normal);
            if (newAnle != 0 && verticalSpeed <= 0)
            {
                return Vector3.ProjectOnPlane(dir, hit.normal);
            }
        }
        return dir;
    }
    */

    public virtual void CharacterMoveInterface(Vector3 moveDirection, float moveSpeed, bool useGravity)
    {
        if (control == null || !control.enabled) return;

        movementDirection = moveDirection.normalized;

        //movementDirection = ResetMoveDirectionOnSlop(movementDirection);

        if (useGravity)
        {
            verticalDirection.Set(0.0f, verticalSpeed, 0.0f);
        }
        else
        {
            verticalDirection = Vector3.zero;
        }

        control.Move((moveSpeed * Time.deltaTime)
            * movementDirection + Time.deltaTime
            * verticalDirection);
    }
}
