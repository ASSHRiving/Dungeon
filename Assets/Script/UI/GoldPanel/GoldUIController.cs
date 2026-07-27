using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoldUIController : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText; // 用於顯示金幣數量的 UI Text 元件

    private void OnEnable()
    {
        UIEvents.OnGoldChanged += UpdateGoldUI;
    }
    private void OnDisable()
    {
        UIEvents.OnGoldChanged -= UpdateGoldUI;
    }
    private void UpdateGoldUI(int newGoldAmount)
    {
        goldText.text = $"Gold: {newGoldAmount}";
    }

}
