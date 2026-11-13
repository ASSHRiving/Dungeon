using UnityEngine;

/// <summary>
/// 骷髏法師：3D 死靈魔法飛彈 (繼承自 Projectile)
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class SkeletonMageProjectile3D : Projectile
{
    [Header("=== 3D 雙螺旋與弧線參數 ===")]
    [Tooltip("若設為 0 則為直線飛行；大於 0 則為拋物線弧線飛行")]
    public float arcHeight = 1.8f;
    public float sideCurveOffset = 0f;     // 左右弧度偏移
    public float helixSpinSpeed = 1260.0f; // 每秒繞 Z 軸旋轉角度
    public float helixRadius = 0.55f;

    [Header("=== 3D 子物件參照 ===")]
    public Transform helixSpinnerPivot;    // 繞 Local Z 軸高速旋轉的樞紐
    public TrailRenderer trailHelixA;      // 子物件 A (LocalPos: +helixRadius, 0, 0)
    public TrailRenderer trailHelixB;      // 子物件 B (LocalPos: -helixRadius, 0, 0)
    public ParticleSystem worldEmbersPS;   // Simulation Space = World 的餘燼粒子
    public Light spellPointLight;          // 動態 3D 點光源
    public GameObject impactVfx3DPrefab;   // 3D 命中爆發 Prefab

    private Vector3 _startPos;
    private Vector3 _controlPos;
    private Vector3 _targetPos;
    private float _flightDuration;
    private float _elapsed;
    private bool _hasImpacted;

    public override void Setup(Transform _attacker, Transform _target, AttackData _attackData)
    {
        base.Setup(_attacker, _target, _attackData);

        Vector3 aimPos = _target != null ? _target.position + Vector3.up * .5f : transform.position + transform.forward * 15f;

        InitTrajectory(aimPos);
    }

    private void InitTrajectory(Vector3 targetPosition)
    {
        _startPos = transform.position;
        _targetPos = targetPosition;
        float dist = Vector3.Distance(_startPos, _targetPos);
        _flightDuration = Mathf.Max(0.1f, dist / Mathf.Max(1f, speed));

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
        _hasImpacted = false;
    }

    protected override void UpdateProjectile()
    {
        if (_hasImpacted) return;

        // 1. 飛行位移：支援拋物線弧線 (arcHeight > 0) 或傳統直線推進
        if (arcHeight > 0.01f || Mathf.Abs(sideCurveOffset) > 0.01f)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _flightDuration);

            float invT = 1f - t;
            Vector3 nextPos = (invT * invT) * _startPos
                            + (2f * invT * t) * _controlPos
                            + (t * t) * _targetPos;

            Vector3 velocityDir = nextPos - transform.position;
            if (velocityDir.sqrMagnitude > 0.00001f)
            {
                transform.rotation = Quaternion.LookRotation(velocityDir.normalized, Vector3.up);
            }
            transform.position = nextPos;

            // 旋轉雙螺旋樞紐 + 包絡線收尾
            if (helixSpinnerPivot != null)
            {
                helixSpinnerPivot.Rotate(Vector3.forward, helixSpinSpeed * Time.deltaTime, Space.Self);
                float envelope = Mathf.Sin(t * Mathf.PI);
                helixSpinnerPivot.localScale = new Vector3(envelope, envelope, 1f);
            }

            /*
            // 飛抵目標座標落地時自動引爆
            if (t >= 1f)
            {
                OnProjectileHit(_targetPos, Vector3.up);
            }
            */
        }
        else
        {
            // 直線飛行模式 (直接呼叫父類別 Projectile 的直線移動)
            base.UpdateProjectile();

            if (helixSpinnerPivot != null)
            {
                helixSpinnerPivot.Rotate(Vector3.forward, helixSpinSpeed * Time.deltaTime, Space.Self);
                helixSpinnerPivot.localScale = Vector3.one;
            }
        }
    }

    protected override void OnProjectileHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_hasImpacted) return;
        _hasImpacted = true;

        // 1. 生成 3D 命中爆發特效
        if (impactVfx3DPrefab != null)
        {
            GameObject impactVfx = Instantiate(impactVfx3DPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(impactVfx, 2.0f);
        }

        // 2. 將雙螺旋 Trail 與世界粒子脫離父物件，避免 Destroy 時拖尾瞬間消失
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

        // 3. 呼叫父類別銷毀飛彈本體
        base.OnProjectileHit(hitPoint, hitNormal);
    }
}