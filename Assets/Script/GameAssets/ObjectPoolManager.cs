using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class ObjectPoolManager : SingletonBase<ObjectPoolManager>
{
    [Header("金幣物件池設定")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int defaultCoinPoolSize = 20;
    [SerializeField] private int maxCoinPoolSize = 100;

    [Header("傷害飄字物件池設定")]
    [SerializeField] private DamageText damageTextPrefab;
    [SerializeField] private int defaultDamagePoolSize = 20;
    [SerializeField] private int maxDamagePoolSize = 50;

    [Header("萬用動態特效池預設容量 (自動依 Prefab 分類)")]
    [SerializeField] private int defaultVFXPoolSize = 10;
    [SerializeField] private int maxVFXPoolSize = 30;

    [Header("萬用破壞物碎片池預設容量 (自動依 Prefab 分類)")]
    [SerializeField] private int defaultDebrisPoolSize = 5;
    [SerializeField] private int maxDebrisPoolSize = 15;

    private IObjectPool<GameObject> _coinPool;
    private IObjectPool<DamageText> _damageTextPool;

    // 動態字典池：Key 為 Prefab 的 InstanceID，Value 為該 Prefab 專屬的 ObjectPool
    private readonly Dictionary<int, IObjectPool<GameObject>> _vfxPools = new Dictionary<int, IObjectPool<GameObject>>();

    // 碎片動態字典池：Key 為碎片 Prefab 的 InstanceID
    private readonly Dictionary<int, IObjectPool<GameObject>> _debrisPools = new Dictionary<int, IObjectPool<GameObject>>();

    protected override void Awake()
    {
        base.Awake();
        InitCoinPool();
        InitDamageTextPool();
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #region 金幣物件池邏輯
    private void InitCoinPool()
    {
        if (coinPrefab == null)
        {
            Debug.LogError("⚠️ ObjectPoolManager 上的 Coin Prefab 未設定！");
            return;
        }

        _coinPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(coinPrefab),
            actionOnGet: (coin) => coin.SetActive(true),
            actionOnRelease: (coin) => coin.SetActive(false),
            actionOnDestroy: (coin) => Destroy(coin),
            collectionCheck: true,
            defaultCapacity: defaultCoinPoolSize,
            maxSize: maxCoinPoolSize
        );
    }

    // 從池子拿一顆金幣出來
    public GameObject GetCoin(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (_coinPool == null) return null;

        GameObject coin = _coinPool.Get();
        coin.transform.position = spawnPosition;
        coin.transform.rotation = spawnRotation;

        if (coin.TryGetComponent<Coin>(out Coin coinScript))
        {
            coinScript.SetPool(_coinPool);
        }

        return coin;
    }
    #endregion

    #region 傷害飄字物件池邏輯
    private void InitDamageTextPool()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("⚠️ ObjectPoolManager 上的 DamageText Prefab 未設定！(若無使用可忽略)");
            return;
        }

        _damageTextPool = new ObjectPool<DamageText>(
            createFunc: () => Instantiate(damageTextPrefab, transform),
            actionOnGet: (dmgText) => dmgText.gameObject.SetActive(true),
            actionOnRelease: (dmgText) => dmgText.gameObject.SetActive(false),
            actionOnDestroy: (dmgText) => Destroy(dmgText),
            collectionCheck: true,
            defaultCapacity: defaultDamagePoolSize,
            maxSize: maxDamagePoolSize
        );
    }

    /// <summary>
    /// 生成傷害數字飄字
    /// </summary>
    public DamageText SpawnDamageText(Vector3 spawnPosition, float damageAmount, bool isCritical = false)
    {
        if (_damageTextPool == null) return null;

        DamageText dmgText = _damageTextPool.Get();
        dmgText.transform.position = spawnPosition;
        
        // 將 Pool 傳遞給 DamageText，方便它動畫結束後自行 Release
        dmgText.Setup(damageAmount, isCritical, _damageTextPool);

        return dmgText;
    }
    #endregion

    #region 3. 萬用粒子特效物件池 (新增！任何武器/技能特效皆可直接傳入 Prefab 呼叫)
    /// <summary>
    /// 生成任何粒子特效 (如 VFX_HolyHitImpact)，播放完畢後會自動歸還至對應物件池
    /// 可選傳入 autoReleaseDelay 指定定時回收秒數 (適用於持續/循環特效)
    /// </summary>
    public GameObject SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation, float autoReleaseDelay = -1f)
    {
        if (vfxPrefab == null) return null;

        int prefabKey = vfxPrefab.GetInstanceID();

        // 若該特效 Prefab 尚未建立過池子，自動為它建立一個專屬 ObjectPool
        if (!_vfxPools.TryGetValue(prefabKey, out IObjectPool<GameObject> pool))
        {
            pool = CreateNewVFXPool(vfxPrefab);
            _vfxPools.Add(prefabKey, pool);
        }

        GameObject vfxInstance = pool.Get();
        vfxInstance.transform.SetPositionAndRotation(position, rotation);

        // 重新播放粒子系統 (包含所有子粒子系統：如十字星芒 + 菱形碎屑 + 速度線)
        if (vfxInstance.TryGetComponent<ParticleSystem>(out ParticleSystem ps))
        {
            ps.Clear(true);
            ps.Play(true);
        }
        else
        {
            ParticleSystem[] childPSs = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < childPSs.Length; i++)
            {
                childPSs[i].Clear(true);
                childPSs[i].Play(true);
            }
        }

        if (autoReleaseDelay > 0f)
        {
            if (vfxInstance.TryGetComponent<PooledVFXAutoRelease>(out var autoRelease) ||
                (autoRelease = vfxInstance.GetComponentInChildren<PooledVFXAutoRelease>()) != null)
            {
                autoRelease.SetAutoReleaseDelay(autoReleaseDelay);
            }
        }

        return vfxInstance;
    }

    /// <summary>
    /// 手動回收粒子特效至物件池
    /// </summary>
    public void ReleaseVFX(GameObject vfxInstance)
    {
        if (vfxInstance == null) return;

        if (vfxInstance.TryGetComponent<PooledVFXAutoRelease>(out var autoRelease) ||
            (autoRelease = vfxInstance.GetComponentInChildren<PooledVFXAutoRelease>()) != null)
        {
            autoRelease.Release();
        }
        else
        {
            vfxInstance.SetActive(false);
        }
    }

    private IObjectPool<GameObject> CreateNewVFXPool(GameObject prefab)
    {
        IObjectPool<GameObject> newPool = null;

        newPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab, transform);

                // 自動掛載「播完自動回收」組件，免去手動寫腳本
                // 優先掛在包含 ParticleSystem 的物件上，若無則掛在根物件
                ParticleSystem ps = obj.GetComponentInChildren<ParticleSystem>();
                GameObject targetGo = ps != null ? ps.gameObject : obj;

                PooledVFXAutoRelease autoRelease = targetGo.GetComponent<PooledVFXAutoRelease>();
                if (autoRelease == null)
                {
                    autoRelease = targetGo.AddComponent<PooledVFXAutoRelease>();
                }
                autoRelease.Initialize(newPool, obj);

                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => { if (obj != null) Destroy(obj); },
            collectionCheck: true,
            defaultCapacity: defaultVFXPoolSize,
            maxSize: maxVFXPoolSize
        );

        return newPool;
    }
    #endregion
    
    #region 4. 萬用場景破壞物碎片池 (木桶、木箱、陶罐皆可直接傳入 Prefab 呼叫)
    /// <summary>
    /// 生成破壞物碎片，自動復位子碎片、施加物理爆炸力，並在延遲後自動歸還至物件池
    /// </summary>
    public GameObject SpawnDebris(
        GameObject debrisPrefab,
        Vector3 position,
        Quaternion rotation,
        Vector3 hitPoint,
        float explosionForce = 300f,
        float explosionRadius = 2f,
        float lifeTime = 3.5f)
    {
        if (debrisPrefab == null) return null;

        int prefabKey = debrisPrefab.GetInstanceID();

        if (!_debrisPools.TryGetValue(prefabKey, out IObjectPool<GameObject> pool))
        {
            pool = CreateNewDebrisPool(debrisPrefab);
            _debrisPools.Add(prefabKey, pool);
        }

        GameObject debrisInstance = pool.Get();
        debrisInstance.transform.SetPositionAndRotation(position, rotation);

        if (debrisInstance.TryGetComponent<PooledDebrisAutoReset>(out var debrisReset))
        {
            debrisReset.ResetAndExplode(hitPoint, explosionForce, explosionRadius, lifeTime);
        }

        return debrisInstance;
    }

    private IObjectPool<GameObject> CreateNewDebrisPool(GameObject prefab)
    {
        IObjectPool<GameObject> newPool = null;

        newPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab, transform);

                // 自動掛載「碎片復位與定時回收」組件，免去在每個碎片 Prefab 手動掛腳本！
                if (!obj.TryGetComponent<PooledDebrisAutoReset>(out var autoReset))
                {
                    autoReset = obj.AddComponent<PooledDebrisAutoReset>();
                }
                autoReset.Initialize(newPool);

                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => { if (obj != null) Destroy(obj); },
            collectionCheck: true,
            defaultCapacity: defaultDebrisPoolSize,
            maxSize: maxDebrisPoolSize
        );
        return newPool;
    }
    #endregion
    
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 當場景切換時，重置所有物件池
        _coinPool?.Clear();
        _damageTextPool?.Clear();

        foreach (var kvp in _vfxPools)
        {
            kvp.Value?.Clear();
        }
        foreach (var kvp in _debrisPools)
        {
            kvp.Value?.Clear();
        }
    }
}



/// <summary>
/// 內部輔助組件：當 ParticleSystem 播放結束或時間到時，自動將特效歸還給 ObjectPool
/// </summary>
public class PooledVFXAutoRelease : MonoBehaviour
{
    private IObjectPool<GameObject> _pool;
    private GameObject _rootObject;
    private bool _isReleased = false;

    public void Initialize(IObjectPool<GameObject> pool, GameObject rootObject = null)
    {
        _pool = pool;
        _rootObject = rootObject != null ? rootObject : gameObject;

        // 強制將主粒子系統的 Stop Action 設為 Callback，以觸發 OnParticleSystemStopped
        if (TryGetComponent<ParticleSystem>(out ParticleSystem ps))
        {
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }
    }

    private void OnEnable()
    {
        _isReleased = false;
    }

    public void SetAutoReleaseDelay(float delay)
    {
        CancelInvoke(nameof(Release));
        if (delay > 0f)
        {
            Invoke(nameof(Release), delay);
        }
    }

    public void Release()
    {
        if (!_isReleased && _pool != null)
        {
            _isReleased = true;
            CancelInvoke(nameof(Release));
            _pool.Release(_rootObject != null ? _rootObject : gameObject);
        }
    }

    // Unity 內建事件：當主粒子系統與子粒子皆播放完畢時自動呼叫
    private void OnParticleSystemStopped()
    {
        Release();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Release));
    }
}

/// <summary>
/// 內部輔助組件：自動紀錄所有子碎片的初始位置，每次從物件池取出時復位、引爆並定時回收
/// </summary>
public class PooledDebrisAutoReset : MonoBehaviour
{
    private IObjectPool<GameObject> myPool;
    private Rigidbody[] shards;
    private Vector3[] startPositions;
    private Quaternion[] startRotations;
    private Vector3[] startScales;
    private bool isReleased = false;
    private Coroutine explodeCoroutine;

    public void Initialize(IObjectPool<GameObject> pool)
    {
        myPool = pool;

        // 在第一次生成時，把所有子碎片拼成木桶的初始位置、旋轉、大小抄進陣列備份
        shards = GetComponentsInChildren<Rigidbody>();
        int count = shards.Length;
        startPositions = new Vector3[count];
        startRotations = new Quaternion[count];
        startScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform t = shards[i].transform;
            startPositions[i] = t.localPosition;
            startRotations[i] = t.localRotation;
            startScales[i] = t.localScale;
        }
    }

    public void ResetAndExplode(Vector3 hitPoint, float force, float radius, float lifeTime)
    {
        isReleased = false;
        if (explodeCoroutine != null)
        {
            StopCoroutine(explodeCoroutine);
        }

        // 啟動協程：分開影格處理「歸零」與「爆炸」
        explodeCoroutine = StartCoroutine(ResetAndExplodeRoutine(hitPoint, force, radius, lifeTime));

        /*
        for (int i = 0; i < shards.Length; i++)
        {
            Rigidbody rb = shards[i];

            // 1. 先清除上一次爆炸殘留的移動速度與旋轉角速度
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 2. 將碎片瞬間排回原本拼成完整木桶的局部位置
            Transform t = rb.transform;
            t.localPosition = startPositions[i];
            t.localRotation = startRotations[i];
            t.localScale = startScales[i];

        }
        foreach(Rigidbody rb in shards)
        {
            rb.WakeUp();
            rb.AddExplosionForce(force, hitPoint, radius);
        }

        CancelInvoke(nameof(ReleaseSelf));
        Invoke(nameof(ReleaseSelf), lifeTime);
        */
    }
    private IEnumerator ResetAndExplodeRoutine(Vector3 hitPoint, float force, float radius, float lifeTime)
    {
        for (int i = 0; i < shards.Length; i++)
        {
            Rigidbody rb = shards[i];

            // 1. 先清除上一次爆炸殘留的移動速度與旋轉角速度
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 2. 將碎片瞬間排回原本拼成完整木桶的局部位置
            Transform t = rb.transform;
            t.localPosition = startPositions[i];
            t.localRotation = startRotations[i];
            t.localScale = startScales[i];

        }
        yield return new WaitForFixedUpdate();
        foreach(Rigidbody rb in shards)
        {
            rb.WakeUp();
            rb.AddExplosionForce(force, hitPoint, radius);
        }

        // 4. 定時回收
        CancelInvoke(nameof(ReleaseSelf));
        Invoke(nameof(ReleaseSelf), lifeTime);
    }

    private void ReleaseSelf()
    {
        if (!isReleased && myPool != null)
        {
            isReleased = true;
            myPool.Release(gameObject);
        }
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ReleaseSelf));
    }
}