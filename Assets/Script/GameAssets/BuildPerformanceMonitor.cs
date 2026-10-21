using UnityEngine;
using UnityEngine.Profiling;

public class BuildPerformanceMonitor : MonoBehaviour
{
    [Header("=== 打包實機效能監控面板 (按 F3 切換顯示) ===")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;
    [SerializeField] private bool showOnStart = true;

    private bool _isVisible;
    private float _deltaTime = 0.0f;
    private float _minFps = 999f;
    private float _resetMinTimer = 0f;

    private GUIStyle _boxStyle;
    private GUIStyle _textStyle;

    private void Awake()
    {
        _isVisible = showOnStart;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _isVisible = !_isVisible;
        }

        if (!_isVisible) return;

        // 平滑計算幀時間與瞬間最低幀數 (抓瞬間卡頓 Stutter 最有效)
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        float currentInstantFps = 1.0f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        if (currentInstantFps < _minFps) _minFps = currentInstantFps;

        // 每 5 秒重置一次最低幀數紀錄
        _resetMinTimer += Time.unscaledDeltaTime;
        if (_resetMinTimer >= 5.0f)
        {
            _minFps = currentInstantFps;
            _resetMinTimer = 0f;
        }
    }

    private void OnGUI()
    {
        if (!_isVisible) return;

        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box);
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true
            };
        }

        float fps = 1.0f / _deltaTime;
        float ms = _deltaTime * 1000.0f;

        // 讀取關鍵記憶體指標 (MB)
        long monoUsedMB = Profiler.GetMonoUsedSizeLong() / (1024 * 1024);     // C# 腳本與物件池目前佔用的記憶體
        long totalAllocMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024); // 含材質、貼圖、Mesh 的總記憶體

        string fpsColor = fps >= 55f ? "#00FF88" : (fps >= 30f ? "#FFD700" : "#FF3333");
        string minFpsColor = _minFps >= 45f ? "#00E5FF" : "#FF5555";

        GUI.Box(new Rect(15, 15, 310, 115), "", _boxStyle);

        GUI.Label(new Rect(25, 22, 290, 25),
            $"FPS: <color={fpsColor}>{fps:0.} ({ms:0.0} ms)</color> | 5秒最低: <color={minFpsColor}>{_minFps:0.}</color>", _textStyle);

        GUI.Label(new Rect(25, 48, 290, 25),
            $"C# 記憶體 (GC): <color=#00E5FF>{monoUsedMB} MB</color>", _textStyle);

        GUI.Label(new Rect(25, 74, 290, 25),
            $"Unity 總顯存/記憶體: <color=#FFD700>{totalAllocMB} MB</color>", _textStyle);

        GUI.Label(new Rect(25, 100, 290, 22),
            "<color=#AAAAAA>[按 F3 隱藏面板]</color>", _textStyle);
    }
}