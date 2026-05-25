using UnityEngine;

public class SingletonBase<T> : MonoBehaviour where T : SingletonBase<T>
{
    private static T _instance;
    
    public static T Instance
    {
        get
        {       
            if(_instance == null)
            {
                _instance = Object.FindAnyObjectByType<T>();
                if(_instance == null) 
                {
                    GameObject go = new GameObject();
                    _instance = go.AddComponent<T>();
                    go.name = typeof(T).Name;
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
}
