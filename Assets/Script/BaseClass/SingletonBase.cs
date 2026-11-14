using UnityEngine;

public class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindAnyObjectByType<T>();

                if (instance == null)
                {
                    // 1. 優先嘗試從 Resources 載入專屬 Prefab 或包含核心服務的 GameManager Prefab
                    GameObject prefab = Resources.Load<GameObject>(typeof(T).Name);
                    if (prefab == null && (typeof(T) == typeof(GameAssets) || typeof(T) == typeof(ObjectPoolManager) || typeof(T) == typeof(GameManager)))
                    {
                        prefab = Resources.Load<GameObject>("GameManager");
                    }

                    if (prefab != null)
                    {
                        GameObject spawned = Object.Instantiate(prefab);
                        spawned.name = prefab.name;
                        instance = spawned.GetComponentInChildren<T>();
                    }

                    // 2. 若仍未找到，則動態建立空物件掛載
                    if (instance == null)
                    {
                        GameObject go = new GameObject(typeof(T).Name);
                        instance = go.AddComponent<T>();
                    }
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            // 防呆：重複出現時刪除多餘的實例
            Destroy(gameObject);
        }
    }
}
