using UnityEngine;
using UnityEngine.Profiling;

public class BuildPerformanceMonitor : MonoBehaviour
{
    [Header("=== 打包實機效能監控面板 (按 F3 切換顯示) ===")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;
    [SerializeField] private bool showOnStart = true;

    private bool isVisible;
    private float deltaTime = 0.0f;
    private float minFps = 999f;
    private float resetMinTimer = 0f;

    private GUIStyle boxStyle;
    private GUIStyle textStyle;

    private void Awake()
    {
        isVisible = showOnStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
        }

        if (!isVisible) return;

        // 平滑計算幀時間與瞬間最低幀數 (抓瞬間卡頓 Stutter 最有效)
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float currentInstantFps = 1.0f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        if (currentInstantFps < minFps) minFps = currentInstantFps;

        // 每 5 秒重置一次最低幀數紀錄
        resetMinTimer += Time.unscaledDeltaTime;
        if (resetMinTimer >= 5.0f)
        {
            minFps = currentInstantFps;
            resetMinTimer = 0f;
        }
    }

    private void OnGUI()
    {
        if (!isVisible) return;

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box);
            textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };
        }

        float fps = 1.0f / deltaTime;
        float ms = deltaTime * 1000.0f;

        // 讀取關鍵記憶體指標 (MB)
        long monoUsedMB = Profiler.GetMonoUsedSizeLong() / (1024 * 1024);     // C# 腳本與物件池目前佔用的記憶體
        long totalAllocMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024); // 含材質、貼圖、Mesh 的總記憶體

        string fpsColor = fps >= 55f ? "#00FF88" : (fps >= 30f ? "#FFD700" : "#FF3333");
        string minFpsColor = minFps >= 45f ? "#00E5FF" : "#FF5555";

        GUI.Box(new Rect(15, 15, 310, 115), "", boxStyle);

        GUI.Label(new Rect(25, 22, 290, 25),
            $"FPS: <color={fpsColor}>{fps:0.} ({ms:0.0} ms)</color> | 5秒最低: <color={minFpsColor}>{minFps:0.}</color>", textStyle);

        GUI.Label(new Rect(25, 48, 290, 25),
            $"C# 記憶體 (GC): <color=#00E5FF>{monoUsedMB} MB</color>", textStyle);

        GUI.Label(new Rect(25, 74, 290, 25),
            $"Unity 總顯存/記憶體: <color=#FFD700>{totalAllocMB} MB</color>", textStyle);

        GUI.Label(new Rect(25, 100, 290, 22),
            "<color=#AAAAAA>[按 F3 隱藏面板]</color>", textStyle);
    }
}