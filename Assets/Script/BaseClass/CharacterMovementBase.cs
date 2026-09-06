using UnityEngine;
public abstract class CharacterMovementBase : MonoBehaviour
{
    protected Animator _animator;
    protected CharacterController control;
    protected CharacterInputSystem _inputSystem;
    protected CharacterCombatBase _combat;

    [SerializeField,Header("移動速度")] protected float characterGravity;
    [SerializeField] protected float characterCurrentMoveSpeed;
    protected float characterFallTime = 0.15f;
    protected float characterFallOutDeltaTime;
    protected float verticalSpeed; 
    protected float maxVerticalSpeed = 53f;

    [SerializeField, Header("地面檢測")] protected LayerMask whatIsGround;
    [SerializeField] protected float groundDetectionOffset = 0.1f;
    [SerializeField] protected float groundDetectionRang = 0.2f;
    [SerializeField] protected float slopRayExtent;
    [SerializeField, Header("障礙物檢測")] protected LayerMask whatIsObs;
    [SerializeField] protected bool isOnGround;

    //AnimationID
    protected int animationMoveID = Animator.StringToHash("AnimationMove");
    protected int speedID = Animator.StringToHash("Speed");
    protected int horizontalID = Animator.StringToHash("Horizontal");
    protected int verticalID = Animator.StringToHash("Vertical");
    protected int runID = Animator.StringToHash("Run");
    protected int rollId = Animator.StringToHash("Roll");


    //移動向量
    protected Vector3 movementDirection;
    protected Vector3 verticalDirection;

    //閃避
    public bool immune { get; protected set; }
    protected float immuneTime = 0.3f;

    protected virtual void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        control = GetComponent<CharacterController>();
        _inputSystem = GetComponent<CharacterInputSystem>();
        _combat = GetComponentInChildren<CharacterCombatBase>();
    }
    protected virtual void Start()
    {
        characterFallOutDeltaTime = characterFallTime;
    }
    protected virtual void Update()
    {
        CheckOnGround();
        CharacterGravity();
    }

    protected bool CanAnimationMotion(Vector3 dir)
    {
        return Physics.Raycast(transform.position + transform.up * .5f, dir.normalized * _animator.GetFloat(animationMoveID), out var hit, 1f,whatIsObs);
    }

    /// <summary>
    /// 地面檢測
    /// </summary>
    private void CheckOnGround()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundDetectionOffset, transform.position.z);
        isOnGround = Physics.CheckSphere(spherePosition, groundDetectionRang, whatIsGround, QueryTriggerInteraction.Ignore);
        
    }

    private void CharacterGravity()
    {
        if (isOnGround)
        {
            
            characterFallOutDeltaTime = characterFallTime;

            if (verticalSpeed < 0.0f)
            {
                verticalSpeed = -2f;
            }
        }
        else
        {
            if (characterFallOutDeltaTime >= 0.0f)  //土狼時間
            {
                characterFallOutDeltaTime -= Time.deltaTime;
                characterFallOutDeltaTime = Mathf.Clamp(characterFallOutDeltaTime, 0, characterFallTime);
            }
        }

        if (verticalSpeed < maxVerticalSpeed)
        {
            verticalSpeed += characterGravity * Time.deltaTime;
        }
    }

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
    
    public virtual void CharacterMoveInterface(Vector3 moveDirection, float moveSpeed, bool useGravity)
    {
        if (!CanAnimationMotion(moveDirection))
        {
            movementDirection = moveDirection.normalized;
    
            movementDirection = ResetMoveDirectionOnSlop(movementDirection);

            if (useGravity)
            {
                verticalDirection.Set(0.0f, verticalSpeed, 0.0f);
            }
            else
            {
                verticalDirection = Vector3.zero;
            }
        
            control.Move((moveSpeed * Time.deltaTime)
                * movementDirection.normalized + Time.deltaTime
                * verticalDirection);
        }
    }

    private void OnDrawGizmosSelected()
        {
            
            if (isOnGround) 
                Gizmos.color = Color.green;
            else 
                Gizmos.color = Color.red;

            Vector3 position = Vector3.zero;
            
            position.Set(transform.position.x, transform.position.y - groundDetectionOffset,
                transform.position.z);

            Gizmos.DrawWireSphere(position, groundDetectionRang);
            
        }
}
