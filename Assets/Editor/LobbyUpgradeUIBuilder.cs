#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class LobbyUpgradeUIBuilder
{
    private const string FONT_PATH = "Assets/TextMesh Pro/Resources/Fonts & Materials/SourceHanSerifTW-SemiBold SDF.asset";
    private const string SPRITE_PANEL_BG = "Assets/SpritesUI/Lobby/lobby_side_panel_bg.png";
    private const string SPRITE_TITLE_BANNER = "Assets/SpritesUI/Lobby/lobby_title_banner.png";
    private const string UPGRADE_CONFIG_PATH = "Assets/Resources/CharacterUpgradeConfig.asset";
    private const string CUSTOM_ASSET_DIR = "Assets/SpritesUI/LobbyUpgrade";

    [MenuItem("Tools/Build Lobby Upgrade UI")]
    [MenuItem("Tools/⚔️ 一鍵搭建選角大廳升級介面 (Lobby Upgrade UI)")]
    public static void BuildLobbyUpgradeUI()
    {
        if (!Directory.Exists(CUSTOM_ASSET_DIR))
        {
            Directory.CreateDirectory(CUSTOM_ASSET_DIR);
        }

        const string LOBBY_SCENE_PATH = "Assets/Scenes/LobbyScene.unity";
        if (EditorSceneManager.GetActiveScene().path != LOBBY_SCENE_PATH)
        {
            EditorSceneManager.OpenScene(LOBBY_SCENE_PATH, OpenSceneMode.Single);
        }

        // 1. 載入與程序化生成材質
        Sprite soulBarSprite = CreateAndLoadSprite("upgrade_soul_bar.png", GenerateSoulBarTex(450, 52));
        Sprite soulIconSprite = CreateAndLoadSprite("upgrade_icon_soul.png", GenerateSoulIconTex(64, 64));
        Sprite levelCardSprite = CreateAndLoadSprite("upgrade_level_card.png", GenerateFrameCardTex(450, 96, 0.92f));
        Sprite statCardSprite = CreateAndLoadSprite("upgrade_stat_summary_card.png", GenerateFrameCardTex(450, 195, 0.90f));
        Sprite progTrackSprite = CreateAndLoadSprite("upgrade_prog_track.png", GenerateTrackTex(400, 12));
        Sprite progFillSprite = CreateAndLoadSprite("upgrade_prog_fill.png", GenerateFillTex(400, 12));
        Sprite btnActionSprite = CreateAndLoadSprite("upgrade_btn_action.png", GenerateUpgradeBtnTex(280, 78));

        Sprite iconHpSprite = CreateAndLoadSprite("upgrade_icon_hp.png", GenerateStatEmblemTex(72, 72, UpgradeType.Health));
        Sprite iconDefSprite = CreateAndLoadSprite("upgrade_icon_def.png", GenerateStatEmblemTex(72, 72, UpgradeType.Defense));
        Sprite iconAtkSprite = CreateAndLoadSprite("upgrade_icon_atk.png", GenerateStatEmblemTex(72, 72, UpgradeType.Attack));

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.Contains("LiberationSans"))
                {
                    font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
                    break;
                }
            }
        }

        Sprite panelBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PANEL_BG);
        Sprite titleBannerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_TITLE_BANNER);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[LobbyUpgradeUIBuilder] 找不到當前場景中的 Canvas！");
            return;
        }

        // 2. 確保場景中有 MetaProgressionManager 與 CharacterUpgradeManager
        EnsureMetaManagersInScene();

        // 3. 移除舊版 CharacterUpgradePanel
        Transform oldPanel = canvas.transform.Find("CharacterUpgradePanel");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        // 4. 建立 CharacterUpgradePanel 根物件 (維持 500x650，與 LeftPanel 完全對稱)
        GameObject panelGO = new GameObject("CharacterUpgradePanel", typeof(RectTransform), typeof(CanvasGroup), typeof(CharacterUpgradeUIController));
        panelGO.layer = LayerMask.NameToLayer("UI");
        panelGO.transform.SetParent(canvas.transform, false);

        Transform leftPanelTrans = canvas.transform.Find("LeftPanel");
        if (leftPanelTrans != null)
        {
            panelGO.transform.SetSiblingIndex(leftPanelTrans.GetSiblingIndex() + 1);
        }

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = new Vector2(600f, -50f);
        panelRT.sizeDelta = new Vector2(500f, 650f);

        CanvasGroup panelCG = panelGO.GetComponent<CanvasGroup>();
        panelCG.alpha = 1f;
        panelCG.interactable = true;
        panelCG.blocksRaycasts = true;

        CharacterUpgradeUIController upgradeCtrl = panelGO.GetComponent<CharacterUpgradeUIController>();

        // 5. Panel_Background (與 LeftPanel 完全相同外框材質，保持對稱美感)
        GameObject bgGO = CreateImage("Panel_Background", panelGO.transform, panelBgSprite, Image.Type.Sliced, Vector2.zero, Vector2.zero, Color.white);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // 6. Header_Banner 與 Header_Title ("角色養成")
        GameObject headerBannerGO = CreateImage("Header_Banner", panelGO.transform, titleBannerSprite, Image.Type.Simple, new Vector2(0f, 270f), new Vector2(320f, 75f), Color.white);
        headerBannerGO.GetComponent<Image>().raycastTarget = false;

        CreateText(
            "Header_Title",
            panelGO.transform,
            font,
            "角色養成",
            36f,
            FontStyles.Normal,
            Color.white,
            new Vector2(0f, 272f),
            new Vector2(260f, 54f),
            TextAlignmentOptions.Center,
            12f
        );

        // 7. 持有靈魂顯示列 (Currency_Bg + Soul_Icon + Currency_Label + CurrencyDisplay)
        GameObject currencyBgGO = CreateImage("Currency_Bg", panelGO.transform, soulBarSprite, Image.Type.Simple, new Vector2(0f, 198f), new Vector2(450f, 52f), Color.white);
        currencyBgGO.GetComponent<Image>().raycastTarget = false;

        GameObject soulIconGO = CreateImage("Soul_Icon", currencyBgGO.transform, soulIconSprite, Image.Type.Simple, new Vector2(-192f, 0f), new Vector2(32f, 32f), Color.white);
        soulIconGO.GetComponent<Image>().raycastTarget = false;

        CreateText(
            "Currency_Label",
            currencyBgGO.transform,
            font,
            "持有靈魂",
            22f,
            FontStyles.Normal,
            new Color32(220, 214, 206, 255),
            new Vector2(-88f, 0f),
            new Vector2(160f, 40f),
            TextAlignmentOptions.MidlineLeft,
            6f
        );

        TMP_Text currencyValueText = CreateText(
            "CurrencyDisplay",
            panelGO.transform,
            font,
            "0 靈魂",
            25f,
            FontStyles.Bold,
            new Color32(228, 198, 255, 255),
            new Vector2(88f, 198f),
            new Vector2(220f, 44f),
            TextAlignmentOptions.MidlineRight,
            4f
        );

        // 8. 角色階級與視覺進度條卡片 (Level Card: 450 x 96)
        GameObject levelCardGO = CreateImage("Card_Level", panelGO.transform, levelCardSprite, Image.Type.Simple, new Vector2(0f, 105f), new Vector2(450f, 96f), Color.white);
        levelCardGO.GetComponent<Image>().raycastTarget = false;

        CreateText(
            "Level_Title",
            levelCardGO.transform,
            font,
            "當前強化階級",
            21f,
            FontStyles.Bold,
            new Color32(235, 228, 218, 255),
            new Vector2(-95f, 16f),
            new Vector2(210f, 36f),
            TextAlignmentOptions.MidlineLeft,
            4f
        );

        TMP_Text levelValueText = CreateText(
            "Level_Value",
            levelCardGO.transform,
            font,
            "Lv.0 / 10",
            24f,
            FontStyles.Bold,
            new Color32(236, 198, 100, 255),
            new Vector2(105f, 16f),
            new Vector2(180f, 36f),
            TextAlignmentOptions.MidlineRight,
            2f
        );

        // 進度條底槽 (400 x 12)
        GameObject trackGO = CreateImage("Level_Prog_Track", levelCardGO.transform, progTrackSprite, Image.Type.Simple, new Vector2(0f, -18f), new Vector2(400f, 12f), Color.white);
        trackGO.GetComponent<Image>().raycastTarget = false;

        // 進度條填充 (400 x 12, Filled)
        GameObject fillGO = CreateImage("Level_Prog_Fill", trackGO.transform, progFillSprite, Image.Type.Filled, Vector2.zero, new Vector2(400f, 12f), Color.white);
        Image fillImg = fillGO.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 0f;
        fillImg.raycastTarget = false;

        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // 9. 三圍同步加成清單卡片 (Stat Summary Card: 450 x 195)
        GameObject statCardGO = CreateImage("Card_StatSummary", panelGO.transform, statCardSprite, Image.Type.Simple, new Vector2(0f, -50f), new Vector2(450f, 195f), Color.white);
        statCardGO.GetComponent<Image>().raycastTarget = false;

        CreateText(
            "Summary_Hint",
            statCardGO.transform,
            font,
            "— 每次升級三圍屬性同步提升 —",
            14f,
            FontStyles.Normal,
            new Color32(165, 155, 142, 255),
            new Vector2(0f, 72f),
            new Vector2(400f, 26f),
            TextAlignmentOptions.Center,
            2f
        );

        // 生命列
        GameObject hpIconGO = CreateImage("Icon_Hp", statCardGO.transform, iconHpSprite, Image.Type.Simple, new Vector2(-175f, 28f), new Vector2(36f, 36f), Color.white);
        hpIconGO.GetComponent<Image>().raycastTarget = false;
        CreateText("Label_Hp", statCardGO.transform, font, "生命上限", 20f, FontStyles.Bold, new Color32(238, 232, 222, 255), new Vector2(-80f, 28f), new Vector2(130f, 32f), TextAlignmentOptions.MidlineLeft, 3f);
        TMP_Text hpBonusText = CreateText("Bonus_Hp", statCardGO.transform, font, "+0 HP  →  <color=#80FF80>+15</color>", 20f, FontStyles.Bold, new Color32(242, 206, 105, 255), new Vector2(75f, 28f), new Vector2(200f, 32f), TextAlignmentOptions.MidlineRight, 2f);

        // 防禦列
        GameObject defIconGO = CreateImage("Icon_Def", statCardGO.transform, iconDefSprite, Image.Type.Simple, new Vector2(-175f, -18f), new Vector2(36f, 36f), Color.white);
        defIconGO.GetComponent<Image>().raycastTarget = false;
        CreateText("Label_Def", statCardGO.transform, font, "防禦護甲", 20f, FontStyles.Bold, new Color32(238, 232, 222, 255), new Vector2(-80f, -18f), new Vector2(130f, 32f), TextAlignmentOptions.MidlineLeft, 3f);
        TMP_Text defBonusText = CreateText("Bonus_Def", statCardGO.transform, font, "+0 護甲  →  <color=#80FF80>+3</color>", 20f, FontStyles.Bold, new Color32(242, 206, 105, 255), new Vector2(75f, -18f), new Vector2(200f, 32f), TextAlignmentOptions.MidlineRight, 2f);

        // 攻擊列
        GameObject atkIconGO = CreateImage("Icon_Atk", statCardGO.transform, iconAtkSprite, Image.Type.Simple, new Vector2(-175f, -64f), new Vector2(36f, 36f), Color.white);
        atkIconGO.GetComponent<Image>().raycastTarget = false;
        CreateText("Label_Atk", statCardGO.transform, font, "基礎攻擊", 20f, FontStyles.Bold, new Color32(238, 232, 222, 255), new Vector2(-80f, -64f), new Vector2(130f, 32f), TextAlignmentOptions.MidlineLeft, 3f);
        TMP_Text atkBonusText = CreateText("Bonus_Atk", statCardGO.transform, font, "+0 攻擊  →  <color=#80FF80>+4</color>", 20f, FontStyles.Bold, new Color32(242, 206, 105, 255), new Vector2(75f, -64f), new Vector2(200f, 32f), TextAlignmentOptions.MidlineRight, 2f);

        // 10. 一體化升級大按鈕 (280 x 78)
        GameObject btnGO = CreateImage("Btn_UnifiedUpgrade", panelGO.transform, btnActionSprite, Image.Type.Simple, new Vector2(0f, -215f), new Vector2(280f, 78f), Color.white);
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnGO.GetComponent<Image>();

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        btn.colors = cb;

        CanvasGroup btnCG = btnGO.AddComponent<CanvasGroup>();
        btnGO.AddComponent<UIButtonTween>();

        TMP_Text btnLabelText = CreateText(
            "BtnLabel",
            btnGO.transform,
            font,
            "強 化",
            25f,
            FontStyles.Bold,
            new Color32(255, 248, 232, 255),
            new Vector2(0f, 15f),
            new Vector2(240f, 34f),
            TextAlignmentOptions.Center,
            8f
        );

        TMP_Text btnCostText = CreateText(
            "CostText",
            btnGO.transform,
            font,
            "50 靈魂",
            19f,
            FontStyles.Bold,
            new Color32(255, 226, 138, 255),
            new Vector2(0f, -16f),
            new Vector2(240f, 28f),
            TextAlignmentOptions.Center,
            2f
        );

        // 底部說明小字
        CreateText(
            "Footer_Hint",
            panelGO.transform,
            font,
            "強化數值將永久注入該英雄所有戰鬥",
            13f,
            FontStyles.Normal,
            new Color32(138, 130, 118, 255),
            new Vector2(0f, -272f),
            new Vector2(400f, 24f),
            TextAlignmentOptions.Center,
            2f
        );

        // 11. 綁定按鈕 Persistent OnClick 事件
        UnityEventTools.AddPersistentListener(btn.onClick, upgradeCtrl.OnClickUpgrade);

        // 12. 使用 SerializedObject 綁定 CharacterUpgradeUIController 所有欄位
        SerializedObject soCtrl = new SerializedObject(upgradeCtrl);
        soCtrl.FindProperty("currentSoulsText").objectReferenceValue = currencyValueText;
        soCtrl.FindProperty("levelValueText").objectReferenceValue = levelValueText;
        soCtrl.FindProperty("levelProgressBar").objectReferenceValue = fillImg;
        soCtrl.FindProperty("hpBonusText").objectReferenceValue = hpBonusText;
        soCtrl.FindProperty("defBonusText").objectReferenceValue = defBonusText;
        soCtrl.FindProperty("atkBonusText").objectReferenceValue = atkBonusText;
        soCtrl.FindProperty("upgradeButton").objectReferenceValue = btn;
        soCtrl.FindProperty("buttonLabelText").objectReferenceValue = btnLabelText;
        soCtrl.FindProperty("buttonCostText").objectReferenceValue = btnCostText;
        soCtrl.FindProperty("buttonCanvasGroup").objectReferenceValue = btnCG;
        soCtrl.ApplyModifiedProperties();
        EditorUtility.SetDirty(upgradeCtrl);

        // 13. 綁定至 LobbyManager.upgradeUIController
        LobbyManager lobbyMgr = Object.FindFirstObjectByType<LobbyManager>();
        if (lobbyMgr != null)
        {
            SerializedObject soLobby = new SerializedObject(lobbyMgr);
            soLobby.FindProperty("upgradeUIController").objectReferenceValue = upgradeCtrl;
            soLobby.FindProperty("maxHP").floatValue = 350f;
            soLobby.FindProperty("maxDEF").floatValue = 100f;
            soLobby.FindProperty("maxATK").floatValue = 80f;
            soLobby.ApplyModifiedProperties();
            EditorUtility.SetDirty(lobbyMgr);
        }

        Undo.RegisterCreatedObjectUndo(panelGO, "Build CharacterUpgradePanel");
        EditorSceneManager.MarkSceneDirty(panelGO.scene);
        EditorSceneManager.SaveScene(panelGO.scene);

        Selection.activeGameObject = panelGO;
        Debug.Log("✅ [LobbyUpgradeUIBuilder] 500x650 全新對稱三圍一體化升級面板建置完成！");
    }

    private static void EnsureMetaManagersInScene()
    {
        MetaProgressionManager metaMgr = Object.FindFirstObjectByType<MetaProgressionManager>();
        if (metaMgr == null)
        {
            GameObject go = new GameObject("MetaProgressionManager", typeof(MetaProgressionManager));
            metaMgr = go.GetComponent<MetaProgressionManager>();
            EditorUtility.SetDirty(go);
        }

        CharacterUpgradeManager upgradeMgr = Object.FindFirstObjectByType<CharacterUpgradeManager>();
        if (upgradeMgr == null)
        {
            GameObject go = new GameObject("CharacterUpgradeManager", typeof(CharacterUpgradeManager));
            upgradeMgr = go.GetComponent<CharacterUpgradeManager>();
            EditorUtility.SetDirty(go);
        }

        CharacterUpgradeConfigSO configSO = AssetDatabase.LoadAssetAtPath<CharacterUpgradeConfigSO>(UPGRADE_CONFIG_PATH);
        if (configSO != null && upgradeMgr != null)
        {
            SerializedObject so = new SerializedObject(upgradeMgr);
            so.FindProperty("upgradeConfig").objectReferenceValue = configSO;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(upgradeMgr);
        }
    }

    private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Image.Type imgType, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = imgType;
        img.color = color;
        return go;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string content,
        float fontSize,
        FontStyles style,
        Color color,
        Vector2 pos,
        Vector2 size,
        TextAlignmentOptions align,
        float charSpacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.characterSpacing = charSpacing;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    // =========================================================================
    // 程序化高畫質 UI 材質繪製引擎 (SDF 平滑邊緣)
    // =========================================================================
    private static Sprite CreateAndLoadSprite(string fileName, Texture2D tex)
    {
        string path = Path.Combine(CUSTOM_ASSET_DIR, fileName).Replace("\\", "/");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static float SdfChamferBox(Vector2 p, Vector2 b, float chamfer)
    {
        Vector2 d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
        float rectDist = Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        float cut = (Mathf.Abs(p.x) + Mathf.Abs(p.y) - (b.x + b.y - chamfer)) * 0.70710678f;
        return Mathf.Max(rectDist, cut);
    }

    private static float SdfRoundedBox(Vector2 p, Vector2 b, float r)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b + new Vector2(r, r);
        return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    private static Texture2D GenerateSoulBarTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 boxHalf = half - new Vector2(2f, 2f);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfChamferBox(p, boxHalf, 10f);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - dist);
                Color fillTop = new Color(0.11f, 0.09f, 0.14f, 0.94f);
                Color fillBot = new Color(0.07f, 0.06f, 0.09f, 0.96f);
                Color c = Color.Lerp(fillBot, fillTop, v);

                float leftGlow = Mathf.Clamp01(1f - Mathf.Abs(x - 36f) / 140f) * 0.15f;
                c = Color.Lerp(c, new Color(0.68f, 0.42f, 0.95f, c.a), leftGlow);

                if (dist > -2.0f)
                {
                    Color borderCol = Color.Lerp(new Color(0.56f, 0.44f, 0.30f, 1f), new Color(0.84f, 0.70f, 0.48f, 1f), v);
                    c = borderCol;
                }
                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateFrameCardTex(int w, int h, float alpha)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 boxHalf = half - new Vector2(2f, 2f);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfChamferBox(p, boxHalf, 8f);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - dist);
                Color c = Color.Lerp(new Color(0.08f, 0.07f, 0.11f, alpha), new Color(0.12f, 0.11f, 0.16f, alpha), v);

                if (dist > -1.8f)
                {
                    c = Color.Lerp(new Color(0.42f, 0.34f, 0.24f, 1f), new Color(0.68f, 0.55f, 0.38f, 1f), v);
                }
                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateTrackTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 boxHalf = half - new Vector2(1f, 1f);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfRoundedBox(p, boxHalf, 4f);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - dist);
                Color c = Color.Lerp(new Color(0.04f, 0.03f, 0.05f, 0.95f), new Color(0.10f, 0.09f, 0.13f, 0.95f), v);
                if (dist > -1.2f)
                {
                    c = new Color(0.30f, 0.24f, 0.18f, 0.85f);
                }
                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateFillTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 boxHalf = half - new Vector2(1f, 1f);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfRoundedBox(p, boxHalf, 4f);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - dist);
                Color c = Color.Lerp(new Color(0.82f, 0.55f, 0.15f, 1f), new Color(0.98f, 0.85f, 0.45f, 1f), v);
                if (y >= h - 3)
                {
                    c = Color.Lerp(c, Color.white, 0.4f);
                }
                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateUpgradeBtnTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 boxHalf = half - new Vector2(2f, 2f);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfRoundedBox(p, boxHalf, 8f);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - dist);
                // 尊貴深邃古銅金按鈕
                Color colBot = new Color(0.18f, 0.13f, 0.10f, 0.98f);
                Color colTop = new Color(0.28f, 0.21f, 0.15f, 0.98f);
                Color c = Color.Lerp(colBot, colTop, v);

                if (dist > -2.2f)
                {
                    c = Color.Lerp(new Color(0.65f, 0.48f, 0.25f, 1f), new Color(0.95f, 0.80f, 0.48f, 1f), v);
                }
                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateSoulIconTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        float scale = w / 36f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = (new Vector2(x + 0.5f, y + 0.5f) - center) / scale;
                float diamond = (Mathf.Abs(p.x) * 1.25f + Mathf.Abs(p.y) * 0.92f) - 12.5f;
                float glow = Mathf.Clamp01(1f - (p.magnitude / 17f));

                if (diamond > 1f)
                {
                    Color glowCol = new Color(0.75f, 0.45f, 0.98f, glow * glow * 0.45f);
                    tex.SetPixel(x, y, glowCol);
                    continue;
                }

                float edgeA = Mathf.Clamp01(0.5f - diamond);
                float core = Mathf.Clamp01(1f - (Mathf.Abs(p.x) * 1.3f + Mathf.Abs(p.y) * 0.95f) / 11f);
                Color outerFacet = new Color(0.66f, 0.33f, 0.96f, 1f);
                Color innerCore = new Color(0.96f, 0.90f, 1.0f, 1f);
                Color c = Color.Lerp(outerFacet, innerCore, core * core);

                if (diamond > -1.6f)
                {
                    c = new Color(0.92f, 0.80f, 1.0f, 1f);
                }
                c.a = edgeA;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateStatEmblemTex(int w, int h, UpgradeType type)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        float radius = (w * 0.5f) - 2f;

        Color mainTheme = (type == UpgradeType.Health) ? new Color(0.85f, 0.22f, 0.24f, 1f) :
                          (type == UpgradeType.Defense) ? new Color(0.24f, 0.52f, 0.90f, 1f) :
                          new Color(0.92f, 0.55f, 0.18f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                float d = p.magnitude - radius;

                if (d > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - d);
                Color c = Color.Lerp(new Color(0.08f, 0.07f, 0.10f, 0.95f), new Color(0.16f, 0.14f, 0.19f, 0.95f), (p.y / radius + 1f) * 0.5f);

                if (d > -2.0f)
                {
                    c = Color.Lerp(new Color(0.55f, 0.40f, 0.25f, 1f), new Color(0.88f, 0.72f, 0.48f, 1f), (p.y / radius + 1f) * 0.5f);
                }
                else if (d > -4.0f)
                {
                    c = Color.Lerp(c, mainTheme, 0.45f);
                }

                Vector2 np = p / radius;
                bool isIcon = false;
                if (type == UpgradeType.Health)
                {
                    float hx = np.x * 1.3f;
                    float hy = np.y * 1.3f + 0.15f;
                    float heart = Mathf.Pow(hx * hx + hy * hy - 0.28f, 3) - hx * hx * hy * hy * hy;
                    if (heart < 0f) isIcon = true;
                }
                else if (type == UpgradeType.Defense)
                {
                    if (Mathf.Abs(np.x) < 0.48f && np.y < 0.45f && np.y > -0.6f + Mathf.Abs(np.x) * 0.8f)
                    {
                        isIcon = true;
                    }
                }
                else
                {
                    if ((Mathf.Abs(np.x) < 0.12f && Mathf.Abs(np.y) < 0.65f) ||
                        (Mathf.Abs(np.y - 0.18f) < 0.11f && Mathf.Abs(np.x) < 0.45f))
                    {
                        isIcon = true;
                    }
                }

                if (isIcon)
                {
                    c = Color.Lerp(c, Color.white, 0.85f);
                }

                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
#endif
