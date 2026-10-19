#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

public class ResultUIGeneratorTool : EditorWindow
{
    private TMP_FontAsset customFont;
    private const string ASSET_DIR = "Assets/Sprites/UI_Result";

    [MenuItem("Tools/⚔️ 一鍵生成結算畫面 UI (版本C)")]
    public static void ShowWindow()
    {
        var win = GetWindow<ResultUIGeneratorTool>("結算 UI 生成器");
        win.minSize = new Vector2(380, 190);
        win.AutoDetectChineseFont();
    }

    private void AutoDetectChineseFont()
    {
        if (customFont != null) return;
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.Contains("LiberationSans"))
            {
                customFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                break;
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(12);
        EditorGUILayout.LabelField("⚔️ 2D 結算頁面 UI 自動建置工具 (版本 C)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "點擊下方按鈕後，工具將自動：\n" +
            "1. 生成 14 張高畫質透明 PNG 素材至 Assets/Sprites/UI_Result\n" +
            "2. 在當前場景建立 1920×1080 Canvas 與版本 C 完整排版\n" +
            "3. 自動掛載 ResultUIController 並綁定所有欄位與按鈕事件",
            MessageType.Info);

        GUILayout.Space(8);
        customFont = (TMP_FontAsset)EditorGUILayout.ObjectField("中文字型 (TMP_FontAsset)", customFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(14);
        GUI.backgroundColor = new Color(0.88f, 0.62f, 0.24f);
        if (GUILayout.Button("🚀 立即一鍵生成 PNG 素材與結算 UI", GUILayout.Height(40)))
        {
            GenerateCompleteResultUI();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateCompleteResultUI()
    {
        if (!Directory.Exists(ASSET_DIR))
        {
            Directory.CreateDirectory(ASSET_DIR);
        }

        EditorUtility.DisplayProgressBar("生成結算 UI", "正在繪製並匯入 PNG 材質素材...", 0.25f);

        // 1. 生成全部 14 張 PNG 素材並取得 Sprite
        Sprite bgDefeat = CreateAndLoadSprite("01_verC_bg_defeat.png", GenerateBgTex(false));
        Sprite bgVictory = CreateAndLoadSprite("02_verC_bg_victory.png", GenerateBgTex(true));
        Sprite headerDefeat = CreateAndLoadSprite("03_verC_header_defeat.png", GenerateHeaderTex(new Color32(220, 38, 38, 255)));
        Sprite headerVictory = CreateAndLoadSprite("04_verC_header_victory.png", GenerateHeaderTex(new Color32(224, 159, 62, 255)));
        Sprite cardGold = CreateAndLoadSprite("05_verC_stat_card_gold.png", GenerateStatCardTex(new Color32(224, 159, 62, 255)));
        Sprite cardNormal = CreateAndLoadSprite("06_verC_stat_card_normal.png", GenerateStatCardTex(new Color32(214, 195, 165, 255)));
        Sprite centerDiv = CreateAndLoadSprite("07_verC_center_divider.png", GenerateDividerTex(1706, 20, new Color32(212, 163, 89, 130)));
        Sprite charTitleRow = CreateAndLoadSprite("08_verC_char_title_row.png", GenerateCharTitleRowTex(580, 82));
        Sprite charStatRow = CreateAndLoadSprite("09_verC_char_stat_row.png", GenerateCharStatRowTex(580, 74));
        Sprite weaponBar = CreateAndLoadSprite("10_verC_weapon_bar.png", GenerateWeaponBarTex(1072, 104));
        Sprite swordIcon = CreateAndLoadSprite("11_verC_icon_sword.png", GenerateSwordIconTex(64, 64));
        Sprite armorSlot = CreateAndLoadSprite("12_verC_armor_slot.png", GenerateArmorSlotTex(341, 108));
        Sprite btnMenuRed = CreateAndLoadSprite("13_verC_btn_menu_red.png", GenerateButtonTex(330, 82, false));
        Sprite btnRetryGold = CreateAndLoadSprite("14_verC_btn_retry_gold.png", GenerateButtonTex(330, 82, true));

        EditorUtility.DisplayProgressBar("生成結算 UI", "正在場景中搭建 Canvas 與 UI 階層...", 0.7f);

        // 2. 建立或取得 Canvas 與 EventSystem
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("ResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // 若場景已有舊的 ResultUI_VerC 則先移除重建
        Transform oldRoot = canvas.transform.Find("ResultUI_VerC");
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);

        GameObject rootGO = CreateUIObject("ResultUI_VerC", canvas.transform);
        StretchFull(rootGO.GetComponent<RectTransform>());
        ResultUIController ctrl = rootGO.AddComponent<ResultUIController>();

        ctrl.defeatBgSprite = bgDefeat;
        ctrl.victoryBgSprite = bgVictory;
        ctrl.defeatHeaderSprite = headerDefeat;
        ctrl.victoryHeaderSprite = headerVictory;

        // --- A. 2D 暗角石牆背景 ---
        GameObject bgGO = CreateImage("Background_2D", rootGO.transform, bgDefeat, Vector2.zero, Vector2.zero);
        StretchFull(bgGO.GetComponent<RectTransform>());
        ctrl.backgroundImage = bgGO.GetComponent<Image>();

        // --- B. 頂部標題橫幅 (殞命地牢) ---
        GameObject headerGO = CreateImage("Header_Banner", rootGO.transform, headerDefeat, new Vector2(0, 445), new Vector2(860, 114));
        ctrl.headerBannerImage = headerGO.GetComponent<Image>();
        ctrl.mainTitleText = CreateText("MainTitle_Text", headerGO.transform, "殞 命 地 牢", 54, new Color32(252, 165, 165, 255), new Vector2(0, 12), new Vector2(800, 70), FontStyles.Bold);
        ctrl.subTitleText = CreateText("SubTitle_Text", headerGO.transform, "EXPEDITION FAILED", 20, new Color32(156, 163, 175, 255), new Vector2(0, -32), new Vector2(600, 34), FontStyles.Normal);

        // --- C. 頂部四連戰績卡 (遊戲進度、遊玩時間、擊殺敵數、獲得金幣) ---
        GameObject topCardsGO = CreateUIObject("TopCardsGroup", rootGO.transform);
        RectTransform topCardsRT = topCardsGO.GetComponent<RectTransform>();
        topCardsRT.anchoredPosition = new Vector2(0, 248);
        topCardsRT.sizeDelta = new Vector2(1706, 195);
        ctrl.topCardsGroup = topCardsGO.AddComponent<CanvasGroup>();

        HorizontalLayoutGroup topHLG = topCardsGO.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 34f;
        topHLG.childAlignment = TextAnchor.MiddleCenter;
        topHLG.childControlWidth = false;
        topHLG.childControlHeight = false;

        ctrl.stageProgressText = BuildStatCard(topCardsGO.transform, "Card_Progress", cardGold, "遊 戲 進 度", "1 - 3", "深淵石牢・第三區", new Color32(224, 159, 62, 255), out ctrl.stageNameText);
        TMP_Text dummy1, dummy2, dummy3;
        ctrl.playTimeText = BuildStatCard(topCardsGO.transform, "Card_PlayTime", cardNormal, "遊 玩 時 間", "00 : 18 : 42", "TOTAL TIME", new Color32(245, 239, 230, 255), out dummy1);
        ctrl.killCountText = BuildStatCard(topCardsGO.transform, "Card_Kills", cardNormal, "擊 殺 敵 數", "148", "ENEMIES SLAIN", new Color32(245, 239, 230, 255), out dummy2);
        ctrl.goldCountText = BuildStatCard(topCardsGO.transform, "Card_Gold", cardNormal, "獲 得 金 幣", "1,260 G", "GOLD COLLECTED", new Color32(245, 239, 230, 255), out dummy3);

        // --- D. 中央魂系長分隔線 ---
        CreateImage("Center_Divider", rootGO.transform, centerDiv, new Vector2(0, 105), new Vector2(1706, 20));

        // --- E. 左下角色與屬性區 ---
        GameObject leftGroupGO = CreateUIObject("LeftStatsPanel", rootGO.transform);
        RectTransform leftRT = leftGroupGO.GetComponent<RectTransform>();
        leftRT.anchoredPosition = new Vector2(-563, -108);
        leftRT.sizeDelta = new Vector2(580, 350);
        ctrl.leftStatsPanel = leftGroupGO.AddComponent<CanvasGroup>();

        VerticalLayoutGroup leftVLG = leftGroupGO.AddComponent<VerticalLayoutGroup>();
        leftVLG.spacing = 16f;
        leftVLG.childAlignment = TextAnchor.UpperCenter;
        leftVLG.childControlWidth = false;
        leftVLG.childControlHeight = false;

        // 角色名稱標題列
        GameObject charTitleGO = CreateImage("Row_CharTitle", leftGroupGO.transform, charTitleRow, Vector2.zero, new Vector2(580, 82));
        ctrl.characterNameText = CreateText("CharName_Text", charTitleGO.transform, "角色：戰士", 32, new Color32(245, 239, 230, 255), new Vector2(24, 0), new Vector2(500, 60), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

        // 三條屬性列 (生命 HP / 防禦 DEF / 攻擊力 ATK)
        ctrl.hpText = BuildCharStatRow(leftGroupGO.transform, "Row_HP", charStatRow, "生命 HP", "160");
        ctrl.defText = BuildCharStatRow(leftGroupGO.transform, "Row_DEF", charStatRow, "防禦 DEF", "89");
        ctrl.atkText = BuildCharStatRow(leftGroupGO.transform, "Row_ATK", charStatRow, "攻擊力 ATK", "100");

        // --- F. 右下主武器與六部位裝備區 ---
        GameObject rightGroupGO = CreateUIObject("RightEquipPanel", rootGO.transform);
        RectTransform rightRT = rightGroupGO.GetComponent<RectTransform>();
        rightRT.anchoredPosition = new Vector2(317, -108);
        rightRT.sizeDelta = new Vector2(1072, 350);
        ctrl.rightEquipPanel = rightGroupGO.AddComponent<CanvasGroup>();

        // 主武器長槽
        GameObject weaponBarGO = CreateImage("Weapon_MainBar", rightGroupGO.transform, weaponBar, new Vector2(0, 123), new Vector2(1072, 104));
        GameObject wIconGO = CreateImage("Weapon_Icon", weaponBarGO.transform, swordIcon, new Vector2(-470, 0), new Vector2(60, 60));
        ctrl.weaponIconImage = wIconGO.GetComponent<Image>();
        ctrl.weaponNameText = CreateText("Weapon_Name", weaponBarGO.transform, "主武器：噬星者", 31, new Color32(224, 159, 62, 255), new Vector2(-115, 0), new Vector2(600, 60), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        ctrl.weaponSubText = CreateText("Weapon_Sub", weaponBarGO.transform, "單手長劍 | 攻擊 +42", 24, new Color32(168, 162, 158, 255), new Vector2(240, 0), new Vector2(520, 60), FontStyles.Normal, TextAlignmentOptions.MidlineRight);

        // 六格防具 Grid (3欄 x 2列)
        GameObject armorGridGO = CreateUIObject("Armor_Grid", rightGroupGO.transform);
        RectTransform gridRT = armorGridGO.GetComponent<RectTransform>();
        gridRT.anchoredPosition = new Vector2(0, -56);
        gridRT.sizeDelta = new Vector2(1072, 238);

        GridLayoutGroup glg = armorGridGO.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(341, 108);
        glg.spacing = new Vector2(24, 22);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 3;
        glg.childAlignment = TextAnchor.MiddleCenter;

        string[] slotTitles = { "頭盔", "胸甲", "護腿", "手套", "腰帶", "戰靴" };
        string[] defaultItemNames = { "鍛鐵戰盔", "岩峰重鎧", "灰岩護腿", "鎖子鐵腕", "重裝扣帶", "暗鐵脛靴" };
        ctrl.armorNameTexts = new TMP_Text[6];
        for (int i = 0; i < 6; i++)
        {
            ctrl.armorNameTexts[i] = BuildArmorSlot(armorGridGO.transform, $"ArmorSlot_{i}", armorSlot, slotTitles[i], defaultItemNames[i]);
        }

        // --- G. 底部雙按鈕 (返回主選單 & 再次挑戰) ---
        GameObject bottomBtnsGO = CreateUIObject("BottomBtnsGroup", rootGO.transform);
        RectTransform botRT = bottomBtnsGO.GetComponent<RectTransform>();
        botRT.anchoredPosition = new Vector2(0, -422);
        botRT.sizeDelta = new Vector2(720, 86);
        ctrl.bottomBtnsGroup = bottomBtnsGO.AddComponent<CanvasGroup>();

        HorizontalLayoutGroup botHLG = bottomBtnsGO.AddComponent<HorizontalLayoutGroup>();
        botHLG.spacing = 52f;
        botHLG.childAlignment = TextAnchor.MiddleCenter;
        botHLG.childControlWidth = false;
        botHLG.childControlHeight = false;

        Button btnMenu = BuildActionButton(bottomBtnsGO.transform, "Btn_MainMenu", btnMenuRed, "返回主選單", false);
        Button btnRetry = BuildActionButton(bottomBtnsGO.transform, "Btn_Retry", btnRetryGold, "再 次 挑 戰", true);

        UnityEventTools.AddPersistentListener(btnMenu.onClick, ctrl.OnClickMainMenu);
        UnityEventTools.AddPersistentListener(btnRetry.onClick, ctrl.OnClickRetry);

        EditorUtility.SetDirty(ctrl);
        Undo.RegisterCreatedObjectUndo(rootGO, "Create Result UI VerC");
        Selection.activeGameObject = rootGO;
        EditorUtility.ClearProgressBar();

        Debug.Log("✅ [ResultUIGeneratorTool] 版本 C 結算畫面與 14 張 PNG 素材已全自動建立並完成綁定！");
    }

    // =========================================================================
    // UI 元件組裝輔助函式
    // =========================================================================
    private TMP_Text BuildStatCard(Transform parent, string name, Sprite bg, string label, string mainVal, string subVal, Color32 valColor, out TMP_Text subTextOut)
    {
        GameObject card = CreateImage(name, parent, bg, Vector2.zero, new Vector2(400, 195));
        CreateText("Label", card.transform, label, 24, new Color32(168, 162, 158, 255), new Vector2(0, 56), new Vector2(360, 40), FontStyles.Normal);
        TMP_Text mainTxt = CreateText("Value", card.transform, mainVal, 50, valColor, new Vector2(0, -2), new Vector2(380, 70), FontStyles.Bold);
        subTextOut = CreateText("Sub", card.transform, subVal, 20, new Color32(120, 113, 108, 255), new Vector2(0, -58), new Vector2(360, 34), FontStyles.Normal);
        return mainTxt;
    }

    private TMP_Text BuildCharStatRow(Transform parent, string name, Sprite bg, string label, string val)
    {
        GameObject row = CreateImage(name, parent, bg, Vector2.zero, new Vector2(580, 74));
        CreateText("Label", row.transform, label, 27, new Color32(229, 224, 216, 255), new Vector2(22, 0), new Vector2(300, 54), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        return CreateText("Value", row.transform, val, 30, new Color32(230, 213, 184, 255), new Vector2(-10, 0), new Vector2(240, 54), FontStyles.Bold, TextAlignmentOptions.MidlineRight);
    }

    private TMP_Text BuildArmorSlot(Transform parent, string name, Sprite bg, string slotName, string itemName)
    {
        GameObject slot = CreateImage(name, parent, bg, Vector2.zero, new Vector2(341, 108));
        CreateText("Slot_Category", slot.transform, slotName, 20, new Color32(140, 134, 126, 255), new Vector2(24, 20), new Vector2(260, 34), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        return CreateText("Item_Name", slot.transform, itemName, 26, new Color32(245, 239, 230, 255), new Vector2(24, -16), new Vector2(260, 42), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
    }

    private Button BuildActionButton(Transform parent, string name, Sprite bg, string text, bool isBold)
    {
        GameObject go = CreateImage(name, parent, bg, Vector2.zero, new Vector2(330, 82));
        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        go.AddComponent<UIButtonTween>();
        CreateText("Text", go.transform, text, 29, new Color32(255, 255, 255, 255), Vector2.zero, new Vector2(300, 64), isBold ? FontStyles.Bold : FontStyles.Normal);
        return btn;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private GameObject CreateImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        return go;
    }

    private TMP_Text CreateText(string name, Transform parent, string content, float fontSize, Color32 color, Vector2 pos, Vector2 size, FontStyles style, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (customFont != null) tmp.font = customFont;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // =========================================================================
    // 像素級程序化 PNG 繪製引擎 (自動生成微括號、斜切角、金菱形與漸層細線)
    // =========================================================================
    private Sprite CreateAndLoadSprite(string fileName, Texture2D tex)
    {
        string path = Path.Combine(ASSET_DIR, fileName).Replace("\\", "/");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private Texture2D NewClearTex(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] cols = new Color32[w * h];
        t.SetPixels32(cols);
        return t;
    }

    private Texture2D GenerateBgTex(bool victory)
    {
        int w = 960, h = 540;
        Texture2D t = NewClearTex(w, h);
        for (int y = 0; y < h; y++)
        {
            int row = y / 48;
            int shift = (row % 2 == 0) ? 0 : 45;
            for (int x = 0; x < w; x++)
            {
                bool isBrickLine = (y % 48 < 2) || ((x + shift) % 90 < 2);
                Color baseCol = isBrickLine ? new Color(0.04f, 0.05f, 0.06f, 1f) : new Color(0.10f, 0.11f, 0.14f, 1f);

                float dx = (x - w * 0.5f) / 400f;
                float dy = (y - (h - 60f)) / 380f;
                float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));

                Color glowCol = victory ? new Color(0.88f, 0.62f, 0.24f, 1f) : new Color(0.60f, 0.11f, 0.11f, 1f);
                Color darkVignette = new Color(0.04f, 0.04f, 0.05f, 1f);

                Color c = Color.Lerp(baseCol, glowCol, (1f - dist) * 0.24f);
                c = Color.Lerp(c, darkVignette, Mathf.SmoothStep(0f, 1f, dist * 0.94f));
                c.a = 1f;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D GenerateHeaderTex(Color32 topLineCol)
    {
        int w = 860, h = 114;
        Texture2D t = NewClearTex(w, h);
        for (int y = 8; y < h - 8; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float nx = Mathf.Abs(x - w * 0.5f) / (w * 0.5f);
                float a = Mathf.Clamp01((1f - nx) * 1.35f) * 0.82f;
                t.SetPixel(x, y, new Color(0.055f, 0.05f, 0.047f, a));
            }
        }
        DrawGradDividerOnTex(t, w / 2, h - 8, 760, topLineCol);
        DrawGradDividerOnTex(t, w / 2, 8, 760, new Color32(212, 163, 89, 120));
        t.Apply();
        return t;
    }

    private Texture2D GenerateStatCardTex(Color32 bracketCol)
    {
        int w = 400, h = 195;
        Texture2D t = NewClearTex(w, h);
        FillRect(t, 5, 5, w - 10, h - 10, new Color32(20, 19, 18, 215));
        DrawBracketsOnTex(t, 2, 2, w - 4, h - 4, 18, 3, bracketCol);
        DrawGradDividerOnTex(t, w / 2, 8, w - 56, new Color32(212, 163, 89, 95));
        t.Apply();
        return t;
    }

    private Texture2D GenerateDividerTex(int w, int h, Color32 col)
    {
        Texture2D t = NewClearTex(w, h);
        DrawGradDividerOnTex(t, w / 2, h / 2, w, col);
        t.Apply();
        return t;
    }

    private Texture2D GenerateCharTitleRowTex(int w, int h)
    {
        Texture2D t = NewClearTex(w, h);
        FillChamfer(t, 0, 0, w, h, 10, new Color32(48, 43, 39, 200), new Color32(0, 0, 0, 0));
        DrawDiamondOnTex(t, 30, h / 2, 7, new Color32(224, 159, 62, 255));
        t.Apply();
        return t;
    }

    private Texture2D GenerateCharStatRowTex(int w, int h)
    {
        Texture2D t = NewClearTex(w, h);
        FillChamfer(t, 0, 0, w, h, 9, new Color32(48, 43, 39, 175), new Color32(0, 0, 0, 0));
        FillRect(t, 10, 1, w - 20, 2, new Color32(212, 163, 89, 65));
        DrawDiamondOnTex(t, 26, h / 2, 6, new Color32(224, 159, 62, 255));
        t.Apply();
        return t;
    }

    private Texture2D GenerateWeaponBarTex(int w, int h)
    {
        Texture2D t = NewClearTex(w, h);
        FillChamfer(t, 0, 0, w, h, 11, new Color32(32, 28, 25, 218), new Color32(0, 0, 0, 0));
        DrawBracketsOnTex(t, 8, 8, w - 16, h - 16, 15, 3, new Color32(224, 159, 62, 255));
        t.Apply();
        return t;
    }

    private Texture2D GenerateSwordIconTex(int w, int h)
    {
        Texture2D t = NewClearTex(w, h);
        float cx = w * 0.5f, cy = h * 0.5f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx, dy = y - cy;
                float rx = dx * 0.7071f - dy * 0.7071f;
                float ry = dx * 0.7071f + dy * 0.7071f;
                if (Mathf.Abs(rx) <= 3.2f && ry >= -10f && ry <= 24f)
                    t.SetPixel(x, y, new Color32(230, 213, 184, 255));
                else if (Mathf.Abs(rx) <= 13f && ry >= -15f && ry <= -10f)
                    t.SetPixel(x, y, new Color32(224, 159, 62, 255));
                else if (Mathf.Abs(rx) <= 3.2f && ry >= -24f && ry < -15f)
                    t.SetPixel(x, y, new Color32(224, 159, 62, 255));
            }
        }
        t.Apply();
        return t;
    }

    private Texture2D GenerateArmorSlotTex(int w, int h)
    {
        Texture2D t = NewClearTex(w, h);
        FillRect(t, 4, 4, w - 8, h - 8, new Color32(22, 20, 19, 214));
        DrawBracketsOnTex(t, 2, 2, w - 4, h - 4, 13, 2, new Color32(214, 195, 165, 145));
        DrawDiamondOnTex(t, 34, h / 2, 6, new Color32(214, 195, 165, 255));
        t.Apply();
        return t;
    }

    private Texture2D GenerateButtonTex(int w, int h, bool isGold)
    {
        Texture2D t = NewClearTex(w, h);
        Color32 fill = isGold ? new Color32(150, 88, 22, 232) : new Color32(135, 28, 28, 218);
        Color32 border = isGold ? new Color32(245, 208, 125, 195) : new Color32(235, 110, 110, 155);
        FillChamfer(t, 2, 2, w - 4, h - 4, 11, fill, border);
        if (isGold)
        {
            DrawDiamondOnTex(t, 26, h / 2, 5, new Color32(245, 208, 125, 255));
            DrawDiamondOnTex(t, w - 26, h / 2, 5, new Color32(245, 208, 125, 255));
        }
        t.Apply();
        return t;
    }

    private void FillRect(Texture2D t, int x0, int y0, int rw, int rh, Color32 col)
    {
        for (int y = y0; y < y0 + rh && y < t.height; y++)
            for (int x = x0; x < x0 + rw && x < t.width; x++)
                if (x >= 0 && y >= 0) t.SetPixel(x, y, col);
    }

    private void FillChamfer(Texture2D t, int x0, int y0, int rw, int rh, int ch, Color32 fill, Color32 stroke)
    {
        for (int y = 0; y < rh; y++)
        {
            for (int x = 0; x < rw; x++)
            {
                int dx = Mathf.Min(x, rw - 1 - x);
                int dy = Mathf.Min(y, rh - 1 - y);
                if (dx + dy < ch) continue;

                bool isBorder = (dx <= 1 || dy <= 1 || dx + dy <= ch + 2);
                if (isBorder && stroke.a > 0)
                    t.SetPixel(x0 + x, y0 + y, stroke);
                else
                    t.SetPixel(x0 + x, y0 + y, fill);
            }
        }
    }

    private void DrawBracketsOnTex(Texture2D t, int x0, int y0, int rw, int rh, int len, int thick, Color32 col)
    {
        // 左下
        FillRect(t, x0, y0, len, thick, col);
        FillRect(t, x0, y0, thick, len, col);
        // 右下
        FillRect(t, x0 + rw - len, y0, len, thick, col);
        FillRect(t, x0 + rw - thick, y0, thick, len, col);
        // 左上
        FillRect(t, x0, y0 + rh - thick, len, thick, col);
        FillRect(t, x0, y0 + rh - len, thick, len, col);
        // 右上
        FillRect(t, x0 + rw - len, y0 + rh - thick, len, thick, col);
        FillRect(t, x0 + rw - thick, y0 + rh - len, thick, len, col);
    }

    private void DrawDiamondOnTex(Texture2D t, int cx, int cy, int r, Color32 col)
    {
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= r)
                    t.SetPixel(cx + dx, cy + dy, col);
    }

    private void DrawGradDividerOnTex(Texture2D t, int cx, int cy, int width, Color32 col)
    {
        int half = width / 2;
        for (int x = cx - half; x <= cx + half; x++)
        {
            if (x < 0 || x >= t.width) continue;
            float norm = Mathf.Abs(x - cx) / (float)half;
            byte a = (byte)(col.a * Mathf.Clamp01(1f - norm));
            t.SetPixel(x, cy, new Color32(col.r, col.g, col.b, a));
            if (cy + 1 < t.height) t.SetPixel(x, cy + 1, new Color32(col.r, col.g, col.b, (byte)(a / 2)));
        }
        DrawDiamondOnTex(t, cx, cy, 6, col);
        DrawDiamondOnTex(t, cx, cy, 3, new Color32(20, 19, 18, 255));
    }
}
#endif