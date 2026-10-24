using UnityEngine;

/// <summary>
/// 骷髏法師：3D 死靈魔法飛彈（含戰鬥傷害判定與防誤傷過濾）
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class SkeletonMageProjectile3D : MonoBehaviour
{
    [Header("=== 測試模式 (正式放入 Prefab 給法師用時可取消勾選) ===")]
    public bool autoLaunchOnStart = false; 
    public float testShootDistance = 15.0f;

    [Header("=== 戰鬥與傷害設定 ===")]
    public float damage = 25.0f;
    public bool useAreaDamage = false;     // true = 爆炸範圍傷害(AOE), false = 單體直接命中傷害
    public float explosionRadius = 2.5f;   // AOE 爆炸半徑
    public LayerMask hitLayers = ~0;       // 建議在 Inspector 勾選 Player 與 Ground/Obstacle
    public string targetTag = "Player";    // 攻擊目標 Tag

    [Header("=== 3D 彈道與雙螺旋參數 ===")]
    public float projectileSpeed = 16.0f;
    public float arcHeight = 2.8f;
    public float helixSpinSpeed = 1260.0f;
    public float helixRadius = 0.55f;

    [Header("=== 3D 子物件參照 ===")]
    public Transform helixSpinnerPivot;
    public TrailRenderer trailHelixA;
    public TrailRenderer trailHelixB;
    public ParticleSystem worldEmbersPS;
    public Light spellPointLight;
    public GameObject impactVfx3DPrefab;

    private GameObject _caster;            // 記錄是哪隻骷髏法師射出的，避免撞到自己
    private Vector3 _startPos;
    private Vector3 _controlPos;
    private Vector3 _targetPos;
    private float _flightDuration;
    private float _elapsed;
    private bool _isLaunched;

    private void Start()
    {
        if (!_isLaunched && autoLaunchOnStart)
        {
            Vector3 defaultTarget = transform.position + transform.forward * testShootDistance;
            Initialize3DProjectile(defaultTarget, null, damage, 0f);
        }
    }

    /// <summary>
    /// 由骷髏法師呼叫的初始化函式
    /// </summary>
    /// <param name="targetPosition">目標座標（建議傳入玩家座標 + Vector3.up * 1.0f 瞄準胸口）</param>
    /// <param name="caster">發射者（骷髏法師自己）</param>
    /// <param name="customDamage">自訂傷害值（例如從 AttackData 傳入）</param>
    /// <param name="sideCurveOffset">左右側旋弧度（0 = 正上方拋物線，正負值 = 左右包夾弧線）</param>
    public void Initialize3DProjectile(Vector3 targetPosition, GameObject caster = null, float customDamage = 25f, float sideCurveOffset = 0f)
    {
        _caster = caster;
        damage = customDamage;
        _startPos = transform.position;
        _targetPos = targetPosition;

        float dist = Vector3.Distance(_startPos, _targetPos);
        _flightDuration = Mathf.Max(0.1f, dist / projectileSpeed);

        // 計算 3D 二階貝茲曲線控制點
        Vector3 mid = (_startPos + _targetPos) * 0.5f;
        Vector3 dir = (_targetPos - _startPos).normalized;
        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
        _controlPos = mid + Vector3.up * arcHeight + right * sideCurveOffset;

        if (trailHelixA != null)
        {
            trailHelixA.transform.localPosition = new Vector3(helixRadius, 0f, 0f);
            trailHelixA.Clear();
        }
        if (trailHelixB != null)
        {
            trailHelixB.transform.localPosition = new Vector3(-helixRadius, 0f, 0f);
            trailHelixB.Clear();
        }

        _elapsed = 0f;
        _isLaunched = true;
    }

    private void Update()
    {
        if (!_isLaunched) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _flightDuration);

        // 1. 計算 3D 貝茲曲線位置與朝向
        float invT = 1f - t;
        Vector3 nextPos = (invT * invT) * _startPos
                        + (2f * invT * t) * _controlPos
                        + (t * t) * _targetPos;

        Vector3 velocityDir = nextPos - transform.position;
        if (velocityDir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocityDir.normalized, Vector3.up);
        }
        transform.position = nextPos;

        // 2. 旋轉雙螺旋樞紐
        if (helixSpinnerPivot != null)
        {
            helixSpinnerPivot.Rotate(Vector3.forward, helixSpinSpeed * Time.deltaTime, Space.Self);
            float envelope = Mathf.Sin(t * Mathf.PI);
            helixSpinnerPivot.localScale = new Vector3(envelope, envelope, 1f);
        }

        // 飛抵目標點時引爆
        if (t >= 1f)
        {
            Trigger3DImpact(_targetPos, Vector3.up, null);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isLaunched) return;

        // 1. 忽略發射者自己（以及發射者身上的子 Collider）
        if (_caster != null && (other.gameObject == _caster || other.transform.IsChildOf(_caster.transform)))
            return;

        // 2. 忽略其他觸發器區域（例如生怪區、警戒範圍 Trigger），除非它是玩家本體
        if (other.isTrigger && !other.CompareTag(targetTag))
            return;

        // 3. 檢查 LayerMask 是否在允許命中的圖層內
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        Trigger3DImpact(transform.position, -transform.forward, other);
    }

    private void Trigger3DImpact(Vector3 hitPoint, Vector3 hitNormal, Collider directHitCollider)
    {
        _isLaunched = false;

        // === 處理傷害邏輯 ===
        if (useAreaDamage)
        {
            // 範圍爆炸傷害 (AOE)：適合飛彈落地後炸傷周圍玩家
            Collider[] hits = Physics.OverlapSphere(hitPoint, explosionRadius, hitLayers);
            foreach (Collider hit in hits)
            {
                if (hit.CompareTag(targetTag))
                {
                    ApplyDamageToTarget(hit.gameObject);
                    break; // 確保同一個玩家只扣一次血
                }
            }
        }
        else if (directHitCollider != null && directHitCollider.CompareTag(targetTag))
        {
            // 單體直擊傷害
            ApplyDamageToTarget(directHitCollider.gameObject);
        }

        // === 生成命中特效 ===
        if (impactVfx3DPrefab != null)
        {
            Instantiate(impactVfx3DPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        // === 脫離雙螺旋拖尾與粒子，避免瞬間消失 ===
        if (helixSpinnerPivot != null)
        {
            if (trailHelixA != null) trailHelixA.emitting = false;
            if (trailHelixB != null) trailHelixB.emitting = false;
            helixSpinnerPivot.SetParent(null);
            Destroy(helixSpinnerPivot.gameObject, 0.55f);
        }
        if (worldEmbersPS != null)
        {
            worldEmbersPS.transform.SetParent(null);
            worldEmbersPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(worldEmbersPS.gameObject, 1.5f);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 實際扣血接口：在此換成你專案裡的生命/戰鬥腳本！
    /// </summary>
    private void ApplyDamageToTarget(GameObject targetObj)
    {
        // TODO: 換成你角色的受傷方法，例如：
        // if (targetObj.TryGetComponent<CharacterCombatBase>(out var combat))
        // {
        //     combat.TakeDamage(damage);
        // }
        Debug.Log($"[死靈魔彈] 命中 {targetObj.name}，造成 {damage} 點魔法傷害！");
    }

    // 在 Scene 視窗畫出爆炸範圍方便微調
    private void OnDrawGizmosSelected()
    {
        if (useAreaDamage)
        {
            Gizmos.color = new Color(0.6f, 0.1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}