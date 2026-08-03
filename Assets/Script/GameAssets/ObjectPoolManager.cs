using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class ObjectPoolManager : SingletonBase<ObjectPoolManager>
{
    [Header("金幣物件池設定")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int defaultPoolSize = 20;
    [SerializeField] private int maxPoolSize = 100;

    private IObjectPool<GameObject> _coinPool;

    protected override void Awake()
    {
        base.Awake(); // 🎯 呼叫父類 Awake
        InitCoinPool();
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

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
            defaultCapacity: defaultPoolSize,
            maxSize: maxPoolSize
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
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 當場景切換時，重置金幣池
        if (_coinPool != null)
        {
            _coinPool.Clear();
        }
    }
}