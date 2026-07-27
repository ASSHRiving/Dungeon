using UnityEngine;

public class PlayerBalanceSystem : MonoBehaviour
{
    private int balance = 0;
    private void Start()
    {
        // 初始化金幣數量
        balance = 0;
        // 廣播初始金幣數量
        UIEvents.GoldChanged(balance);
    }
    public void AddCoin(int amount)
    {
        balance += amount;
        UIEvents.GoldChanged(balance);
        Debug.Log($"玩家獲得 {amount} 金幣，當前金幣數量: {balance}");
    }
    public int GetCoin() => balance;
}
