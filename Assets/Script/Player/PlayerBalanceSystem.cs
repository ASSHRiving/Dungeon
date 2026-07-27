using UnityEngine;

public class PlayerBalanceSystem : MonoBehaviour
{
    private int balance = 0;
    public void AddCoin(int amount)
    {
        balance += amount;
        Debug.Log($"玩家獲得 {amount} 金幣，當前金幣數量: {balance}");
    }
    public int GetCoin() => balance;
}
