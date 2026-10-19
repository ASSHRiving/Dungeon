using UnityEngine;


public abstract class Weapon : MonoBehaviour
{
    [Header("基礎設定")]
    public string weaponName = "武器名稱";
    public float damage = 0f;
    public int weaponType = 0;
    public int combo = 0;
    [Header("移動速度")]
    public float runSpeed = 7;
    public float walkSpeed = 3;

    [Header("攻擊檢測")]
    public Transform attackPoint;
    public float attackRadius;
    
    public SoundAssetsType weaponSoundType = SoundAssetsType.Sword;
    public AnimatorOverrideController overrideController;

    public TrailRenderer[] weaponTrails;
    public ParticleSystem weaponEffect;

    protected virtual void Start()
    {
        if(weaponEffect != null) weaponEffect.Stop();
    }

    public abstract void Attack(Animator anim);

    [Header("=== 神聖受擊反饋 (Hit Impact & Hit Stop) ===")]
    [SerializeField] private GameObject hitImpactPrefab;     // 拖入剛生成的 VFX_HolyHitImpact.prefab
    [SerializeField] private Transform bladeStartRef;        // 劍顎起點 (0.213m)
    [SerializeField] private Transform bladeTipRef;          // 劍尖頂點 (0.965m)
    [SerializeField] private Light gemPointLight;            // 選填：護手寶石下的 Point Light
    [SerializeField] private float hitStopDuration = 0.045f; // 命中頓幀時間 (約 3 幀，打擊感極佳)

    private Coroutine _hitStopCoroutine;

    /// <summary>
    /// 當您的戰鬥判定 (Hitbox / OnTriggerEnter) 確認命中敵人 Collider 時呼叫此方法
    /// </summary>
    public void SpawnHitImpact(Collider enemyCollider)
    {
        if (enemyCollider == null) return;

        // 1. 計算劍刃中上段 (約 0.65m 處) 的世界座標，並找出敵人碰撞體表面最近的接觸點
        Vector3 bladeSamplePos = (bladeStartRef != null && bladeTipRef != null)
            ? Vector3.Lerp(bladeStartRef.position, bladeTipRef.position, 0.6f)
            : transform.position;

        Vector3 hitPoint = enemyCollider.ClosestPoint(bladeSamplePos);

        // 2. 計算受擊噴濺法線方向 (由敵人中心指向受擊點，讓結晶碎屑朝外噴濺)
        Vector3 hitNormal = (hitPoint - enemyCollider.bounds.center).normalized;
        if (hitNormal.sqrMagnitude < 0.001f) hitNormal = -transform.forward;

        SpawnHitImpactAtPoint(hitPoint, hitNormal);
    }

    /// <summary>
    /// 若您的戰鬥系統使用 Raycast / SphereCast，可直接傳入 RaycastHit.point 與 normal
    /// </summary>
    public void SpawnHitImpactAtPoint(Vector3 hitPoint, Vector3 hitNormal)
    {
        // 生成 VFX_HolyHitImpact 特效
        if (hitImpactPrefab != null)
        {
            Instantiate(hitImpactPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
        }

        // 觸發微頓幀 (Hit Stop)
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
        _hitStopCoroutine = StartCoroutine(HitStopRoutine());
    }

    private System.Collections.IEnumerator HitStopRoutine()
    {
        if (gemPointLight != null) gemPointLight.intensity = 4.5f; // 瞬間照亮周圍地牢

        yield return new WaitForSecondsRealtime(hitStopDuration);

        if (gemPointLight != null) gemPointLight.intensity = 0.3f;
        _hitStopCoroutine = null;
    }

}
