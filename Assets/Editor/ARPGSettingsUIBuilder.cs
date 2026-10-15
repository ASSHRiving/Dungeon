#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class ARPGSettingsUIBuilder : EditorWindow
{
    private const string ASSET_DIR = "Assets/Sprites/SettingsUI";

    [MenuItem("Tools/ARPG 設定介面/1. 導出設定介面 PNG 資產包 (9張透明PNG)", false, 1)]
    public static void ExportSettingsPNGAssets()
    {
        if (!Directory.Exists(ASSET_DIR))
            Directory.CreateDirectory(ASSET_DIR);

        // 顏色定義：結合地牢藍灰石板 + 角色選擇畫面的暗黑金銅邊框
        Color panelTop = new Color(0.14f, 0.16f, 0.20f, 0.95f);
        Color panelBot = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        Color goldBorder = new Color(0.74f, 0.60f, 0.36f, 1f);
        Color innerLine = new Color(0.42f, 0.34f, 0.22f, 0.85f);
        Color rivetGold = new Color(0.92f, 0.78f, 0.48f, 1f);

        // 1. 主面板底框 (560x480, 支援 9-Slice)
        SavePNG(CreatePanelTex(560, 480, 16f, 6f, panelTop, panelBot, goldBorder, innerLine, true, rivetGold), "settings_panel_bg.png");

        // 2. 分頁按鈕 - 選中 (琥珀金銅發光)
        SavePNG(CreatePanelTex(160, 48, 10f, 4f, new Color(0.82f, 0.48f, 0.12f, 1f), new Color(0.48f, 0.24f, 0.06f, 1f), new Color(0.98f, 0.86f, 0.48f, 1f), Color.clear, false, Color.clear), "tab_active.png");

        // 3. 分頁按鈕 - 未選中 (地牢暗色石板)
        SavePNG(CreatePanelTex(160, 48, 10f, 3f, new Color(0.20f, 0.23f, 0.28f, 0.95f), new Color(0.12f, 0.14f, 0.18f, 0.95f), new Color(0.42f, 0.46f, 0.53f, 1f), Color.clear, false, Color.clear), "tab_inactive.png");

        // 4. 設定項目橫條底框 (呼應地牢木寶箱與鐵邊)
        SavePNG(CreatePanelTex(480, 64, 10f, 3f, new Color(0.20f, 0.17f, 0.15f, 0.90f), new Color(0.13f, 0.11f, 0.10f, 0.92f), new Color(0.50f, 0.40f, 0.28f, 1f), Color.clear, false, Color.clear), "setting_row_bg.png");

        // 5. Slider 軌道深色凹槽
        SavePNG(CreatePanelTex(240, 26, 8f, 3f, new Color(0.06f, 0.07f, 0.09f, 0.96f), new Color(0.11f, 0.12f, 0.15f, 0.96f), new Color(0.48f, 0.39f, 0.26f, 1f), Color.clear, false, Color.clear), "slider_track_bg.png");

        // 6. Slider 純白高光填充條 (供 Unity Image.color 染色)
        SavePNG(CreatePanelTex(240, 22, 6f, 0f, Color.white, new Color(0.78f, 0.82f, 0.88f, 1f), Color.white, Color.clear, false, Color.clear), "slider_fill_white.png");

        // 7. Slider 寶石金屬滑塊 Handle
        SavePNG(CreateKnobTex(48, new Color(0.98f, 0.72f, 0.20f, 1f), new Color(0.98f, 0.90f, 0.58f, 1f)), "slider_handle_knob.png");

        // 8. 底部關閉按鈕 (暗緋紅寶石 + 金銅邊)
        SavePNG(CreatePanelTex(180, 54, 12f, 4f, new Color(0.72f, 0.14f, 0.14f, 1f), new Color(0.38f, 0.06f, 0.06f, 1f), goldBorder, new Color(0.95f, 0.45f, 0.45f, 0.4f), false, Color.clear), "btn_close_ruby.png");

        // 9. 標題下方金色漸層分隔線
        SavePNG(CreateDividerTex(360, 10, rivetGold), "header_divider_gold.png");

        AssetDatabase.Refresh();
        ConfigureImportedSprites();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("導出成功！", $"已在 {ASSET_DIR} 導出 9 張透明 PNG 資產，並完成 9-Slice 設定！\n\n現在可點擊選單「2. 在場景中一鍵生成美化版設定面板」直接建立 UI。", "確定");
    }

    [MenuItem("Tools/ARPG 設定介面/2. 在場景中一鍵生成美化版設定面板", false, 2)]
    public static void BuildRedesignedSettingsUI()
    {
        if (!File.Exists(Path.Combine(ASSET_DIR, "settings_panel_bg.png")))
            ExportSettingsPNGAssets();

        Sprite spPanel = LoadSp("settings_panel_bg.png");
        Sprite spDivider = LoadSp("header_divider_gold.png");
        Sprite spTabAct = LoadSp("tab_active.png");
        Sprite spTabInact = LoadSp("tab_inactive.png");
        Sprite spRow = LoadSp("setting_row_bg.png");
        Sprite spTrack = LoadSp("slider_track_bg.png");
        Sprite spFill = LoadSp("slider_fill_white.png");
        Sprite spKnob = LoadSp("slider_handle_knob.png");
        Sprite spClose = LoadSp("btn_close_ruby.png");

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject cGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        Transform old = canvas.transform.Find("Panel_Settings_Redesigned");
        if (old != null) DestroyImmediate(old.gameObject);

        // 1. 主面板 (緊湊黃金比例 560x470，解決原本下半部空洞問題)
        GameObject panel = CreateImg("Panel_Settings_Redesigned", canvas.transform, spPanel, new Vector2(560, 470), Vector2.zero);

        // 2. 頂部標題與金色分隔線
        CreateTMP("Title_Text", panel.transform, "遊戲設定", 32, new Color(0.99f, 0.93f, 0.65f), new Vector2(0, 192), new Vector2(300, 46));
        CreateImg("Header_Divider", panel.transform, spDivider, new Vector2(380, 10), new Vector2(0, 162));

        // 3. 分頁切換列 (音效 / 影像 / 偏好)
        string[] tabNames = { "音 效", "影 像", "偏 好" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            float x = (i - 1) * 150f;
            bool isFirst = (i == 0);
            GameObject tab = CreateImg($"Btn_Tab_{i}", panel.transform, isFirst ? spTabAct : spTabInact, new Vector2(136, 44), new Vector2(x, 122));
            tab.AddComponent<Button>();
            CreateTMP("Label", tab.transform, tabNames[i], 20, isFirst ? Color.white : new Color(0.68f, 0.72f, 0.78f), Vector2.zero, new Vector2(130, 40));
        }

        // 4. 三條緊湊且資訊完整的音量設定列 (主音量 / 音樂音量 / 音效音量)
        CreateVolumeRow("Row_MasterVolume", "主 音 量", panel.transform, spRow, spTrack, spFill, spKnob, new Vector2(0, 48), 1.00f);
        CreateVolumeRow("Row_MusicVolume",  "音樂音量", panel.transform, spRow, spTrack, spFill, spKnob, new Vector2(0, -24), 0.75f);
        CreateVolumeRow("Row_SFXVolume",    "音效音量", panel.transform, spRow, spTrack, spFill, spKnob, new Vector2(0, -96), 0.85f);

        // 5. 底部按鈕區 (恢復預設 + 關閉)
        GameObject resetBtn = CreateImg("Btn_Reset", panel.transform, spTabInact, new Vector2(150, 48), new Vector2(-95, -182));
        resetBtn.AddComponent<Button>();
        CreateTMP("Label", resetBtn.transform, "恢復預設", 20, new Color(0.85f, 0.88f, 0.92f), Vector2.zero, new Vector2(140, 44));

        GameObject closeBtn = CreateImg("Btn_Close", panel.transform, spClose, new Vector2(150, 48), new Vector2(95, -182));
        closeBtn.AddComponent<Button>();
        CreateTMP("Label", closeBtn.transform, "關 閉", 22, Color.white, Vector2.zero, new Vector2(140, 44));

        Selection.activeGameObject = panel;
    }

    private static void CreateVolumeRow(string objName, string title, Transform parent, Sprite spRow, Sprite spTrack, Sprite spFill, Sprite spKnob, Vector2 pos, float defaultVal)
    {
        GameObject row = CreateImg(objName, parent, spRow, new Vector2(490, 58), pos);

        // 左側標題
        var lbl = CreateTMP("Label", row.transform, title, 21, new Color(0.95f, 0.95f, 0.98f), new Vector2(-155, 0), new Vector2(130, 48));
        lbl.alignment = TextAlignmentOptions.MidlineLeft;

        // 中間加厚版奇幻滑桿 Slider
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform sRT = sliderObj.GetComponent<RectTransform>();
        sRT.anchoredPosition = new Vector2(30, 0);
        sRT.sizeDelta = new Vector2(220, 24);

        GameObject bg = CreateImg("Background", sliderObj.transform, spTrack, Vector2.zero, Vector2.zero);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.15f); bgRT.anchorMax = new Vector2(1, 0.85f); bgRT.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.15f); faRT.anchorMax = new Vector2(1, 0.85f); faRT.sizeDelta = new Vector2(-10, -4);

        GameObject fill = CreateImg("Fill", fillArea.transform, spFill, Vector2.zero, Vector2.zero);
        fill.GetComponent<Image>().color = new Color(0.96f, 0.62f, 0.14f, 1f); // 琥珀橘金進度條
        fill.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one; haRT.sizeDelta = new Vector2(-14, 0);

        GameObject handle = CreateImg("Handle", handleArea.transform, spKnob, new Vector2(28, 28), Vector2.zero);

        Slider sl = sliderObj.GetComponent<Slider>();
        sl.fillRect = fill.GetComponent<RectTransform>();
        sl.handleRect = handle.GetComponent<RectTransform>();
        sl.targetGraphic = handle.GetComponent<Image>();
        sl.value = defaultVal;

        // 右側百分比數值顯示
        var valTxt = CreateTMP("Value_Text", row.transform, Mathf.RoundToInt(defaultVal * 100) + "%", 20, new Color(0.98f, 0.78f, 0.30f), new Vector2(195, 0), new Vector2(65, 48));
        valTxt.alignment = TextAlignmentOptions.MidlineRight;
    }

    private static Texture2D CreatePanelTex(int w, int h, float r, float borderW, Color topCol, Color botCol, Color borderCol, Color innerCol, bool rivets, Color rivetCol)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - new Vector2(3f, 3f)) + new Vector2(r, r);
                float dist = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;

                if (dist > 1f) { tex.SetPixel(x, y, Color.clear); continue; }
                float alpha = Mathf.Clamp01(0.5f - dist);
                Color c = Color.Lerp(botCol, topCol, t);

                if (borderW > 0f && dist > -borderW)
                {
                    float bevel = Mathf.Lerp(0.75f, 1.25f, t);
                    c = new Color(borderCol.r * bevel, borderCol.g * bevel, borderCol.b * bevel, 1f);
                }
                else if (innerCol.a > 0.01f && dist > -(borderW + 6f) && dist < -(borderW + 4f))
                {
                    c = innerCol;
                }

                if (rivets)
                {
                    Vector2[] pts = { new Vector2(20, 20), new Vector2(w - 20, 20), new Vector2(20, h - 20), new Vector2(w - 20, h - 20) };
                    foreach (var pt in pts)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), pt);
                        if (d < 5f) c = rivetCol;
                        else if (d < 6.5f) c = new Color(0.08f, 0.08f, 0.08f, 1f);
                    }
                }
                c.a *= alpha;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateKnobTex(int size, Color fill, Color border)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxR = size * 0.42f;
        for (int y = 0; y < size; y++)
        {
            float t = (float)y / size;
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) - maxR;
                if (d > 1f) { tex.SetPixel(x, y, Color.clear); continue; }
                Color c = (d > -3.5f) ? border : fill * Mathf.Lerp(0.75f, 1.25f, t);
                c.a = Mathf.Clamp01(0.5f - d);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateDividerTex(int w, int h, Color gold)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = Mathf.Abs(x - w * 0.5f) / (w * 0.5f);
                float dy = Mathf.Abs(y - h * 0.5f);
                float alpha = (dy <= 1.5f) ? Mathf.Clamp01(1f - dx * dx) : 0f;
                tex.SetPixel(x, y, new Color(gold.r, gold.g, gold.b, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private static void SavePNG(Texture2D tex, string filename)
    {
        File.WriteAllBytes(Path.Combine(ASSET_DIR, filename), tex.EncodeToPNG());
        DestroyImmediate(tex);
    }

    private static void ConfigureImportedSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ASSET_DIR });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            if (path.Contains("settings_panel_bg")) ti.spriteBorder = new Vector4(28, 28, 28, 28);
            else if (path.Contains("tab_") || path.Contains("setting_row_bg") || path.Contains("btn_close")) ti.spriteBorder = new Vector4(14, 14, 14, 14);
            else if (path.Contains("slider_")) ti.spriteBorder = new Vector4(8, 8, 8, 8);
            ti.SaveAndReimport();
        }
    }

    private static Sprite LoadSp(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(ASSET_DIR, name));

    private static GameObject CreateImg(string name, Transform parent, Sprite sp, Vector2 size, Vector2 pos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.sprite = sp;
        img.type = (sp != null && sp.border != Vector4.zero) ? Image.Type.Sliced : Image.Type.Simple;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent, string txt, float size, Color col, Vector2 pos, Vector2 rectSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = rectSize;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = txt; tmp.fontSize = size; tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }
}
#endif