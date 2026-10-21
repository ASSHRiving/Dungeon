using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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

    private IObjectPool<GameObject> _coinPool;
    private IObjectPool<DamageText> _damageTextPool;

    // 動態字典池：Key 為 Prefab 的 InstanceID，Value 為該 Prefab 專屬的 ObjectPool
    private readonly Dictionary<int, IObjectPool<GameObject>> _vfxPools = new Dictionary<int, IObjectPool<GameObject>>();

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
    /// </summary>
    public GameObject SpawnVFX(GameObject vfxPrefab, Vector3 position, Quaternion rotation)
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

        return vfxInstance;
    }

    private IObjectPool<GameObject> CreateNewVFXPool(GameObject prefab)
    {
        IObjectPool<GameObject> newPool = null;

        newPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab, transform);

                // 自動掛載「播完自動回收」組件，免去手動寫腳本
                PooledVFXAutoRelease autoRelease = obj.GetComponent<PooledVFXAutoRelease>();
                if (autoRelease == null)
                {
                    autoRelease = obj.AddComponent<PooledVFXAutoRelease>();
                }
                autoRelease.Initialize(newPool);

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
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 當場景切換時，重置所有物件池
        _coinPool?.Clear();
        _damageTextPool?.Clear();

        foreach (var kvp in _vfxPools)
        {
            kvp.Value?.Clear();
        }
    }
}
/// <summary>
/// 內部輔助組件：當 ParticleSystem 播放結束時，自動將特效歸還給 ObjectPool
/// </summary>
public class PooledVFXAutoRelease : MonoBehaviour
{
    private IObjectPool<GameObject> _pool;
    private bool _isReleased = false;

    public void Initialize(IObjectPool<GameObject> pool)
    {
        _pool = pool;

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

    // Unity 內建事件：當主粒子系統與子粒子皆播放完畢時自動呼叫
    private void OnParticleSystemStopped()
    {
        if (!_isReleased && _pool != null)
        {
            _isReleased = true;
            _pool.Release(gameObject);
        }
    }
}