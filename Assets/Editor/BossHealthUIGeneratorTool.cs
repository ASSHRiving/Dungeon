#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class BossHealthUIGeneratorTool : EditorWindow
{
    private TMP_FontAsset customFont;
    private const string ASSET_DIR = "Assets/Sprites/UI_BossBar";

    [MenuItem("Tools/⚔️ 一鍵生成 Boss 螢幕血條 (Top-Center)")]
    public static void ShowWindow()
    {
        var win = GetWindow<BossHealthUIGeneratorTool>("Boss 血條生成器");
        win.minSize = new Vector2(380, 180);
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
        GUILayout.Space(10);
        EditorGUILayout.LabelField("⚔️ Boss 獨立螢幕血條自動建置工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "點擊下方按鈕將自動：\n" +
            "1. 繪製 4 張透明 PNG (底板、金殘影、暗紅主條、微括號+50%菱形外框)\n" +
            "2. 在當前 HUD Canvas 正上方 (Top-Center) 建立完整階層\n" +
            "3. 自動掛載 BossHealthUIController 並完成所有欄位綁定",
            MessageType.Info);

        GUILayout.Space(6);
        customFont = (TMP_FontAsset)EditorGUILayout.ObjectField("中文字型 (TMP_FontAsset)", customFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(12);
        GUI.backgroundColor = new Color(0.88f, 0.62f, 0.24f);
        if (GUILayout.Button("🚀 立即一鍵生成 Boss 螢幕血條", GUILayout.Height(38)))
        {
            BuildBossHealthUI();
        }
        GUI.backgroundColor = Color.white;
    }

    private void BuildBossHealthUI()
    {
        if (!Directory.Exists(ASSET_DIR))
            Directory.CreateDirectory(ASSET_DIR);

        EditorUtility.DisplayProgressBar("生成 Boss 血條", "正在生成 4 張高畫質透明 PNG 素材...", 0.3f);

        Sprite bgSprite = SaveAndImportSprite("boss_bar_bg.png", GenerateBgTex(880, 28));
        Sprite chipSprite = SaveAndImportSprite("boss_bar_fill_chip.png", GenerateChipFillTex(864, 16));
        Sprite redSprite = SaveAndImportSprite("boss_bar_fill_red.png", GenerateRedFillTex(864, 16));
        Sprite frameSprite = SaveAndImportSprite("boss_bar_frame_overlay.png", GenerateFrameOverlayTex(880, 28));

        EditorUtility.DisplayProgressBar("生成 Boss 血條", "正在場景中組裝 Top-Center UI 階層...", 0.75f);

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGO = new GameObject("HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = cGO.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
        }

        Transform old = canvas.transform.Find("BossHealthHUD_TopCenter");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        // 1. 根物件 (錨點固定於 Top-Center 正上方置中)
        GameObject rootGO = new GameObject("BossHealthHUD_TopCenter", typeof(RectTransform));
        rootGO.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.anchorMin = new Vector2(0.5f, 1f);
        rootRT.anchorMax = new Vector2(0.5f, 1f);
        rootRT.pivot = new Vector2(0.5f, 1f);
        rootRT.anchoredPosition = new Vector2(0f, -36f);
        rootRT.sizeDelta = new Vector2(880f, 76f);

        CanvasGroup cg = rootGO.AddComponent<CanvasGroup>();
        cg.alpha = 1f; // 在編輯器中預設設為 1 方便預覽，執行時 Awake 會自動隱藏
        cg.blocksRaycasts = false;

        BossHealthUIController ctrl = rootGO.AddComponent<BossHealthUIController>();
        ctrl.rootCanvasGroup = cg;

        // 2. 動態縮放與震動容器
        GameObject containerGO = new GameObject("Bar_Container", typeof(RectTransform));
        containerGO.transform.SetParent(rootGO.transform, false);
        RectTransform containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.anchoredPosition = Vector2.zero;
        containerRT.sizeDelta = new Vector2(880f, 76f);
        ctrl.barContainerRect = containerRT;

        // 3. Boss 名稱文字 (左上方對齊)
        GameObject nameGO = new GameObject("BossName_Text", typeof(RectTransform));
        nameGO.transform.SetParent(containerGO.transform, false);
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchoredPosition = new Vector2(4f, 18f);
        nameRT.sizeDelta = new Vector2(872f, 36f);
        TextMeshProUGUI nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        if (customFont != null) nameTmp.font = customFont;
        nameTmp.text = "<color=#E09F3E>◆</color> 深淵守衛・阿斯塔";
        nameTmp.fontSize = 26f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = new Color32(245, 239, 230, 255);
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameTmp.raycastTarget = false;
        ctrl.bossNameText = nameTmp;

        // 4. 血條底槽 (Bar_Back)
        GameObject bgGO = CreateImg("Bar_Back", containerGO.transform, bgSprite, new Vector2(0f, -14f), new Vector2(880f, 28f));

        // 5. 香檳金殘影條 (Bar_Chip_Fill - Image.Type.Filled)
        GameObject chipGO = CreateImg("Bar_Chip_Fill", bgGO.transform, chipSprite, Vector2.zero, new Vector2(864f, 16f));
        Image chipImg = chipGO.GetComponent<Image>();
        chipImg.type = Image.Type.Filled;
        chipImg.fillMethod = Image.FillMethod.Horizontal;
        chipImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        chipImg.fillAmount = 1f;
        ctrl.chipFillImage = chipImg;

        // 6. 暗紅主血條 (Bar_Main_Fill - Image.Type.Filled)
        GameObject mainGO = CreateImg("Bar_Main_Fill", bgGO.transform, redSprite, Vector2.zero, new Vector2(864f, 16f));
        Image mainImg = mainGO.GetComponent<Image>();
        mainImg.type = Image.Type.Filled;
        mainImg.fillMethod = Image.FillMethod.Horizontal;
        mainImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        mainImg.fillAmount = 1f;
        ctrl.mainFillImage = mainImg;

        // 7. 微括號外框 + 中央 50% 二階段金菱形刻度 (Bar_Frame_Overlay)
        CreateImg("Bar_Frame_Overlay", bgGO.transform, frameSprite, Vector2.zero, new Vector2(880f, 28f));

        EditorUtility.SetDirty(ctrl);
        Undo.RegisterCreatedObjectUndo(rootGO, "Create Boss Health UI");
        Selection.activeGameObject = rootGO;
        EditorUtility.ClearProgressBar();

        Debug.Log("✅ [BossHealthUIGeneratorTool] Boss 螢幕血條已建立於 Canvas 正上方！進入 Play 模式後可對 BossHealthUIController 按右鍵測試登場與扣血效果。");
    }

    private GameObject CreateImg(string name, Transform parent, Sprite sp, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.sprite = sp;
        img.raycastTarget = false;
        return go;
    }

    private Sprite SaveAndImportSprite(string fileName, Texture2D tex)
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

    private Texture2D NewTex(int w, int h)
    {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels32(new Color32[w * h]);
        return t;
    }

    private Texture2D GenerateBgTex(int w, int h)
    {
        Texture2D t = NewTex(w, h);
        FillRect(t, 4, 4, w - 8, h - 8, new Color32(18, 16, 15, 232));
        t.Apply();
        return t;
    }

    private Texture2D GenerateChipFillTex(int w, int h)
    {
        Texture2D t = NewTex(w, h);
        for (int y = 0; y < h; y++)
        {
            float ratio = y / (float)(h - 1);
            Color c = Color.Lerp(new Color(0.78f, 0.66f, 0.49f, 1f), new Color(0.96f, 0.93f, 0.88f, 1f), ratio);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }
        t.Apply();
        return t;
    }

    private Texture2D GenerateRedFillTex(int w, int h)
    {
        Texture2D t = NewTex(w, h);
        for (int y = 0; y < h; y++)
        {
            float ratio = y / (float)(h - 1);
            Color c = Color.Lerp(new Color(0.36f, 0.07f, 0.07f, 1f), new Color(0.86f, 0.15f, 0.15f, 1f), ratio);
            if (y >= h - 3) c = Color.Lerp(c, new Color(1f, 0.7f, 0.7f, 1f), 0.25f);
            for (int x = 0; x < w; x++) t.SetPixel(x, y, c);
        }
        t.Apply();
        return t;
    }

    private Texture2D GenerateFrameOverlayTex(int w, int h)
    {
        Texture2D t = NewTex(w, h);
        // 內細金框
        Color32 innerGold = new Color32(212, 163, 89, 100);
        FillRect(t, 4, 4, w - 8, 1, innerGold);
        FillRect(t, 4, h - 5, w - 8, 1, innerGold);
        FillRect(t, 4, 4, 1, h - 8, innerGold);
        FillRect(t, w - 5, 4, 1, h - 8, innerGold);

        // 四角 2px 微括號 ⌜ ⌝
        Color32 bracketCol = new Color32(230, 213, 184, 255);
        int lx = 18, ly = 9, th = 2;
        FillRect(t, 0, 0, lx, th, bracketCol); FillRect(t, 0, 0, th, ly, bracketCol);
        FillRect(t, w - lx, 0, lx, th, bracketCol); FillRect(t, w - th, 0, th, ly, bracketCol);
        FillRect(t, 0, h - th, lx, th, bracketCol); FillRect(t, 0, h - ly, th, ly, bracketCol);
        FillRect(t, w - lx, h - th, lx, th, bracketCol); FillRect(t, w - th, h - ly, th, ly, bracketCol);

        // 正中央 50% 二階段刻度線與上下微雕金菱形 ◆
        int mid = w / 2;
        FillRect(t, mid - 1, 5, 2, h - 10, new Color32(18, 16, 15, 220));
        DrawDiamond(t, mid, h - 4, 4, new Color32(224, 159, 62, 255), new Color32(20, 19, 18, 255));
        DrawDiamond(t, mid, 3, 4, new Color32(224, 159, 62, 255), new Color32(20, 19, 18, 255));

        t.Apply();
        return t;
    }

    private void FillRect(Texture2D t, int x0, int y0, int rw, int rh, Color32 col)
    {
        for (int y = y0; y < y0 + rh && y < t.height; y++)
            for (int x = x0; x < x0 + rw && x < t.width; x++)
                if (x >= 0 && y >= 0) t.SetPixel(x, y, col);
    }

    private void DrawDiamond(Texture2D t, int cx, int cy, int r, Color32 borderCol, Color32 innerCol)
    {
        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (dist <= r)
                {
                    int px = cx + dx, py = cy + dy;
                    if (px >= 0 && px < t.width && py >= 0 && py < t.height)
                        t.SetPixel(px, py, (dist >= r - 1) ? borderCol : innerCol);
                }
            }
        }
    }
}
#endif