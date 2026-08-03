using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerBalanceSystem : MonoBehaviour
{
    [SerializeField] private int balance = 0;
    private void Start()
    {
        // 初始化金幣數量
        balance = 50;
        // 廣播初始金幣數量
        UIEvents.GoldChanged(balance);
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UIEvents.GoldChanged(balance);
    }
    public void AddCoin(int amount)
    {
        balance += amount;
        UIEvents.GoldChanged(balance);
        Debug.Log($"玩家獲得 {amount} 金幣，當前金幣數量: {balance}");
    }
    public void SpendCoin(int amount)
    {
        if (balance >= amount)
        {
            balance -= amount;
            UIEvents.GoldChanged(balance);
            Debug.Log($"玩家花費 {amount} 金幣，當前金幣數量: {balance}");
        }
        else
        {
            Debug.Log("金幣不足，無法花費");
        }
    }
    public int GetCoin() => balance;
}
