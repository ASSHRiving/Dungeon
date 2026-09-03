using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

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

    private IObjectPool<GameObject> _coinPool;
    private IObjectPool<DamageText> _damageTextPool;

    protected override void Awake()
    {
        base.Awake(); // 🎯 呼叫父類 Awake
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
            createFunc: () => Instantiate(coinPrefab, transform),
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
            actionOnDestroy: (dmgText) => Destroy(dmgText.gameObject),
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 當場景切換時，重置所有物件池
        _coinPool?.Clear();
        _damageTextPool?.Clear();
    }
}