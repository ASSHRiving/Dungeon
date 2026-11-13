using UnityEngine;
using UnityEngine.Serialization;

public abstract class Weapon : MonoBehaviour, IItem
{
    [Header("ScriptableObject 規格 (優先讀取)")]
    [SerializeField] protected WeaponDataSO weaponData;
    public WeaponDataSO WeaponData => weaponData;

    [Header("基礎設定 (舊版 Inspector 設定 / SO 為空時之 Fallback)")]
    [FormerlySerializedAs("weaponName")]
    [SerializeField] protected string _weaponName = "武器名稱";
    [FormerlySerializedAs("itemIcon")]
    [SerializeField] protected Sprite _itemIcon;
    [FormerlySerializedAs("damage")]
    [SerializeField] protected float _damage = 0f;
    [FormerlySerializedAs("weaponType")]
    [SerializeField] protected int _weaponType = 0;
    public int combo = 0;

    public string weaponName
    {
        get => weaponData != null ? weaponData.weaponName : _weaponName;
        set => _weaponName = value;
    }

    public Sprite itemIcon
    {
        get => weaponData != null ? weaponData.itemIcon : _itemIcon;
        set => _itemIcon = value;
    }

    public float damage
    {
        get => weaponData != null ? weaponData.damage : _damage;
        set => _damage = value;
    }

    public int weaponType
    {
        get => weaponData != null ? weaponData.weaponType : _weaponType;
        set => _weaponType = value;
    }

    public string ItemName => weaponName;
    public Sprite ItemIcon => itemIcon;

    public virtual string GetTooltipStatsText()
    {
        if (weaponData != null)
        {
            return weaponData.GetTooltipStatsText();
        }
        return damage > 0 ? $"攻擊力: +{Mathf.RoundToInt(damage)}" : "無附加屬性";
    }

    [Header("移動速度 (舊版相容 / SO 未配置時使用)")]
    public float runSpeed = 7f;
    public float walkSpeed = 3f;

    [Header("攻擊檢測")]
    public Transform attackPoint;
    public float attackRadius;

    [FormerlySerializedAs("weaponSoundType")]
    [SerializeField] protected SoundAssetsType _weaponSoundType = SoundAssetsType.Sword;
    public SoundAssetsType weaponSoundType
    {
        get => weaponData != null ? weaponData.weaponSoundType : _weaponSoundType;
        set => _weaponSoundType = value;
    }

    [FormerlySerializedAs("overrideController")]
    [SerializeField] protected AnimatorOverrideController _overrideController;
    public AnimatorOverrideController overrideController
    {
        get => weaponData != null ? weaponData.overrideController : _overrideController;
        set => _overrideController = value;
    }

    public TrailRenderer[] weaponTrails;
    public ParticleSystem weaponEffect;

    protected virtual void Start()
    {
        if (weaponEffect != null) weaponEffect.Stop();
    }

    public abstract void Attack(Animator anim);

    [Header("=== 打擊反饋 (Hit Impact & Hit Stop) ===")]
    [FormerlySerializedAs("hitImpactPrefab")]
    [SerializeField] private GameObject _hitImpactPrefab;     // 拖入剛生成的 VFX_HolyHitImpact.prefab
    public GameObject hitImpactPrefab
    {
        get => weaponData != null && weaponData.hitImpactPrefab != null ? weaponData.hitImpactPrefab : _hitImpactPrefab;
        set => _hitImpactPrefab = value;
    }

    [SerializeField] private Transform bladeStartRef;        // 劍顎起點 (0.213m)
    [SerializeField] private Transform bladeTipRef;          // 劍尖頂點 (0.965m)
    [SerializeField] private Light gemPointLight;            // 選填：護手寶石下的 Point Light

    [FormerlySerializedAs("hitStopDuration")]
    [SerializeField] private float _hitStopDuration = 0.045f; // 命中頓幀時間 (約 3 幀，打擊感極佳)
    public float hitStopDuration
    {
        get => weaponData != null ? weaponData.hitStopDuration : _hitStopDuration;
        set => _hitStopDuration = value;
    }

    private Coroutine _hitStopCoroutine;

    /// <summary>
    /// 當戰鬥判定 (Hitbox / OnTriggerEnter) 確認命中敵人 Collider 時呼叫此方法
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
    /// 若戰鬥系統使用 Raycast / SphereCast，可直接傳入 RaycastHit.point 與 normal
    /// </summary>
    public void SpawnHitImpactAtPoint(Vector3 hitPoint, Vector3 hitNormal)
    {
        // 生成 VFX_HolyHitImpact 特效
        GameObject effectPrefab = hitImpactPrefab;
        if (effectPrefab != null)
        {
            ObjectPoolManager.Instance.SpawnVFX(effectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
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
