#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 局外養成與靈魂測試控制台 (Editor Window)
/// 提供開發與測試人員在編輯器內隨時「設定靈魂、加減貨幣、重置等級、一鍵滿級」，
/// 支援 Edit Mode (離線修改存檔) 與 Play Mode (遊戲中即時生效並刷新 UI)。
/// </summary>
public class MetaProgressionDebugWindow : EditorWindow
{
    private int customSoulsInput = 1000;
    private int selectedCharID = 0;
    private int customLevelInput = 0;

    [MenuItem("Tools/Meta Debugger Window", priority = 100)]
    [MenuItem("Tools/養成測試控制台", priority = 100)]
    public static void OpenWindow()
    {
        MetaProgressionDebugWindow window = GetWindow<MetaProgressionDebugWindow>("養成測試工具");
        window.minSize = new Vector2(380, 480);
        window.Show();
    }

    #region 頂部選單快速按鈕
    [MenuItem("Tools/Add 1000 Souls", priority = 110)]
    public static void QuickAdd1000Souls() => AddSouls(1000);

    [MenuItem("Tools/Add 5000 Souls", priority = 111)]
    public static void QuickAdd5000Souls() => AddSouls(5000);

    [MenuItem("Tools/Reset All Character Levels", priority = 120)]
    public static void QuickResetAllLevels() => ResetAllCharacterLevels();

    [MenuItem("Tools/Max All Levels Lv10", priority = 121)]
    public static void QuickMaxAllLevels() => MaxAllCharacterLevels();

    [MenuItem("Tools/Reset All Souls and Levels", priority = 130)]
    public static void QuickResetAllData()
    {
        SetSouls(0);
        ResetAllCharacterLevels();
        Debug.Log("🧹 [MetaDebugger] 已清空所有靈魂與角色養成等級！");
    }
    #endregion

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        DrawHeader();

        EditorGUILayout.Space(10);
        DrawSoulsSection();

        EditorGUILayout.Space(15);
        DrawCharacterUpgradeSection();

        EditorGUILayout.Space(15);
        DrawQuickActionsSection();
    }

    private void DrawHeader()
    {
        GUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("✦ 局外養成與靈魂測試控制台", EditorStyles.boldLabel);
        string modeStr = Application.isPlaying ? "<color=#80FF80>● 遊戲運行中 (即時生效)</color>" : "<color=#FFCC00>○ 編輯器模式 (存檔生效)</color>";
        GUIStyle style = new GUIStyle(EditorStyles.label) { richText = true };
        EditorGUILayout.LabelField($"狀態: {modeStr}", style);
        GUILayout.EndVertical();
    }

    private void DrawSoulsSection()
    {
        GUILayout.BeginVertical("靈魂貨幣控制 (Souls)", GUI.skin.box);
        EditorGUILayout.Space(15);

        int currentSouls = GetCurrentSouls();
        EditorGUILayout.LabelField("當前持有靈魂:", $"{currentSouls:N0} 靈魂", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        customSoulsInput = EditorGUILayout.IntField("設定數額:", customSoulsInput);
        if (GUILayout.Button("設為此數值", GUILayout.Width(90)))
        {
            SetSouls(Mathf.Max(0, customSoulsInput));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("快速增減:");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+500")) AddSouls(500);
        if (GUILayout.Button("+1,000")) AddSouls(1000);
        if (GUILayout.Button("+5,000")) AddSouls(5000);
        if (GUILayout.Button("歸零 (0)")) SetSouls(0);
        EditorGUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawCharacterUpgradeSection()
    {
        GUILayout.BeginVertical("角色強化等級 (Character Upgrades)", GUI.skin.box);
        EditorGUILayout.Space(15);

        string[] charNames = new string[] { "角色 0 (戰士 Fighter)", "角色 1 (法師 Mage)", "角色 2 (遊俠/刺客)", "角色 3", "角色 4" };
        selectedCharID = EditorGUILayout.Popup("選擇目標角色:", selectedCharID, charNames);

        int currentLevel = GetCharLevel(selectedCharID);
        EditorGUILayout.LabelField("當前強化等級:", $"Lv.{currentLevel} / 10", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("重置為 Lv.0"))
        {
            SetCharLevel(selectedCharID, 0);
        }
        if (GUILayout.Button("-1 級"))
        {
            SetCharLevel(selectedCharID, Mathf.Max(0, currentLevel - 1));
        }
        if (GUILayout.Button("+1 級"))
        {
            SetCharLevel(selectedCharID, Mathf.Min(10, currentLevel + 1));
        }
        if (GUILayout.Button("滿級 Lv.10"))
        {
            SetCharLevel(selectedCharID, 10);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawQuickActionsSection()
    {
        GUILayout.BeginVertical("全局批量操作", GUI.skin.box);
        EditorGUILayout.Space(15);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 重置所有角色為 Lv.0", GUILayout.Height(28)))
        {
            ResetAllCharacterLevels();
        }
        if (GUILayout.Button("⭐ 一鍵滿級所有角色 (Lv.10)", GUILayout.Height(28)))
        {
            MaxAllCharacterLevels();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        if (GUILayout.Button("🧹 清空所有靈魂與養成數據 (全面重置)", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("確認全面重置？", "此操作將清空玩家的所有靈魂幣與所有角色的養成等級！", "確認清空", "取消"))
            {
                QuickResetAllData();
            }
        }

        GUILayout.EndVertical();
    }

    #region 資料操作核心邏輯
    public static int GetCurrentSouls()
    {
        if (Application.isPlaying && MetaProgressionManager.Instance != null)
        {
            return MetaProgressionManager.Instance.TotalCurrency;
        }
        return PlayerPrefs.GetInt(MetaProgressionManager.PREFS_KEY_SOULS, 0);
    }

    public static void SetSouls(int amount)
    {
        amount = Mathf.Max(0, amount);
        PlayerPrefs.SetInt(MetaProgressionManager.PREFS_KEY_SOULS, amount);
        PlayerPrefs.Save();

        if (Application.isPlaying && MetaProgressionManager.Instance != null)
        {
            MetaProgressionManager.Instance.LoadData();
            MetaProgressionManager.Instance.SaveData();
        }
        else
        {
            UIEvents.MetaCurrencyChanged(amount);
        }

        NotifyAllUI();
        Debug.Log($"✦ [MetaDebugger] 持有靈魂已設定為: {amount:N0}");
    }

    public static void AddSouls(int delta)
    {
        int cur = GetCurrentSouls();
        SetSouls(cur + delta);
    }

    public static int GetCharLevel(int characterID)
    {
        if (Application.isPlaying && CharacterUpgradeManager.Instance != null)
        {
            return CharacterUpgradeManager.Instance.GetCharacterLevel(characterID);
        }
        return PlayerPrefs.GetInt($"MetaUpgrade_Char_{characterID}_Level", 0);
    }

    public static void SetCharLevel(int characterID, int level)
    {
        level = Mathf.Clamp(level, 0, 10);
        PlayerPrefs.SetInt($"MetaUpgrade_Char_{characterID}_Level", level);
        PlayerPrefs.DeleteKey($"MetaUpgrade_{characterID}_Health");
        PlayerPrefs.DeleteKey($"MetaUpgrade_{characterID}_Defense");
        PlayerPrefs.DeleteKey($"MetaUpgrade_{characterID}_Attack");
        PlayerPrefs.Save();

        NotifyAllUI();
        Debug.Log($"🔄 [MetaDebugger] 角色 [{characterID}] 等級已設定為 Lv.{level}");
    }

    public static void ResetAllCharacterLevels()
    {
        for (int i = 0; i < 10; i++)
        {
            SetCharLevel(i, 0);
        }
        NotifyAllUI();
        Debug.Log("🔄 [MetaDebugger] 所有角色升級等級已重置為 0！");
    }

    public static void MaxAllCharacterLevels()
    {
        for (int i = 0; i < 10; i++)
        {
            SetCharLevel(i, 10);
        }
        NotifyAllUI();
        Debug.Log("⭐ [MetaDebugger] 所有角色已設定為滿級 Lv.10！");
    }

    private static void NotifyAllUI()
    {
        CharacterUpgradeManager.TriggerAnyUpgradeChanged();

        // 觸發大廳升級面板與左側屬性面板刷新
        CharacterUpgradeUIController upgradeUI = Object.FindFirstObjectByType<CharacterUpgradeUIController>();
        if (upgradeUI != null && upgradeUI.isActiveAndEnabled)
        {
            upgradeUI.RefreshAllUI();
        }
    }
    #endregion
}
#endif
