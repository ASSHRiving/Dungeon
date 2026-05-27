using UnityEngine;
using MoveBase;

public class PlayerMovementSystem : CharacterMovementBase
{
    private float targetRotation;
    private float rotationVelocity;
    private Transform characterCamera;
    private float rotationLerpTime = 0.1f;
    private float moveDirctionSlerpTime = 15;

    [SerializeField, Header("行走速度")] private float walkSpeed;
    [SerializeField, Header("奔跑速度")] private float runSpeed;

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
        return isOnGround && _animator.CheckAnimationTag("Motion");
    }

    private bool CanRunControl()
    {
        if (Vector3.Dot(movementDirection.normalized, transform.forward) < 0.75f) return false;
        if (!CanMoveControl()) return false;
       
        return true;
    }

    private void PlayerMoveDirection()
    {
        
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
                movementDirection = Vector3.Slerp(movementDirection, ResetMoveDirectionOnSlop(targetDirection), moveDirctionSlerpTime * Time.deltaTime);
            }
        }
        else 
        {
            movementDirection = Vector3.zero;
        }
            
        control.Move((characterCurrentMoveSpeed * Time.deltaTime)
            * movementDirection.normalized + Time.deltaTime
            * new Vector3(0.0f, verticalSpeed, 0.0f));
        
    }

    private void UpdateMotionAnimation()
    {
        if (CanRunControl())
        {
            _animator.SetFloat(speedID, _inputSystem.playerMovement.magnitude * (_inputSystem.playerRun? 2f : 1f), 0.1f, Time.deltaTime);

            characterCurrentMoveSpeed = _inputSystem.playerRun? runSpeed : walkSpeed;
        }
        else
        {
            _animator.SetFloat(speedID, 0f, 0.1f, Time.deltaTime);
            characterCurrentMoveSpeed = 0f;
        }

        _animator.SetFloat(runID, _inputSystem.playerRun? 1f : 0f);
    }
    private void UpdateRollAnimation()
    {
        if (_inputSystem.playerRoll)
        {
            _animator.SetTrigger(rollId);
        }
        if(_animator.CheckAnimationTag("Roll"))
        {
            CharacterMoveInterface(-transform.forward, _animator.GetFloat(animationMoveID), true);
        }
    }
}
