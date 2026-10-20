using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Header("顯示設定")]
    [Tooltip("更新頻率（秒），預設 0.2 秒刷新一次，避免數字狂跳難以辨識")]
    [SerializeField] private float updateInterval = 0.2f;
    [SerializeField] private int fontSize = 22;
    [SerializeField] private Color textColor = Color.green;

    private float accumTime = 0f;
    private int frameCount = 0;
    private float timeLeft;
    private float currentFPS;

    private GUIStyle style;
    private Rect drawRect;

    private void Start()
    {
        timeLeft = updateInterval;

        // 初始化字型樣式與左上角顯示位置 (X: 20, Y: 20)
        style = new GUIStyle();
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = textColor;

        drawRect = new Rect(20, 20, 200, 50);
    }

    private void Update()
    {
        // 使用 unscaledDeltaTime，即使遊戲暫停 (Time.timeScale = 0) 也能正常計算
        timeLeft -= Time.unscaledDeltaTime;
        accumTime += Time.unscaledDeltaTime;
        frameCount++;

        if (timeLeft <= 0f)
        {
            currentFPS = frameCount / accumTime;

            // 依幀率自動變換顏色：≥60 綠色、30~59 黃色、<30 紅色
            if (currentFPS >= 59.5f)
                style.normal.textColor = Color.green;
            else if (currentFPS >= 29.5f)
                style.normal.textColor = Color.yellow;
            else
                style.normal.textColor = Color.red;

            timeLeft = updateInterval;
            accumTime = 0f;
            frameCount = 0;
        }
    }

    private void OnGUI()
    {
        GUI.Label(drawRect, $"FPS: {currentFPS:F0}", style);
    }
}