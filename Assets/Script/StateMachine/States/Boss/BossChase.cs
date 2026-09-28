using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BossChase", menuName = "StateMachine/States/Boss/BossChase")]
public class BossChase : StateActionSO
{
    [Header("距離設定")]
    [SerializeField] private float minDistance = 2.4f;      // 太近界線 (低於此距離後退)
    [SerializeField] private float maxDistance = 6.6f;      // 太遠界線 (高於此距離追擊)

    [Header("移動速度")]
    [SerializeField] private float moveSpeed = 1.4f;
    [SerializeField] private float strafeSpeed = 1.4f;

    [Header("計時設定")]
        [SerializeField] private float strafeChangeInterval = 1.5f; // 每 1.5 秒換一次左右方向

        [Header("路徑計算")]
        [SerializeField] private float pathRecalcInterval = 0.5f; // 路徑重算間隔

        private NavMeshPath _cachedPath;
        private float _pathRecalcTimer;
        private Vector3 _currentDestination;
        private bool _hasValidPath;

        public override void OnEnter(StateMachineSystem stateMachineSystem)
        {
            Animator animator = stateMachineSystem.animator;
            CharacterController controller = stateMachineSystem.characterController;
        
            if (animator != null)
            {
                animator.SetFloat(lockOnID, 1);
                animator.Play("Ready");
            }

            _cachedPath = new NavMeshPath();
            _pathRecalcTimer = 0f;
            _hasValidPath = false;

            stateMachineSystem.strafeTimer = 0f;
            stateMachineSystem.attackTimer = 0f;
            stateMachineSystem.UpdateRandomHorizontal();
        }
    public override void OnUpdate(StateMachineSystem stateMachineSystem)
        {
            EnemyCombatSystem combat = stateMachineSystem.combat;
            Animator animator = stateMachineSystem.animator;
            CharacterController controller = stateMachineSystem.characterController;
            EnemyMovementSystem movement = stateMachineSystem.movement;

            if (combat == null || animator == null || controller == null || movement == null || combat.GetCurrentTarget() == null) return;

            Transform targetTransform = combat.GetCurrentTarget();
            Transform selfTransform = stateMachineSystem.transform;
            float distance = combat.GetCurrentTargetDistance();

            stateMachineSystem.strafeTimer += Time.deltaTime;
            stateMachineSystem.attackTimer += Time.deltaTime;
            _pathRecalcTimer += Time.deltaTime;

            // 旋轉朝向目標
            if (animator.CheckAnimationTag("Motion"))
            {
                selfTransform.rotation = stateMachineSystem.transform.LockOnTarget(targetTransform, selfTransform, 10f);
            }
            else if ((animator.CheckAnimationTag("Attack") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.4f) || animator.CheckAnimationTag("Aim"))
            {
                selfTransform.rotation = stateMachineSystem.transform.LockOnTarget(targetTransform, selfTransform, 10f);
            }

            // 嘗試使用技能
            if (animator.CheckAnimationTag("Motion"))
            {
                AbilityBase readySkill = stateMachineSystem.SelectReadySkill(distance);
                if (readySkill != null)
                {
                    stateMachineSystem.UseSkill(readySkill);
                    return;
                }
            }

            if (animator.CheckAnimationTag("Motion"))
            {
                float currentSpeed = moveSpeed;
                bool shouldMove = true;

                // 近距離後退
                if (distance < minDistance)
                {
                    currentSpeed = moveSpeed;
                    Vector3 retreatDir = (selfTransform.position - targetTransform.position).normalized;
                    _currentDestination = selfTransform.position + retreatDir * 2f;
                
                    animator.SetFloat(verticalID, -1f, 0.25f, Time.deltaTime);
                    animator.SetFloat(horizontalID, 0f, 0.25f, Time.deltaTime);
                }
                // 中距離徘徊
                else if (distance >= minDistance && distance <= maxDistance)
                {
                    currentSpeed = strafeSpeed;
                
                    if (stateMachineSystem.strafeTimer >= strafeChangeInterval)
                    {
                        stateMachineSystem.UpdateRandomHorizontal();
                        stateMachineSystem.strafeTimer = 0f;
                    }

                    Vector3 strafeDir = selfTransform.right * stateMachineSystem.randomHorizontal;
                    _currentDestination = selfTransform.position + strafeDir * 2f;
                
                    animator.SetFloat(verticalID, 0f, 0.25f, Time.deltaTime);
                    animator.SetFloat(horizontalID, stateMachineSystem.randomHorizontal, 0.25f, Time.deltaTime);
                }
                // 遠距離追擊
                else if (distance > maxDistance + 0.1f)
                {
                    currentSpeed = moveSpeed;
                    _currentDestination = targetTransform.position;

                    animator.SetFloat(verticalID, 1f, 0.25f, Time.deltaTime);
                    animator.SetFloat(horizontalID, 0f, 0.25f, Time.deltaTime);

                    stateMachineSystem.strafeTimer = 0f;
                }

                // 計算路徑並移動
                if (shouldMove)
                {
                    UpdatePathAndMove(stateMachineSystem, currentSpeed);
                }
            }
            else
            {
                // 停止移動
                movement.CharacterMoveInterface(Vector3.zero, 0f, false);
            
                animator.SetFloat(verticalID, 0f);
                animator.SetFloat(horizontalID, 0f);
                animator.SetFloat(runID, 0f);
            }
        }

        private void UpdatePathAndMove(StateMachineSystem stateMachineSystem, float currentSpeed)
        {
            CharacterController controller = stateMachineSystem.characterController;
            EnemyMovementSystem movement = stateMachineSystem.movement;
            Transform selfTransform = stateMachineSystem.transform;

            // 定期重算路徑
            if (_pathRecalcTimer >= pathRecalcInterval || !_hasValidPath)
            {
                _hasValidPath = NavMesh.CalculatePath(selfTransform.position, _currentDestination, NavMesh.AllAreas, _cachedPath);
                _pathRecalcTimer = 0f;
            }

            if (_hasValidPath && _cachedPath.corners.Length > 0)
            {
                // 取得下一個路徑點
                Vector3 nextCorner = _cachedPath.corners[0];
            
                // 如果第一個點太近，取第二個點
                if (_cachedPath.corners.Length > 1 && Vector3.Distance(selfTransform.position, nextCorner) < 0.5f)
                {
                    nextCorner = _cachedPath.corners[1];
                }

                Vector3 moveDir = (nextCorner - selfTransform.position).normalized;
                moveDir.y = 0; // 確保水平移動
            
                if (moveDir != Vector3.zero)
                {
                    // 使用 CharacterMovementBase 的移動介面
                    movement.CharacterMoveInterface(moveDir, currentSpeed, true);
                }
            }
            else
            {
                // 沒有有效路徑時直接朝目標方向移動
                Vector3 directDir = (_currentDestination - selfTransform.position).normalized;
                directDir.y = 0;
            
                if (directDir != Vector3.zero)
                {
                    movement.CharacterMoveInterface(directDir, currentSpeed, true);
                }
            }
        }

        public override void OnExit(StateMachineSystem stateMachineSystem)
        {
            // 離開狀態時停止移動
            EnemyMovementSystem movement = stateMachineSystem.movement;
            if (movement != null)
            {
                movement.CharacterMoveInterface(Vector3.zero, 0f, false);
            }
        }
}
