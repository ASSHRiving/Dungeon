using UnityEngine;
using System.Collections;

public class PlayerMovementSystem : CharacterMovementBase
{
    private float targetRotation;
    private float rotationVelocity;
    private Transform characterCamera;
    private float rotationLerpTime = 0.1f;
    private float moveDirctionSlerpTime = 20;
    private Vector3 rollDirection;

    //閃避系統
    //private bool canRoll = true;


    [SerializeField, Header("行走速度")] public float walkSpeed;
    [SerializeField, Header("奔跑速度")] public float runSpeed;
    protected override void Awake()
    {
        base.Awake();
        characterCamera = Camera.main.transform;
        
    }

    protected override void Update()
    {
        base.Update();

        PlayerMoveDirection();    
    }

    private void LateUpdate()
    {
        UpdateMotionAnimation();
        UpdateRollAnimation();
    }

    private bool CanMoveControl()
    {
        return isOnGround && (animator.CheckAnimationTag("Motion") ||(animator.CheckAnimationTag("Attack") && _combat.canAttack)
            );
    }

    private bool CanRunControl()
    {
        if (Vector3.Dot(movementDirection.normalized, transform.forward) < 0.75f) return false;
        if (!CanMoveControl()) return false;
       
        return true;
    }

    private void PlayerMoveDirection()
    {
        if(characterCamera == null)
        {
            if(Camera.main != null)
            {
                characterCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("找不到主相機，請確保場景中有一個標記為 MainCamera 的相機。");
                return;
            }
        }
        
        if (isOnGround && _inputSystem.playerMovement == Vector2.zero)
            movementDirection = Vector3.zero;
        
        if(CanMoveControl()) 
        {
            if(_inputSystem.playerMovement != Vector2.zero){
                // 1. 抓取相機在世界座標的正前方與正右方
                Vector3 camForward = characterCamera.forward;
                Vector3 camRight = characterCamera.right;

                // 2. 重要：把 Y 軸（高度）歸零！我們只需要水平面的方向
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                // 3. 根據玩家的輸入（X 是左右，Y 是前後），直接計算出世界座標的移動方向
                Vector3 targetDirection = camForward * _inputSystem.playerMovement.y + camRight * _inputSystem.playerMovement.x;

                // 4. 計算出這個方向對應的 3D 角度（讓角色轉身用）
                targetRotation = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

                // 5. 讓角色平滑轉向目標角度
                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationLerpTime);

                // 6. 這裡直接把我們算好的精準方向餵給 movementDirection
                movementDirection = Vector3.Slerp(movementDirection, targetDirection, moveDirctionSlerpTime * Time.deltaTime);
                rollDirection = movementDirection;
            }
        }
        else 
        {
            movementDirection = Vector3.zero;
        }
        if (!animator.CheckAnimationTag("Roll"))
        {  
            control.Move((characterCurrentMoveSpeed * Time.deltaTime)
                * movementDirection.normalized + Time.deltaTime
                * new Vector3(0.0f, verticalSpeed, 0.0f));
        }
    }

    private void UpdateMotionAnimation()
    {
        if (CanRunControl())
        {
            float targetSpeed = _inputSystem.playerMovement.magnitude * (_inputSystem.playerRun ? 2f : 1f);
            animator.SetFloat(speedID, targetSpeed, 0.1f, Time.deltaTime);
            
            characterCurrentMoveSpeed = _inputSystem.playerRun? runSpeed : walkSpeed;
        }
        else
        {
            animator.SetFloat(speedID, 0f, 0.1f, Time.deltaTime);
            if(animator.GetFloat(speedID) < 0.001f)
            {
                animator.SetFloat(speedID, 0f);
            }
            characterCurrentMoveSpeed = 0f;
        }

        animator.SetFloat(runID, _inputSystem.playerRun? 1f : 0f);
    }
    private void UpdateRollAnimation()
    {
        // 按下翻滾鍵，且當前不在翻滾中
        if (_inputSystem.playerRoll && !animator.CheckAnimationTag("Roll"))
        {
            // 1. 決定這次翻滾的精確方向與目標角度
            if (_inputSystem.playerMovement != Vector2.zero)
            {
                Vector3 camForward = characterCamera.forward;
                Vector3 camRight = characterCamera.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                // 根據玩家當前的搖桿輸入決定翻滾方向
                rollDirection = (camForward * _inputSystem.playerMovement.y + camRight * _inputSystem.playerMovement.x).normalized;
                targetRotation = Mathf.Atan2(rollDirection.x, rollDirection.z) * Mathf.Rad2Deg;
            }
            else
            {
                // 沒推搖桿時，朝角色當前正前方翻滾
                rollDirection = transform.forward;
                targetRotation = transform.eulerAngles.y;
            }

            // 2. 觸發動畫與無敵幀
            animator.SetTrigger(rollId);
            /*
            StartCoroutine(RollRoutine());
            _combat.inAttack = false;
            _combat.canAttack = true;
            */
        }

        // 翻滾動畫播放期間
        if (animator.CheckAnimationTag("Roll"))
        {
            animator.ResetTrigger(rollId);

            //強制轉向
            transform.eulerAngles = targetRotation * Vector3.up;

            // 執行翻滾位移
            CharacterMoveInterface(rollDirection, animator.GetFloat(animationMoveID), true);
        }
    }
    /// <summary>
    /// 當動畫正式進入翻滾（Roll）狀態時由 StateMachineBehaviour 調用
    /// </summary>
    public void OnRollEnter()
    {
        // 2. 開啟無敵幀協程
        StartCoroutine(RollRoutine());
    }
    private IEnumerator RollRoutine()
    {
        immune = true;
        yield return new WaitForSeconds(immuneTime);
        immune = false;
    }
}
//new Vector3(_inputSystem.playerMovement.x, 0, _inputSystem.playerMovement.y)

