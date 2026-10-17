#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class SettingsUIAssetGenerator : EditorWindow
{
    private const string BASE_PATH = "Assets/Sprites/SettingsUI";

    [MenuItem("Tools/ARPG UI/1. 生成設定頁面 PNG 資產包 (3種樣式)", false, 1)]
    public static void GenerateAllStyleAssets()
    {
        GenerateStyleAssets("Style1_DungeonStone", 1);
        GenerateStyleAssets("Style2_DarkFantasyGold", 2);
        GenerateStyleAssets("Style3_LowPolySlate", 3);

        AssetDatabase.Refresh();
        ConfigureSpritesAndBorders();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "UI 資產生成完成！",
            $"已成功在 {BASE_PATH} 下生成 3 套（共 24 張）透明 PNG 資產，並自動完成 Sprite 與 9-Slice 九宮格邊界設定！\n\n接著可使用「Tools -> ARPG UI -> 2. 建立/切換場景中的設定面板」直接套用。",
            "太棒了"
        );
    }

    [MenuItem("Tools/ARPG UI/2. 建立或切換設定面板 — 樣式一 (地牢石板風)", false, 20)]
    public static void BuildStyle1() => BuildOrSwapSceneUI("Style1_DungeonStone", new Color(1f, 0.55f, 0.15f), new Color(0.92f, 0.92f, 0.95f));

    [MenuItem("Tools/ARPG UI/3. 建立或切換設定面板 — 樣式二 (暗黑金銅風)", false, 21)]
    public static void BuildStyle2() => BuildOrSwapSceneUI("Style2_DarkFantasyGold", new Color(0.95f, 0.72f, 0.22f), new Color(1f, 0.93f, 0.78f));

    [MenuItem("Tools/ARPG UI/4. 建立或切換設定面板 — 樣式三 (Low-Poly 斜角風)", false, 22)]
    public static void BuildStyle3() => BuildOrSwapSceneUI("Style3_LowPolySlate", new Color(0.25f, 0.82f, 0.95f), new Color(0.9f, 0.96f, 1f));

    private static void GenerateStyleAssets(string folderName, int styleIndex)
    {
        string dir = Path.Combine(BASE_PATH, folderName);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // 設定三種風格的配色參數
        Color bgFill, borderOuter, borderInner, rivetColor;
        Color tabActiveFill, tabActiveBorder, tabInactiveFill;
        Color rowFill, rowBorder, closeFill, closeBorder, handleFill;
        bool chamfered = (styleIndex == 3);

        if (styleIndex == 1) // 樣式一：地牢石板與鍛鐵風
        {
            bgFill = new Color(0.14f, 0.16f, 0.19f, 0.94f);
            borderOuter = new Color(0.42f, 0.46f, 0.52f, 1f);
            borderInner = new Color(0.24f, 0.27f, 0.31f, 1f);
            rivetColor = new Color(0.62f, 0.66f, 0.72f, 1f);

            tabActiveFill = new Color(0.72f, 0.42f, 0.14f, 1f);
            tabActiveBorder = new Color(0.95f, 0.68f, 0.32f, 1f);
            tabInactiveFill = new Color(0.20f, 0.23f, 0.27f, 0.95f);

            rowFill = new Color(0.21f, 0.18f, 0.16f, 0.88f); // 呼應木寶箱暗褐色
            rowBorder = new Color(0.38f, 0.35f, 0.32f, 1f);

            closeFill = new Color(0.58f, 0.16f, 0.16f, 1f);
            closeBorder = new Color(0.65f, 0.68f, 0.72f, 1f);
            handleFill = new Color(0.78f, 0.82f, 0.86f, 1f);
        }
        else if (styleIndex == 2) // 樣式二：暗黑奇幻金銅風
        {
            bgFill = new Color(0.11f, 0.09f, 0.08f, 0.94f);
            borderOuter = new Color(0.72f, 0.58f, 0.34f, 1f);
            borderInner = new Color(0.42f, 0.32f, 0.18f, 1f);
            rivetColor = new Color(0.90f, 0.76f, 0.46f, 1f);

            tabActiveFill = new Color(0.78f, 0.56f, 0.20f, 1f);
            tabActiveBorder = new Color(0.98f, 0.85f, 0.52f, 1f);
            tabInactiveFill = new Color(0.18f, 0.15f, 0.12f, 0.95f);

            rowFill = new Color(0.17f, 0.14f, 0.12f, 0.88f);
            rowBorder = new Color(0.52f, 0.40f, 0.24f, 1f);

            closeFill = new Color(0.64f, 0.14f, 0.12f, 1f);
            closeBorder = new Color(0.82f, 0.66f, 0.38f, 1f);
            handleFill = new Color(0.95f, 0.75f, 0.30f, 1f);
        }
        else // 樣式三：Low-Poly 斜角幾何風
        {
            bgFill = new Color(0.10f, 0.13f, 0.18f, 0.92f);
            borderOuter = new Color(0.32f, 0.58f, 0.75f, 1f);
            borderInner = new Color(0.18f, 0.32f, 0.45f, 1f);
            rivetColor = new Color(0.45f, 0.82f, 0.98f, 1f);

            tabActiveFill = new Color(0.18f, 0.55f, 0.72f, 1f);
            tabActiveBorder = new Color(0.52f, 0.90f, 1.0f, 1f);
            tabInactiveFill = new Color(0.15f, 0.20f, 0.26f, 0.95f);

            rowFill = new Color(0.15f, 0.20f, 0.27f, 0.85f);
            rowBorder = new Color(0.25f, 0.42f, 0.56f, 1f);

            closeFill = new Color(0.68f, 0.18f, 0.24f, 1f);
            closeBorder = new Color(0.92f, 0.45f, 0.50f, 1f);
            handleFill = new Color(0.40f, 0.88f, 1.0f, 1f);
        }

        // 1. 主面板背景 (512x640)
        SaveTex(DrawBoxTexture(512, 640, 18f, 7f, bgFill, borderOuter, borderInner, true, rivetColor, chamfered), Path.Combine(dir, "panel_bg.png"));
        // 2. 分頁按鈕 - 選中與未選中 (180x56)
        SaveTex(DrawBoxTexture(180, 56, 10f, 4f, tabActiveFill, tabActiveBorder, borderInner, false, rivetColor, chamfered, true), Path.Combine(dir, "tab_active.png"));
        SaveTex(DrawBoxTexture(180, 56, 10f, 3f, tabInactiveFill, borderOuter, borderInner, false, rivetColor, chamfered, false), Path.Combine(dir, "tab_inactive.png"));
        // 3. 音量橫列底框 (480x72)
        SaveTex(DrawBoxTexture(480, 72, 10f, 3f, rowFill, rowBorder, Color.clear, false, rivetColor, chamfered, false), Path.Combine(dir, "row_bg.png"));
        // 4. Slider 軌道底槽 (256x28)
        SaveTex(DrawBoxTexture(256, 28, 8f, 3f, new Color(0.07f, 0.07f, 0.09f, 0.95f), borderOuter, Color.clear, false, rivetColor, chamfered, false), Path.Combine(dir, "slider_track.png"));
        // 5. Slider 白色高光填充條 (256x28, 供 Unity Image.color 動態染色)
        SaveTex(DrawBoxTexture(256, 28, 6f, 0f, Color.white, Color.white, Color.clear, false, Color.clear, chamfered, true), Path.Combine(dir, "slider_fill_white.png"));
        // 6. Slider 滑塊 Handle (48x48)
        SaveTex(DrawHandleTexture(48, handleFill, borderOuter, chamfered), Path.Combine(dir, "slider_handle.png"));
        // 7. 關閉按鈕 (200x64)
        SaveTex(DrawBoxTexture(200, 64, 12f, 4f, closeFill, closeBorder, borderInner, false, rivetColor, chamfered, true), Path.Combine(dir, "btn_close.png"));
    }

    private static Texture2D DrawBoxTexture(int w, int h, float radius, float borderThickness, Color fill, Color outerBorder, Color innerBorder, bool drawRivets, Color rivetCol, bool chamfer, bool glossyGradient = false)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);

        for (int y = 0; y < h; y++)
        {
            float vRatio = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = chamfer ? SdfChamferBox(p, half - new Vector2(3f, 3f), radius) : SdfRoundBox(p, half - new Vector2(3f, 3f), radius);

                if (dist > 1.0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float outerAlpha = Mathf.Clamp01(0.5f - dist);
                Color c = fill;

                // 垂直立體漸層與微弱石紋雜訊
                float grad = glossyGradient ? Mathf.Lerp(0.75f, 1.18f, vRatio) : Mathf.Lerp(0.88f, 1.08f, vRatio);
                float noise = (Mathf.PerlinNoise(x * 0.08f, y * 0.08f) - 0.5f) * 0.04f;
                c.r = Mathf.Clamp01(c.r * grad + noise);
                c.g = Mathf.Clamp01(c.g * grad + noise);
                c.b = Mathf.Clamp01(c.b * grad + noise);

                // 內框線與外框線
                if (dist > -borderThickness)
                {
                    float bevel = Mathf.Lerp(0.8f, 1.25f, vRatio);
                    c = new Color(outerBorder.r * bevel, outerBorder.g * bevel, outerBorder.b * bevel, 1f);
                }
                else if (innerBorder.a > 0.01f && dist > -(borderThickness + 6f) && dist < -(borderThickness + 3f))
                {
                    c = innerBorder;
                }

                // 四角鉚釘
                if (drawRivets)
                {
                    Vector2[] rivets = {
                        new Vector2(20f, 20f), new Vector2(w - 20f, 20f),
                        new Vector2(20f, h - 20f), new Vector2(w - 20f, h - 20f)
                    };
                    foreach (var rv in rivets)
                    {
                        float rd = Vector2.Distance(new Vector2(x, y), rv);
                        if (rd < 5.5f) c = rivetCol;
                        else if (rd < 7.0f) c = new Color(0.08f, 0.08f, 0.08f, 1f);
                    }
                }

                c.a *= outerAlpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D DrawHandleTexture(int size, Color fill, Color border, bool diamond)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            float vRatio = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = diamond ? (Mathf.Abs(p.x) + Mathf.Abs(p.y)) * 0.7071f - maxR * 0.85f : p.magnitude - maxR;

                if (d > 1f) { tex.SetPixel(x, y, Color.clear); continue; }

                float alpha = Mathf.Clamp01(0.5f - d);
                Color c = (d > -4f) ? border : fill * Mathf.Lerp(0.8f, 1.25f, vRatio);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static float SdfRoundBox(Vector2 p, Vector2 b, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b + new Vector2(r, r);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static float SdfChamferBox(Vector2 p, Vector2 b, float chamfer)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
        float d1 = Mathf.Max(q.x, q.y);
        float d2 = (q.x + q.y + chamfer) * 0.70710678f;
        return Mathf.Max(d1, d2);
    }

    private static void SaveTex(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);
    }

    private static void ConfigureSpritesAndBorders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { BASE_PATH });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;

            if (path.Contains("panel_bg")) ti.spriteBorder = new Vector4(30, 30, 30, 30);
            else if (path.Contains("tab_") || path.Contains("row_bg") || path.Contains("btn_close")) ti.spriteBorder = new Vector4(16, 16, 16, 16);
            else if (path.Contains("slider_track") || path.Contains("slider_fill")) ti.spriteBorder = new Vector4(10, 10, 10, 10);

            ti.SaveAndReimport();
        }
    }

    private static void BuildOrSwapSceneUI(string styleFolder, Color sliderTint, Color textTint)
    {
        string dir = Path.Combine(BASE_PATH, styleFolder);
        if (!Directory.Exists(dir))
        {
            GenerateAllStyleAssets();
        }

        Sprite spPanel = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "panel_bg.png"));
        Sprite spTabAct = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "tab_active.png"));
        Sprite spTabInact = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "tab_inactive.png"));
        Sprite spRow = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "row_bg.png"));
        Sprite spTrack = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "slider_track.png"));
        Sprite spFill = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "slider_fill_white.png"));
        Sprite spHandle = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "slider_handle.png"));
        Sprite spClose = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(dir, "btn_close.png"));

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        Transform existing = canvas.transform.Find("ARPG_SettingsPanel");
        if (existing != null) DestroyImmediate(existing.gameObject);

        // 建立主面板
        GameObject panel = CreateUIImage("ARPG_SettingsPanel", canvas.transform, spPanel, new Vector2(540, 620), Vector2.zero);

        // 標題：遊戲設定
        CreateTMP("Title_Text", panel.transform, "遊戲設定", 34, textTint, new Vector2(0, 255), new Vector2(300, 50));

        // 三個分頁按鈕：音效、影像、偏好
        string[] tabs = { "音效", "影像", "偏好" };
        for (int i = 0; i < tabs.Length; i++)
        {
            float xPos = (i - 1) * 145f;
            Sprite tabSp = (i == 0) ? spTabAct : spTabInact;
            GameObject tabBtn = CreateUIImage($"Tab_{tabs[i]}", panel.transform, tabSp, new Vector2(130, 44), new Vector2(xPos, 185));
            tabBtn.AddComponent<Button>();
            CreateTMP("Label", tabBtn.transform, tabs[i], 20, (i == 0) ? Color.white : new Color(0.75f, 0.75f, 0.78f), Vector2.zero, new Vector2(120, 40));
        }

        // 兩個音量設定列：音樂音量、音效音量
        CreateSliderRow("Row_MusicVolume", "音樂音量", panel.transform, spRow, spTrack, spFill, spHandle, sliderTint, textTint, new Vector2(0, 95), 0.75f);
        CreateSliderRow("Row_SFXVolume", "音效音量", panel.transform, spRow, spTrack, spFill, spHandle, sliderTint, textTint, new Vector2(0, 10), 0.80f);

        // 底部關閉按鈕
        GameObject closeBtn = CreateUIImage("Btn_Close", panel.transform, spClose, new Vector2(160, 52), new Vector2(0, -240));
        closeBtn.AddComponent<Button>();
        CreateTMP("Label", closeBtn.transform, "關閉", 24, Color.white, Vector2.zero, new Vector2(150, 48));

        Selection.activeGameObject = panel;
    }

    private static GameObject CreateUIImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = (sprite != null && sprite.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent, string text, float fontSize, Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private static void CreateSliderRow(string name, string label, Transform parent, Sprite spRow, Sprite spTrack, Sprite spFill, Sprite spHandle, Color fillTint, Color textTint, Vector2 pos, float defaultVal)
    {
        GameObject row = CreateUIImage(name, parent, spRow, new Vector2(460, 64), pos);
        var labelTmp = CreateTMP("Label", row.transform, label, 22, textTint, new Vector2(-130, 0), new Vector2(160, 50));
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // 建立標準 Unity Slider 結構
        GameObject sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(row.transform, false);
        RectTransform sRT = sliderGO.GetComponent<RectTransform>();
        sRT.anchoredPosition = new Vector2(85, 0);
        sRT.sizeDelta = new Vector2(210, 26);

        GameObject bg = CreateUIImage("Background", sliderGO.transform, spTrack, Vector2.zero, Vector2.zero);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.2f);
        bgRT.anchorMax = new Vector2(1, 0.8f);
        bgRT.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.2f);
        faRT.anchorMax = new Vector2(1, 0.8f);
        faRT.sizeDelta = new Vector2(-12, -4);

        GameObject fill = CreateUIImage("Fill", fillArea.transform, spFill, Vector2.zero, Vector2.zero);
        fill.GetComponent<Image>().color = fillTint;
        RectTransform fRT = fill.GetComponent<RectTransform>();
        fRT.sizeDelta = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGO.transform, false);
        RectTransform haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.sizeDelta = new Vector2(-16, 0);

        GameObject handle = CreateUIImage("Handle", handleArea.transform, spHandle, new Vector2(30, 30), Vector2.zero);

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fRT;
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.value = defaultVal;
    }
}
#endif