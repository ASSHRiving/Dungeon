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

        // 1. 程序化生成高質感暗黑奇幻升級介面專屬 PNG 素材
        Sprite soulBarSprite = CreateAndLoadSprite("upgrade_soul_bar.png", GenerateSoulBarTex(450, 52));
        Sprite soulIconSprite = CreateAndLoadSprite("upgrade_icon_soul.png", GenerateSoulIconTex(64, 64));
        Sprite slotCardSprite = CreateAndLoadSprite("upgrade_slot_card.png", GenerateSlotCardTex(450, 136));
        Sprite levelBadgeSprite = CreateAndLoadSprite("upgrade_level_badge.png", GenerateLevelBadgeTex(104, 28));
        Sprite btnActionSprite = CreateAndLoadSprite("upgrade_btn_action.png", GenerateSplitUpgradeBtnTex(124, 98));
        Sprite iconHpSprite = CreateAndLoadSprite("upgrade_icon_hp.png", GenerateStatEmblemTex(96, 96, UpgradeType.Health));
        Sprite iconDefSprite = CreateAndLoadSprite("upgrade_icon_def.png", GenerateStatEmblemTex(96, 96, UpgradeType.Defense));
        Sprite iconAtkSprite = CreateAndLoadSprite("upgrade_icon_atk.png", GenerateStatEmblemTex(96, 96, UpgradeType.Attack));

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

        // 3. 移除舊的 CharacterUpgradePanel
        Transform oldPanel = canvas.transform.Find("CharacterUpgradePanel");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        // 4. 建立 CharacterUpgradePanel 根物件 (與 LeftPanel 左右對稱)
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

        // 5. Panel_Background (與 LeftPanel 相同外框材質)
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

        // 8. 三大屬性升級插槽 (Slot_Health / Slot_Defense / Slot_Attack)
        SlotRefs healthRefs = BuildUpgradeSlot(
            panelGO.transform,
            "Slot_Health",
            new Vector2(0f, 86f),
            slotCardSprite,
            iconHpSprite,
            levelBadgeSprite,
            btnActionSprite,
            font,
            "生命強化",
            "Lv.0 / 10",
            "+0 HP → <color=#80FF80>+15</color>",
            "50 靈魂"
        );

        SlotRefs defenseRefs = BuildUpgradeSlot(
            panelGO.transform,
            "Slot_Defense",
            new Vector2(0f, -62f),
            slotCardSprite,
            iconDefSprite,
            levelBadgeSprite,
            btnActionSprite,
            font,
            "防禦強化",
            "Lv.0 / 10",
            "+0 護甲 → <color=#80FF80>+3</color>",
            "50 靈魂"
        );

        SlotRefs attackRefs = BuildUpgradeSlot(
            panelGO.transform,
            "Slot_Attack",
            new Vector2(0f, -210f),
            slotCardSprite,
            iconAtkSprite,
            levelBadgeSprite,
            btnActionSprite,
            font,
            "攻擊強化",
            "Lv.0 / 10",
            "+0 攻擊 → <color=#80FF80>+4</color>",
            "60 靈魂"
        );

        // 9. 綁定按鈕 Persistent OnClick 事件
        UnityEventTools.AddPersistentListener(healthRefs.button.onClick, upgradeCtrl.OnClickUpgradeHealth);
        UnityEventTools.AddPersistentListener(defenseRefs.button.onClick, upgradeCtrl.OnClickUpgradeDefense);
        UnityEventTools.AddPersistentListener(attackRefs.button.onClick, upgradeCtrl.OnClickUpgradeAttack);

        // 10. 使用 SerializedObject 綁定 CharacterUpgradeUIController 所有欄位
        SerializedObject soCtrl = new SerializedObject(upgradeCtrl);
        soCtrl.FindProperty("currentSoulsText").objectReferenceValue = currencyValueText;

        BindSlotProperty(soCtrl.FindProperty("healthSlot"), UpgradeType.Health, healthRefs);
        BindSlotProperty(soCtrl.FindProperty("defenseSlot"), UpgradeType.Defense, defenseRefs);
        BindSlotProperty(soCtrl.FindProperty("attackSlot"), UpgradeType.Attack, attackRefs);
        soCtrl.ApplyModifiedProperties();
        EditorUtility.SetDirty(upgradeCtrl);

        // 11. 綁定至 LobbyManager.upgradeUIController
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
        Debug.Log("✅ [LobbyUpgradeUIBuilder] 全新設計的暗黑金銅風格升級介面與雙層鑲嵌升級按鈕已搭建並完成綁定！");
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

    private struct SlotRefs
    {
        public TMP_Text statTitleText;
        public TMP_Text levelText;
        public TMP_Text bonusText;
        public TMP_Text costText;
        public Button button;
        public CanvasGroup buttonCanvasGroup;
    }

    private static SlotRefs BuildUpgradeSlot(
        Transform parent,
        string slotName,
        Vector2 anchoredPos,
        Sprite slotBgSprite,
        Sprite emblemSprite,
        Sprite levelBadgeSprite,
        Sprite btnSprite,
        TMP_FontAsset font,
        string defaultTitle,
        string defaultLevel,
        string defaultBonus,
        string defaultCost)
    {
        // 插槽底卡 (450 x 136)
        GameObject slotGO = CreateImage(slotName, parent, slotBgSprite, Image.Type.Simple, anchoredPos, new Vector2(450f, 136f), Color.white);

        // 左上屬性徽章圖示 (48 x 48)
        GameObject iconGO = CreateImage("StatIcon", slotGO.transform, emblemSprite, Image.Type.Simple, new Vector2(-180f, 24f), new Vector2(48f, 48f), Color.white);
        iconGO.GetComponent<Image>().raycastTarget = false;

        // 屬性名稱 ("生命強化")
        TMP_Text statTitle = CreateText(
            "StatName",
            slotGO.transform,
            font,
            defaultTitle,
            25f,
            FontStyles.Bold,
            new Color32(245, 239, 230, 255),
            new Vector2(-90f, 24f),
            new Vector2(118f, 40f),
            TextAlignmentOptions.MidlineLeft,
            4f
        );

        // 等級膠囊底座 + 等級文字 ("Lv.0 / 10")
        GameObject lvlBadgeGO = CreateImage("Level_Badge", slotGO.transform, levelBadgeSprite, Image.Type.Simple, new Vector2(18f, 24f), new Vector2(104f, 28f), Color.white);
        lvlBadgeGO.GetComponent<Image>().raycastTarget = false;

        TMP_Text levelTxt = CreateText(
            "Level",
            slotGO.transform,
            font,
            defaultLevel,
            18f,
            FontStyles.Bold,
            new Color32(236, 198, 118, 255),
            new Vector2(18f, 24f),
            new Vector2(100f, 28f),
            TextAlignmentOptions.Center,
            1f
        );

        // 左下加成預覽文字 ("+0 HP → +15")
        TMP_Text bonusTxt = CreateText(
            "Bonus",
            slotGO.transform,
            font,
            defaultBonus,
            21f,
            FontStyles.Bold,
            new Color32(218, 212, 202, 255),
            new Vector2(-56f, -31f),
            new Vector2(252f, 36f),
            TextAlignmentOptions.MidlineLeft,
            2f
        );

        // 右側雙層鑲嵌式強化按鈕 (124 x 98，上層琥珀金「強化」、下層黑曜石凹槽顯示消耗靈魂)
        GameObject btnGO = CreateImage("Btn_Upgrade", slotGO.transform, btnSprite, Image.Type.Simple, new Vector2(148f, 0f), new Vector2(124f, 98f), Color.white);
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnGO.GetComponent<Image>();

        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = cb;

        CanvasGroup btnCG = btnGO.AddComponent<CanvasGroup>();
        btnGO.AddComponent<UIButtonTween>();

        CreateText(
            "BtnLabel",
            btnGO.transform,
            font,
            "強 化",
            23f,
            FontStyles.Bold,
            new Color32(255, 248, 232, 255),
            new Vector2(0f, 19f),
            new Vector2(114f, 36f),
            TextAlignmentOptions.Center,
            8f
        );

        TMP_Text costTxt = CreateText(
            "CostText",
            btnGO.transform,
            font,
            defaultCost,
            18f,
            FontStyles.Bold,
            new Color32(255, 255, 255, 255),
            new Vector2(0f, -24f),
            new Vector2(116f, 30f),
            TextAlignmentOptions.Center,
            2f
        );

        return new SlotRefs
        {
            statTitleText = statTitle,
            levelText = levelTxt,
            bonusText = bonusTxt,
            costText = costTxt,
            button = btn,
            buttonCanvasGroup = btnCG
        };
    }

    private static void BindSlotProperty(SerializedProperty slotProp, UpgradeType type, SlotRefs refs)
    {
        if (slotProp == null) return;
        slotProp.FindPropertyRelative("upgradeType").enumValueIndex = (int)type;
        slotProp.FindPropertyRelative("statTitleText").objectReferenceValue = refs.statTitleText;
        slotProp.FindPropertyRelative("levelText").objectReferenceValue = refs.levelText;
        slotProp.FindPropertyRelative("bonusText").objectReferenceValue = refs.bonusText;
        slotProp.FindPropertyRelative("costText").objectReferenceValue = refs.costText;
        slotProp.FindPropertyRelative("upgradeButton").objectReferenceValue = refs.button;
        slotProp.FindPropertyRelative("buttonCanvasGroup").objectReferenceValue = refs.buttonCanvasGroup;
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
    // 程序化高畫質 UI 材質繪製引擎
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
                // 深邃紫黑曜石漸層底
                Color fillTop = new Color(0.11f, 0.09f, 0.14f, 0.94f);
                Color fillBot = new Color(0.07f, 0.06f, 0.09f, 0.96f);
                Color c = Color.Lerp(fillBot, fillTop, v);

                // 左側紫晶微光暈
                float leftGlow = Mathf.Clamp01(1f - Mathf.Abs(x - 36f) / 140f) * 0.15f;
                c = Color.Lerp(c, new Color(0.68f, 0.42f, 0.95f, c.a), leftGlow);

                // 金紫雙色細緻外框
                if (dist > -2.0f)
                {
                    Color borderCol = Color.Lerp(new Color(0.56f, 0.44f, 0.30f, 1f), new Color(0.84f, 0.70f, 0.48f, 1f), v);
                    c = borderCol;
                }
                else if (dist > -4.5f && dist < -3.0f)
                {
                    c = Color.Lerp(c, new Color(0.58f, 0.42f, 0.76f, c.a), 0.4f);
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
                // 菱形靈魂結晶 (上下略修長)
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

    private static Texture2D GenerateSlotCardTex(int w, int h)
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
                // 暖黑石板底色 (上淺下深)
                Color topCol = new Color(0.15f, 0.14f, 0.13f, 0.95f);
                Color botCol = new Color(0.10f, 0.09f, 0.09f, 0.97f);
                Color c = Color.Lerp(botCol, topCol, v);

                // 左下預覽區微暗凹槽感 (y < 60, x < 296)
                if (x >= 16 && x <= 294 && y >= 14 && y <= 56)
                {
                    c = Color.Lerp(c, new Color(0.06f, 0.06f, 0.06f, 0.97f), 0.45f);
                }

                // 外框金銅線
                if (dist > -2.0f)
                {
                    float bevel = Mathf.Lerp(0.75f, 1.2f, v);
                    c = new Color(0.58f * bevel, 0.47f * bevel, 0.30f * bevel, 1f);
                }
                else if (dist > -5.0f && dist < -3.5f)
                {
                    c = Color.Lerp(c, new Color(0.45f, 0.36f, 0.22f, c.a), 0.45f);
                }

                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }

        // 左區水平分隔細線 (y = 60, x = 20..294)
        for (int x = 20; x <= 294; x++)
        {
            float norm = Mathf.Abs(x - 157f) / 137f;
            float lineA = Mathf.Clamp01(1f - norm * norm) * 0.65f;
            Color cur = tex.GetPixel(x, 60);
            tex.SetPixel(x, 60, Color.Lerp(cur, new Color(0.84f, 0.68f, 0.42f, 1f), lineA));
        }

        // 左下預覽區小菱形標記 (cx = 28, cy = 36)
        DrawDiamond(tex, 28, 36, 4, new Color32(224, 176, 92, 255));

        // 左右分區垂直分隔線 (x = 302, y = 16..h-16)
        for (int y = 16; y <= h - 16; y++)
        {
            float norm = Mathf.Abs(y - h * 0.5f) / (h * 0.5f - 16f);
            float lineA = Mathf.Clamp01(1f - norm * norm) * 0.65f;
            Color cur = tex.GetPixel(302, y);
            tex.SetPixel(302, y, Color.Lerp(cur, new Color(0.82f, 0.66f, 0.40f, 1f), lineA));
        }
        DrawDiamond(tex, 302, h / 2, 4, new Color32(214, 172, 96, 220));

        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateLevelBadgeTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 boxHalf = half - new Vector2(1.5f, 1.5f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfChamferBox(p, boxHalf, 6f);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }
                float a = Mathf.Clamp01(0.5f - dist);
                Color c = new Color(0.07f, 0.06f, 0.06f, 0.95f);
                if (dist > -1.6f)
                {
                    c = new Color(0.68f, 0.54f, 0.32f, 0.95f);
                }
                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 雙層鑲嵌式強化按鈕：
    /// 上層 (y >= 42)：琥珀暗金立體斜角按鈕面 (顯示「強化」)
    /// 下層 (y < 41)：黑曜石深色鑲嵌凹槽 (顯示「50 靈魂」或紅字「50 靈魂」，對比極高且絕不撞色)
    /// </summary>
    private static Texture2D GenerateSplitUpgradeBtnTex(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 boxHalf = half - new Vector2(2f, 2f);
        int splitY = 42;

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfChamferBox(p, boxHalf, 9f);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - dist);
                Color c;

                if (y >= splitY)
                {
                    // 上半部：皇家琥珀暗金立體漸層
                    float topV = (float)(y - splitY) / (h - splitY);
                    Color goldBot = new Color(0.36f, 0.21f, 0.07f, 1f);
                    Color goldTop = new Color(0.64f, 0.40f, 0.14f, 1f);
                    c = Color.Lerp(goldBot, goldTop, topV);

                    // 頂部內側高光
                    if (y >= h - 6 && dist <= -2.5f)
                    {
                        c = Color.Lerp(c, new Color(0.95f, 0.78f, 0.45f, 1f), 0.35f);
                    }
                }
                else
                {
                    // 下半部：黑曜石深色鑲嵌凹槽 (讓白字與不足時的紅字都清晰銳利)
                    float botV = (float)y / splitY;
                    Color trayBot = new Color(0.07f, 0.06f, 0.06f, 0.98f);
                    Color trayTop = new Color(0.11f, 0.10f, 0.09f, 0.98f);
                    c = Color.Lerp(trayBot, trayTop, botV);
                }

                // 上下分界金線 (y == splitY or splitY - 1)
                if ((y == splitY || y == splitY - 1) && dist <= -2f)
                {
                    c = (y == splitY) ? new Color(0.88f, 0.72f, 0.42f, 1f) : new Color(0.22f, 0.16f, 0.08f, 1f);
                }

                // 外框立體金邊
                if (dist > -2.4f)
                {
                    float bevel = Mathf.Lerp(0.72f, 1.25f, v);
                    c = new Color(0.76f * bevel, 0.60f * bevel, 0.34f * bevel, 1f);
                }
                else if (dist > -4.6f && dist < -3.2f && y >= splitY)
                {
                    c = Color.Lerp(c, new Color(0.96f, 0.82f, 0.50f, 1f), 0.30f);
                }

                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }

        // 分界線中央小金鑽裝飾
        DrawDiamond(tex, w / 2, splitY, 3, new Color32(245, 208, 125, 255));

        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateStatEmblemTex(int w, int h, UpgradeType type)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
        float scale = w / 48f;
        Vector2 boxHalf = half - new Vector2(1.5f * scale, 1.5f * scale);

        Color glowTint = (type == UpgradeType.Health)
            ? new Color(0.85f, 0.22f, 0.22f, 1f)
            : (type == UpgradeType.Defense ? new Color(0.28f, 0.58f, 0.92f, 1f) : new Color(0.92f, 0.58f, 0.18f, 1f));

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / h;
            for (int x = 0; x < w; x++)
            {
                Vector2 pRaw = new Vector2(x + 0.5f, y + 0.5f) - half;
                float dist = SdfChamferBox(pRaw, boxHalf, 8f * scale);
                if (dist > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float a = Mathf.Clamp01(0.5f - dist);
                Vector2 p = pRaw / scale;
                float radial = Mathf.Clamp01(1f - pRaw.magnitude / (w * 0.48f));
                Color c = Color.Lerp(new Color(0.08f, 0.07f, 0.07f, 0.98f), glowTint * 0.38f, radial * radial);
                c.a = 0.98f;

                if (dist > -2.0f * scale)
                {
                    float bevel = Mathf.Lerp(0.75f, 1.2f, v);
                    c = new Color(0.68f * bevel, 0.54f * bevel, 0.32f * bevel, 1f);
                }

                // 繪製內部抗鋸齒幾何徽章圖騰
                if (type == UpgradeType.Health)
                {
                    float dV = Mathf.Max(Mathf.Abs(p.x) - 3.2f, Mathf.Abs(p.y) - 11.2f);
                    float dH = Mathf.Max(Mathf.Abs(p.y) - 3.2f, Mathf.Abs(p.x) - 11.2f);
                    float dDia = (Mathf.Abs(p.x) + Mathf.Abs(p.y)) * 0.7071f - 6.8f;
                    float shapeDist = Mathf.Min(Mathf.Min(dV, dH), dDia) * scale;
                    float shapeAlpha = Mathf.Clamp01(0.5f - shapeDist);
                    if (shapeAlpha > 0f)
                    {
                        Color emblemCol = Color.Lerp(new Color(0.86f, 0.18f, 0.22f, 1f), new Color(1f, 0.65f, 0.65f, 1f), v);
                        c = Color.Lerp(c, emblemCol, shapeAlpha);
                    }
                }
                else if (type == UpgradeType.Defense)
                {
                    float sy = p.y;
                    float maxW = (sy >= -1f) ? 10.2f : Mathf.Max(0f, 10.2f * (1f - (-1f - sy) / 12.2f));
                    float dY = Mathf.Max(sy - 10.8f, -12.2f - sy);
                    float dX = Mathf.Abs(p.x) - maxW;
                    float shieldDist = Mathf.Max(dX, dY) * scale;
                    float shieldAlpha = Mathf.Clamp01(0.5f - shieldDist);
                    if (shieldAlpha > 0f)
                    {
                        float innerEdge = Mathf.Max(Mathf.Abs(p.x) - (maxW - 2.2f), sy - 8.6f) * scale;
                        float borderMix = Mathf.Clamp01(0.5f + innerEdge);
                        Color innerCol = Color.Lerp(new Color(0.22f, 0.45f, 0.76f, 1f), new Color(0.55f, 0.80f, 0.98f, 1f), v);
                        Color rimCol = new Color(0.92f, 0.80f, 0.52f, 1f);
                        Color emblemCol = Color.Lerp(innerCol, rimCol, borderMix);
                        c = Color.Lerp(c, emblemCol, shieldAlpha);
                    }
                }
                else
                {
                    float rx = p.x * 0.7071f + p.y * 0.7071f;
                    float ry = -p.x * 0.7071f + p.y * 0.7071f;
                    float tipTaper = Mathf.Max(0f, (rx - 8f) * 0.55f);
                    float dBlade = Mathf.Max(Mathf.Abs(ry) - Mathf.Max(0f, 2.3f - tipTaper), Mathf.Max(rx - 13.5f, -4.5f - rx)) * scale;
                    float dGuard = Mathf.Max(Mathf.Abs(ry) - 7.2f, Mathf.Max(rx - (-4.2f), -7.2f - rx)) * scale;
                    float dGrip = Mathf.Max(Mathf.Abs(ry) - 1.9f, Mathf.Max(rx - (-7.2f), -12.8f - rx)) * scale;

                    float bladeA = Mathf.Clamp01(0.5f - dBlade);
                    float hiltA = Mathf.Clamp01(0.5f - Mathf.Min(dGuard, dGrip));

                    if (hiltA > 0f)
                    {
                        c = Color.Lerp(c, new Color(0.85f, 0.58f, 0.22f, 1f), hiltA);
                    }
                    if (bladeA > 0f)
                    {
                        Color bladeCol = Color.Lerp(new Color(0.95f, 0.72f, 0.28f, 1f), new Color(1f, 0.96f, 0.84f, 1f), Mathf.Clamp01((rx + 5f) / 18f));
                        c = Color.Lerp(c, bladeCol, bladeA);
                    }
                }

                c.a *= a;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static void DrawDiamond(Texture2D t, int cx, int cy, int r, Color32 col)
    {
        for (int dy = -r; dy <= r; dy++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= r)
                {
                    int px = cx + dx, py = cy + dy;
                    if (px >= 0 && px < t.width && py >= 0 && py < t.height)
                        t.SetPixel(px, py, col);
                }
            }
        }
    }
}
#endif
