using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Debug = UnityEngine.Debug;

[Serializable]
public class GitHubAsset
{
    public string name;
    public string browser_download_url;
}

[Serializable]
public class GitHubReleaseResponse
{
    public string tag_name;
    public string body;
    public GitHubAsset[] assets;
}

public class GameAutoUpdater : MonoBehaviour
{
    [Header("GitHub 設定")]
    [Tooltip("你的 GitHub 帳號或組織名稱")]
    [SerializeField] private string githubOwner = "ASSHRiving";

    [Tooltip("你的專案儲存庫名稱")]
    [SerializeField] private string githubRepo = "Dungeon";

    [Header("UI 綁定")]
    [SerializeField] private GameObject updatePromptPanel;   // 發現新版本的提示面板
    [SerializeField] private GameObject downloadingPanel;    // 下載進度面板
    [SerializeField] private TextMeshProUGUI patchNotesText; // 顯示 Release 內容與版本
    [SerializeField] private TextMeshProUGUI statusText;     // 狀態與下載進度文字
    [SerializeField] private Slider progressBar;             // 進度條
    [SerializeField] private Button confirmUpdateButton;     // 更新按鈕
    [SerializeField] private Button skipUpdateButton;        // 略過按鈕

    private string latestVersion;
    private string downloadUrl;
    private string releaseNotes;

    private string gameRootPath;
    private string tempUpdateFolder;
    private string zipFilePath;

    private void Awake()
    {
        // 取得 Game.exe 所在的遊戲根目錄
        gameRootPath = Directory.GetParent(Application.dataPath).FullName;
        tempUpdateFolder = Path.Combine(gameRootPath, "TempUpdate");
        zipFilePath = Path.Combine(gameRootPath, "UpdatePackage.zip");

        if (updatePromptPanel) updatePromptPanel.SetActive(false);
        if (downloadingPanel) downloadingPanel.SetActive(false);
    }

    private void Start()
    {
#if UNITY_EDITOR
        Debug.Log($"[AutoUpdater] 目前在 Editor 模式，本地版本：{Application.version}。跳過自動更新檢查。");
#else
        StartCoroutine(CheckGitHubReleaseRoutine());
#endif
    }

    /// <summary>
    /// 步驟 1：向 GitHub API 請求最新的 Release 資訊
    /// </summary>
    private IEnumerator CheckGitHubReleaseRoutine()
    {
        // 如果檢測到裝置完全沒有網路連線，直接退出檢查
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("[AutoUpdater] 偵測到無網路連線，跳過更新檢查。");
            yield break;
        }
        string apiUrl = $"https://api.github.com/repos/{githubOwner}/{githubRepo}/releases/latest";

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            // GitHub API 嚴格要求必須傳入 User-Agent Header，否則會回傳 403 Forbidden
            request.SetRequestHeader("User-Agent", $"{githubOwner}-{githubRepo}-GameUpdater");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[AutoUpdater] 無法取得 GitHub 最新 Release: {request.error}");
                yield break;
            }

            GitHubReleaseResponse responseData;
            try
            {
                responseData = JsonUtility.FromJson<GitHubReleaseResponse>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoUpdater] GitHub JSON 解析失敗: {ex.Message}");
                yield break;
            }

            if (responseData == null || string.IsNullOrEmpty(responseData.tag_name))
            {
                Debug.LogWarning("[AutoUpdater] 找不到有效的 Release 資料。");
                yield break;
            }

            // 清理 tag_name，如果標籤習慣寫成 "v1.0.2"，去除開頭的 'v'
            latestVersion = responseData.tag_name.TrimStart('v', 'V');
            releaseNotes = responseData.body;

            // 從附帶資產中尋找 .zip 檔案
            downloadUrl = null;
            if (responseData.assets != null)
            {
                foreach (var asset in responseData.assets)
                {
                    if (asset.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        downloadUrl = asset.browser_download_url;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                Debug.LogWarning("[AutoUpdater] 最新 Release 中未找到 .zip 格式的安裝包。");
                yield break;
            }

            // 比對版本號
            if (IsNewerVersion(Application.version, latestVersion))
            {
                ShowUpdatePrompt();
            }
            else
            {
                Debug.Log($"[AutoUpdater] 目前已是最新版本 (v{Application.version})。");
            }
        }
    }

    private bool IsNewerVersion(string localVerStr, string remoteVerStr)
    {
        if (Version.TryParse(localVerStr, out Version localVer) &&
            Version.TryParse(remoteVerStr, out Version remoteVer))
        {
            return remoteVer.CompareTo(localVer) > 0;
        }
        return localVerStr != remoteVerStr;
    }

    private void ShowUpdatePrompt()
    {
        if (updatePromptPanel) updatePromptPanel.SetActive(true);
        if (patchNotesText)
        {
            patchNotesText.text = $"發現新版本 v{latestVersion} (目前版本 v{Application.version})\n\n" +
                                  $"【更新說明】\n{releaseNotes}";
        }

        confirmUpdateButton.onClick.RemoveAllListeners();
        confirmUpdateButton.onClick.AddListener(() =>
        {
            updatePromptPanel.SetActive(false);
            StartCoroutine(DownloadAndExtractRoutine());
        });

        if (skipUpdateButton)
        {
            skipUpdateButton.onClick.RemoveAllListeners();
            skipUpdateButton.onClick.AddListener(() => updatePromptPanel.SetActive(false));
        }
    }

    /// <summary>
    /// 步驟 2：從 GitHub Releases 直接串流下載 zip 檔案
    /// </summary>
    private IEnumerator DownloadAndExtractRoutine()
    {
        if (downloadingPanel) downloadingPanel.SetActive(true);
        if (progressBar) progressBar.value = 0f;

        CleanupTempFiles();

        using (UnityWebRequest request = new UnityWebRequest(downloadUrl, UnityWebRequest.kHttpVerbGET))
        {
            // 將檔案寫入硬碟避免吃 RAM
            request.downloadHandler = new DownloadHandlerFile(zipFilePath);
            request.SendWebRequest();

            while (!request.isDone)
            {
                float progress = request.downloadProgress;
                if (progressBar) progressBar.value = progress;

                float downloadedMB = request.downloadedBytes / (1024f * 1024f);
                if (statusText) statusText.text = $"正在下載更新檔... {progress * 100f:F1}% ({downloadedMB:F1} MB)";
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (statusText) statusText.text = $"下載失敗：{request.error}";
                yield break;
            }
        }

        if (progressBar) progressBar.value = 1f;
        if (statusText) statusText.text = "下載完成，正在解壓縮檔案...";
        yield return null;

        if (!ExtractZipFile()) yield break;

        if (statusText) statusText.text = "準備重啟遊戲套用更新...";
        yield return new WaitForSeconds(1f);

        ExecuteBatchUpdater();
    }

    private bool ExtractZipFile()
    {
        try
        {
            Directory.CreateDirectory(tempUpdateFolder);
            ZipFile.ExtractToDirectory(zipFilePath, tempUpdateFolder);
            File.Delete(zipFilePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AutoUpdater] 解壓縮失敗: {e.Message}");
            if (statusText) statusText.text = "解壓縮失敗，請檢查硬碟空間或防毒軟體。";
            return false;
        }
    }

    /// <summary>
    /// 步驟 3：呼叫 .bat 覆蓋檔案並重啟
    /// </summary>
    private void ExecuteBatchUpdater()
    {
        string batchFilePath = Path.Combine(gameRootPath, "updater.bat");
        string exeFileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

        string batchScript = $@"@echo off
chcp 65001 > NUL
echo 正在等待遊戲關閉...
timeout /t 3 /nobreak > NUL

echo 正在覆蓋更新檔案...
xcopy /s /e /y ""{tempUpdateFolder}\*"" ""{gameRootPath}\"" > NUL

echo 正在清理暫存...
rmdir /s /q ""{tempUpdateFolder}""

echo 正在重啟新版本...
start """" ""{Path.Combine(gameRootPath, exeFileName)}""

del ""%~f0""
";

        File.WriteAllText(batchFilePath, batchScript, new System.Text.UTF8Encoding(false));

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = batchFilePath,
            WorkingDirectory = gameRootPath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process.Start(startInfo);
        Application.Quit();
    }

    private void CleanupTempFiles()
    {
        if (File.Exists(zipFilePath)) File.Delete(zipFilePath);
        if (Directory.Exists(tempUpdateFolder)) Directory.Delete(tempUpdateFolder, true);
    }
}