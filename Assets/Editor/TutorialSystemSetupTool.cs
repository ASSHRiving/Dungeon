using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 互動式新手教學系統一鍵建置工具：
/// 1. 程序化生成暗黑奇幻金銅＋地牢石板風格 UI Sprite (Assets/SpritesUI/Tutorial/)
/// 2. 生成 6 個 TutorialStepSO 與 TutorialSequence_Default (Assets/ScriptableObjects/Tutorial/)
/// 3. 建立獨立 Sub-Canvas Prefab (Assets/Prefab/TutorialHUD.prefab) 並配置進 GameScene.unity
/// 4. 停用舊版阻斷式 TutorialPanel，並於 SettingsPanel/PreferType 新增「重置新手教學」按鈕
/// </summary>
public static class TutorialSystemSetupTool
{
    private const string SpritesDir = "Assets/SpritesUI/Tutorial";
    private const string SoDir = "Assets/ScriptableObjects/Tutorial";
    private const string PrefabPath = "Assets/Prefab/TutorialHUD.prefab";
    private const string ShadowMatPath = "Assets/SpritesUI/Tutorial/SourceHanSerifTW_TutorialShadow.mat";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string InputActionsPath = "Assets/Script/Input/InputController.inputactions";
    private const string FontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/SourceHanSerifTW-SemiBold SDF.asset";

    [MenuItem("Dungeon/Tutorial/一鍵建置互動式新手教學系統")]
    public static void RunAll()
    {
        EnsureFolder(SpritesDir);
        EnsureFolder(SoDir);
        EnsureFolder("Assets/Prefab");

        GenerateAllSprites();
        TutorialSequenceSO sequence = CreateOrUpdateTutorialAssets();
        GameObject prefab = BuildOrUpdateTutorialHudPrefab(sequence);
        SetupGameScene(sequence, prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TutorialSystemSetupTool] 互動式新手教學系統建置完成！");
    }

    #region 1. 程序化 UI Sprite 生成
    private static void GenerateAllSprites()
    {
        DeleteAssetIfExists($"{SpritesDir}/tutorial_card_bg.png");
        DeleteAssetIfExists($"{SpritesDir}/tutorial_divider.png");
        DeleteAssetIfExists($"{SpritesDir}/tutorial_skip_btn.png");

        CreateProgressTrackSprite($"{SpritesDir}/tutorial_progress_track.png", 64, 16);
        CreateProgressFillSprite($"{SpritesDir}/tutorial_progress_fill.png", 64, 16);
        CreateCheckBadgeSprite($"{SpritesDir}/tutorial_check_badge.png", 64, 64);
        CreateCheckMarkSprite($"{SpritesDir}/tutorial_check_mark.png", 64, 64);
        AssetDatabase.Refresh();
    }

    private static void DeleteAssetIfExists(string assetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private static void CreateProgressTrackSprite(string path, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];
        Vector2 halfSize = new Vector2(w * 0.5f - 1f, h * 0.5f - 1f);

        Color bg = new Color(0.055f, 0.051f, 0.047f, 0.95f);
        Color edge = new Color(0.45f, 0.36f, 0.22f, 0.7f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - w * 0.5f, y + 0.5f - h * 0.5f);
                float dist = SdfChamferBox(p, halfSize, 3f);
                float alpha = Mathf.Clamp01(0.5f - dist);
                float border = Mathf.Clamp01(1f - Mathf.Abs(dist + 0.8f));
                Color c = Color.Lerp(bg, edge, border * 0.8f);
                c.a *= alpha;
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        SavePngAndConfigureSprite(path, tex, new Vector4(6, 4, 6, 4));
    }

    private static void CreateProgressFillSprite(string path, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];
        Vector2 halfSize = new Vector2(w * 0.5f - 1f, h * 0.5f - 1f);

        for (int y = 0; y < h; y++)
        {
            float v = (float)y / (h - 1);
            float highlight = Mathf.Lerp(0.78f, 1.0f, v);
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - w * 0.5f, y + 0.5f - h * 0.5f);
                float dist = SdfChamferBox(p, halfSize, 2.5f);
                float alpha = Mathf.Clamp01(0.5f - dist);
                Color c = new Color(highlight, highlight, highlight, alpha);
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        SavePngAndConfigureSprite(path, tex, new Vector4(6, 4, 6, 4));
    }

    private static void CreateCheckBadgeSprite(string path, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];
        Vector2 halfSize = new Vector2(w * 0.5f - 4f, h * 0.5f - 4f);

        Color bg = new Color(0.10f, 0.095f, 0.09f, 0.95f);
        Color border = new Color(0.831f, 0.639f, 0.349f, 0.9f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f - w * 0.5f, y + 0.5f - h * 0.5f);
                float dist = SdfChamferBox(p, halfSize, 10f);
                float alpha = Mathf.Clamp01(0.5f - dist);
                float edge = Mathf.Clamp01(1f - Mathf.Abs(dist + 1.4f) / 1.4f);
                Color c = Color.Lerp(bg, border, edge * 0.9f);
                c.a *= alpha;
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        SavePngAndConfigureSprite(path, tex, Vector4.zero);
    }

    private static void CreateCheckMarkSprite(string path, int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[w * h];

        Vector2 a = new Vector2(16f, 32f);
        Vector2 b = new Vector2(27f, 20f);
        Vector2 c = new Vector2(49f, 44f);
        float thickness = 4.2f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d1 = DistanceToSegment(p, a, b);
                float d2 = DistanceToSegment(p, b, c);
                float d = Mathf.Min(d1, d2) - thickness;
                float alpha = Mathf.Clamp01(0.5f - d);
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        SavePngAndConfigureSprite(path, tex, Vector4.zero);
    }

    private static float SdfChamferBox(Vector2 p, Vector2 b, float chamfer)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
        float rectDist = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        float cut = (Mathf.Abs(p.x) + Mathf.Abs(p.y) - (b.x + b.y - chamfer)) * 0.70710678f;
        return Mathf.Max(rectDist, cut);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a;
        Vector2 ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
        return (pa - ba * h).magnitude;
    }

    private static void SavePngAndConfigureSprite(string assetPath, Texture2D tex, Vector4 border)
    {
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }
    }
    #endregion

    #region 2. ScriptableObject 資料層資產建置
    private static TutorialSequenceSO CreateOrUpdateTutorialAssets()
    {
        DeleteAssetIfExists($"{SoDir}/Step_04_Attack.asset");
        DeleteAssetIfExists($"{SoDir}/Step_05_Interact.asset");
        DeleteAssetIfExists($"{SoDir}/Step_05_Kill.asset");
        DeleteAssetIfExists($"{SoDir}/Step_06_Interact.asset");
        DeleteAssetIfExists($"{SoDir}/Step_06_Kill.asset");

        List<TutorialStepSO> steps = new List<TutorialStepSO>
        {
            UpsertStepAsset<MoveStepSO>("Step_01_Move.asset", s =>
            {
                s.stepId = "move";
                s.descriptionTemplate = "按 {Movement} 移動";
                s.actionBindings = new List<TutorialActionPlaceholder>
                {
                    new TutorialActionPlaceholder { placeholder = "{Movement}", actionPath = "Player/Movement", isCompositeGroup = true }
                };
                s.targetProgress = 1.2f;
                s.showProgressBar = true;
                s.isDurationCondition = true;
            }),

            UpsertStepAsset<RunStepSO>("Step_02_Run.asset", s =>
            {
                s.stepId = "run";
                s.descriptionTemplate = "移動時按住 {Run} 奔跑";
                s.actionBindings = new List<TutorialActionPlaceholder>
                {
                    new TutorialActionPlaceholder { placeholder = "{Run}", actionPath = "Player/Run", isCompositeGroup = false }
                };
                s.targetProgress = 0.8f;
                s.showProgressBar = true;
                s.isDurationCondition = true;
            }),

            UpsertStepAsset<AttackStepSO>("Step_03_Attack.asset", s =>
            {
                s.stepId = "attack";
                s.descriptionTemplate = "按 {LAtk} 普通攻擊";
                s.actionBindings = new List<TutorialActionPlaceholder>
                {
                    new TutorialActionPlaceholder { placeholder = "{LAtk}", actionPath = "Player/LAtk", isCompositeGroup = false }
                };
                s.targetProgress = 2f;
                s.showProgressBar = true;
                s.isDurationCondition = false;
            }),

            UpsertStepAsset<InteractStepSO>("Step_04_Interact.asset", s =>
            {
                s.stepId = "interact";
                s.descriptionTemplate = "靠近門或寶箱按 {Interact} 互動";
                s.actionBindings = new List<TutorialActionPlaceholder>
                {
                    new TutorialActionPlaceholder { placeholder = "{Interact}", actionPath = "Player/Interact", isCompositeGroup = false }
                };
                s.targetProgress = 1f;
                s.showProgressBar = true;
                s.isDurationCondition = false;
            }),

            UpsertStepAsset<PerfectDodgeStepSO>("Step_06_Dodge.asset", s =>
            {
                s.stepId = "dodge";
                s.descriptionTemplate = "敵人攻擊！按 {RAtk} 閃避";
                s.actionBindings = new List<TutorialActionPlaceholder>
                {
                    new TutorialActionPlaceholder { placeholder = "{RAtk}", actionPath = "Player/RAtk", isCompositeGroup = false }
                };
                s.targetProgress = 1f;
                s.showProgressBar = true;
                s.isDurationCondition = false;
            }),

            UpsertStepAsset<KillEnemyStepSO>("Step_05_Kill.asset", s =>
            {
                s.stepId = "kill";
                s.descriptionTemplate = "擊敗房間內的敵人";
                s.actionBindings = new List<TutorialActionPlaceholder>();
                s.targetProgress = 1f;
                s.showProgressBar = true;
                s.isDurationCondition = false;
                s.canTriggerAnytime = false;
            }),

            UpsertStepAsset<ShopInteractStepSO>("Step_shop.asset", s =>
            {
                s.stepId = "shop";
                s.descriptionTemplate = "使用 {Interact} 購買物品";
                s.actionBindings = new List<TutorialActionPlaceholder>
                {
                    new TutorialActionPlaceholder { placeholder = "{Interact}", actionPath = "Player/Interact", isCompositeGroup = false }
                };
                s.targetProgress = 1f;
                s.showProgressBar = true;
                s.isDurationCondition = false;
                s.canTriggerAnytime = true;
            })
        };

        string seqPath = $"{SoDir}/TutorialSequence_Default.asset";
        TutorialSequenceSO seq = AssetDatabase.LoadAssetAtPath<TutorialSequenceSO>(seqPath);
        if (seq == null)
        {
            seq = ScriptableObject.CreateInstance<TutorialSequenceSO>();
            AssetDatabase.CreateAsset(seq, seqPath);
        }

        seq.steps = steps;
        EditorUtility.SetDirty(seq);
        return seq;
    }

    private static T UpsertStepAsset<T>(string fileName, System.Action<T> configure) where T : TutorialStepSO
    {
        string path = $"{SoDir}/{fileName}";
        T step = AssetDatabase.LoadAssetAtPath<T>(path);
        if (step == null)
        {
            step = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(step, path);
        }
        configure(step);
        EditorUtility.SetDirty(step);
        return step;
    }
    #endregion

    #region 3. TutorialHUD Prefab 建置（極簡無底板浮動設計 + 左中側非阻塞）
    private static Material GetOrCreateShadowTextMaterial(TMP_FontAsset font)
    {
        if (font == null || font.material == null) return null;

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(ShadowMatPath);
        if (mat == null)
        {
            mat = new Material(font.material);
            AssetDatabase.CreateAsset(mat, ShadowMatPath);
        }
        else
        {
            mat.CopyPropertiesFromMaterial(font.material);
        }

        mat.EnableKeyword("OUTLINE_ON");
        mat.SetColor("_OutlineColor", new Color(0.03f, 0.025f, 0.025f, 0.94f));
        mat.SetFloat("_OutlineWidth", 0.18f);

        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
        mat.SetFloat("_UnderlayOffsetX", 0.55f);
        mat.SetFloat("_UnderlayOffsetY", -0.55f);
        mat.SetFloat("_UnderlayDilate", 0.22f);
        mat.SetFloat("_UnderlaySoftness", 0.35f);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject BuildOrUpdateTutorialHudPrefab(TutorialSequenceSO sequence)
    {
        TMP_FontAsset font = LoadChineseFont();
        Material shadowMat = GetOrCreateShadowTextMaterial(font);
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/tutorial_progress_track.png");
        Sprite fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/tutorial_progress_fill.png");
        Sprite checkBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/tutorial_check_badge.png");
        Sprite checkMarkSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesDir}/tutorial_check_mark.png");

        GameObject root = new GameObject("TutorialHUD", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Canvas subCanvas = root.GetComponent<Canvas>();
        subCanvas.overrideSorting = true;
        subCanvas.sortingOrder = 15;

        CanvasGroup rootCG = root.GetComponent<CanvasGroup>();
        rootCG.alpha = 1f;
        rootCG.interactable = false;
        rootCG.blocksRaycasts = false;

        // 極簡浮動容器：無背景底板、無階段標題、左側約七成高度 (anchor y=0.7)
        GameObject card = CreateUIObject("TutorialCard", root.transform);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0f, 0.7f);
        cardRT.anchorMax = new Vector2(0f, 0.7f);
        cardRT.pivot = new Vector2(0f, 0.5f);
        cardRT.anchoredPosition = new Vector2(-420f, 0f);
        cardRT.sizeDelta = new Vector2(380f, 76f);

        CanvasGroup cardCG = card.AddComponent<CanvasGroup>();
        cardCG.alpha = 0f;

        // 勾選狀態小框（左側 24x24）
        GameObject badgeObj = CreateUIObject("StatusBadge", card.transform);
        SetRect(badgeObj.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -4f), new Vector2(24f, 24f));
        Image badgeImg = badgeObj.AddComponent<Image>();
        badgeImg.sprite = checkBadgeSprite;
        badgeImg.color = new Color(1f, 1f, 1f, 0.85f);
        badgeImg.raycastTarget = false;

        GameObject checkObj = CreateUIObject("CheckIcon", badgeObj.transform);
        RectTransform checkRT = checkObj.GetComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.5f, 0.5f);
        checkRT.anchorMax = new Vector2(0.5f, 0.5f);
        checkRT.pivot = new Vector2(0.5f, 0.5f);
        checkRT.anchoredPosition = Vector2.zero;
        checkRT.sizeDelta = new Vector2(18f, 18f);
        checkRT.localScale = Vector3.zero;
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.sprite = checkMarkSprite;
        checkImg.color = new Color(0.369f, 0.918f, 0.831f, 1f);
        checkImg.raycastTarget = false;

        // 直接操作指令文字（單行清爽、無重複標題、無底框，靠外框陰影清晰辨識）
        TMP_Text descText = CreateText("DescriptionText", card.transform, font, "按 <color=#FCECA6>[WASD]</color> 移動", 21, new Color(0.961f, 0.937f, 0.902f, 1f), TextAlignmentOptions.Left, shadowMat);
        descText.fontStyle = FontStyles.Bold;
        descText.textWrappingMode = TextWrappingModes.NoWrap;
        SetRect(descText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -1f), new Vector2(346f, 30f));

        // 細進度條列（緊貼指令下方）
        GameObject progressRow = CreateUIObject("ProgressRow", card.transform);
        SetRect(progressRow.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -35f), new Vector2(240f, 16f));

        GameObject trackObj = CreateUIObject("ProgressTrack", progressRow.transform);
        SetRect(trackObj.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(174f, 6f));
        Image trackImg = trackObj.AddComponent<Image>();
        trackImg.sprite = trackSprite;
        trackImg.type = Image.Type.Sliced;
        trackImg.color = new Color(1f, 1f, 1f, 0.8f);
        trackImg.raycastTarget = false;

        GameObject fillObj = CreateUIObject("ProgressFill", trackObj.transform);
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(1f, 1f);
        fillRT.offsetMax = new Vector2(-1f, -1f);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = fillSprite;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = 0;
        fillImg.fillAmount = 0f;
        fillImg.color = new Color(0.878f, 0.624f, 0.243f, 1f);
        fillImg.raycastTarget = false;

        TMP_Text progressValText = CreateText("ProgressValueText", progressRow.transform, font, "0.0/1.2s", 13, new Color(0.839f, 0.765f, 0.647f, 1f), TextAlignmentOptions.Left, shadowMat);
        SetRect(progressValText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(182f, 0f), new Vector2(64f, 18f));

        // 輕量純文字略過提示（無按鈕邊框）
        GameObject skipObj = CreateUIObject("SkipButton", card.transform);
        RectTransform skipRT = skipObj.GetComponent<RectTransform>();
        SetRect(skipRT, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -55f), new Vector2(92f, 20f));

        // 使用全透明 Image 僅保留極小點擊區，無任何邊框底色
        Image skipHitImg = skipObj.AddComponent<Image>();
        skipHitImg.color = new Color(1f, 1f, 1f, 0f);
        skipHitImg.raycastTarget = true;

        Button skipBtn = skipObj.AddComponent<Button>();
        skipBtn.transition = Selectable.Transition.None;

        TMP_Text skipLabel = CreateText("SkipText", skipObj.transform, font, "長按 Tab 略過", 12, new Color(0.74f, 0.72f, 0.68f, 0.78f), TextAlignmentOptions.Left, shadowMat);
        skipLabel.rectTransform.anchorMin = Vector2.zero;
        skipLabel.rectTransform.anchorMax = Vector2.one;
        skipLabel.rectTransform.offsetMin = Vector2.zero;
        skipLabel.rectTransform.offsetMax = Vector2.zero;

        GameObject skipFillObj = CreateUIObject("SkipHoldFill", skipObj.transform);
        RectTransform skipFillRT = skipFillObj.GetComponent<RectTransform>();
        skipFillRT.anchorMin = new Vector2(0f, 0f);
        skipFillRT.anchorMax = new Vector2(1f, 0f);
        skipFillRT.pivot = new Vector2(0f, 0f);
        skipFillRT.anchoredPosition = new Vector2(0f, -2f);
        skipFillRT.sizeDelta = new Vector2(0f, 2f);
        Image skipFillImg = skipFillObj.AddComponent<Image>();
        skipFillImg.sprite = fillSprite;
        skipFillImg.type = Image.Type.Filled;
        skipFillImg.fillMethod = Image.FillMethod.Horizontal;
        skipFillImg.fillOrigin = 0;
        skipFillImg.fillAmount = 0f;
        skipFillImg.color = new Color(0.988f, 0.925f, 0.651f, 0.9f);
        skipFillImg.raycastTarget = false;

        // 掛載並綁定 TutorialHUDController 與 TutorialManager
        TutorialHUDController hudController = root.AddComponent<TutorialHUDController>();
        SerializedObject hudSO = new SerializedObject(hudController);
        hudSO.FindProperty("rootCanvasGroup").objectReferenceValue = rootCG;
        hudSO.FindProperty("cardCanvasGroup").objectReferenceValue = cardCG;
        hudSO.FindProperty("cardRect").objectReferenceValue = cardRT;
        hudSO.FindProperty("descriptionText").objectReferenceValue = descText;
        hudSO.FindProperty("progressRow").objectReferenceValue = progressRow;
        hudSO.FindProperty("progressFillImage").objectReferenceValue = fillImg;
        hudSO.FindProperty("progressValueText").objectReferenceValue = progressValText;
        hudSO.FindProperty("checkIconRect").objectReferenceValue = checkRT;
        hudSO.FindProperty("checkIconImage").objectReferenceValue = checkImg;
        hudSO.FindProperty("skipButton").objectReferenceValue = skipBtn;
        hudSO.FindProperty("skipHoldFillImage").objectReferenceValue = skipFillImg;
        hudSO.FindProperty("shownPosX").floatValue = 28f;
        hudSO.FindProperty("hiddenPosX").floatValue = -420f;
        hudSO.ApplyModifiedPropertiesWithoutUndo();

        TutorialManager manager = root.AddComponent<TutorialManager>();
        SerializedObject mgrSO = new SerializedObject(manager);
        mgrSO.FindProperty("tutorialSequence").objectReferenceValue = sequence;
        mgrSO.FindProperty("hudController").objectReferenceValue = hudController;
        mgrSO.FindProperty("fallbackInputActions").objectReferenceValue = inputActions;
        mgrSO.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }
    #endregion

    #region 4. GameScene 場景整合與 SettingPanel 重置按鈕
    private static void SetupGameScene(TutorialSequenceSO sequence, GameObject tutorialHudPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        // 1. 停用舊版阻斷式 TutorialPanel
        GameObject oldTutorialPanel = GameObject.Find("TutorialPanel");
        if (oldTutorialPanel != null)
        {
            oldTutorialPanel.SetActive(false);
            EditorUtility.SetDirty(oldTutorialPanel);
        }

        // 2. 找尋主 Canvas 並安插 TutorialHUD
        Canvas mainCanvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c.gameObject.name == "Canvas" && c.transform.parent == null)
            {
                mainCanvas = c;
                break;
            }
        }

        if (mainCanvas == null)
        {
            Debug.LogError("[TutorialSystemSetupTool] 找不到 GameScene 中的根節點 Canvas！");
            return;
        }

        UIController uiController = Object.FindFirstObjectByType<UIController>(FindObjectsInactive.Include);

        Transform existingHud = mainCanvas.transform.Find("TutorialHUD");
        if (existingHud != null)
        {
            Object.DestroyImmediate(existingHud.gameObject);
        }

        GameObject hudInstance = PrefabUtility.InstantiatePrefab(tutorialHudPrefab, mainCanvas.transform) as GameObject;
        if (hudInstance != null)
        {
            hudInstance.name = "TutorialHUD";
            TutorialManager mgr = hudInstance.GetComponent<TutorialManager>();
            if (mgr != null)
            {
                SerializedObject mgrSO = new SerializedObject(mgr);
                mgrSO.FindProperty("tutorialSequence").objectReferenceValue = sequence;
                mgrSO.FindProperty("uiController").objectReferenceValue = uiController;
                mgrSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // 3. 在 SettingsPanel/PreferType 新增「重置新手教學」按鈕列
        SetupSettingsResetButton();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupSettingsResetButton()
    {
        SettingsController settings = Object.FindFirstObjectByType<SettingsController>(FindObjectsInactive.Include);
        if (settings == null)
        {
            Debug.LogWarning("[TutorialSystemSetupTool] 找不到 SettingsController，略過設定頁按鈕配置。");
            return;
        }

        SerializedObject settingsSO = new SerializedObject(settings);
        GameObject preferType = settingsSO.FindProperty("preferType").objectReferenceValue as GameObject;
        if (preferType == null)
        {
            Debug.LogWarning("[TutorialSystemSetupTool] SettingsController 缺少 preferType 參照。");
            return;
        }

        Transform existingRow = preferType.transform.Find("Row_ResetTutorial");
        if (existingRow != null)
        {
            Object.DestroyImmediate(existingRow.gameObject);
        }

        Transform templateRow = preferType.transform.Find("Row2 (3)");
        if (templateRow == null && preferType.transform.childCount > 0)
        {
            templateRow = preferType.transform.GetChild(preferType.transform.childCount - 1);
        }
        if (templateRow == null)
        {
            Debug.LogWarning("[TutorialSystemSetupTool] 找不到 PreferType 樣板列。");
            return;
        }

        GameObject newRow = Object.Instantiate(templateRow.gameObject, preferType.transform);
        newRow.name = "Row_ResetTutorial";

        RectTransform newRowRT = newRow.GetComponent<RectTransform>();
        if (newRowRT != null)
        {
            newRowRT.anchoredPosition = new Vector2(50f, -485f);
        }

        TMP_Text labelText = newRow.transform.Find("MapText")?.GetComponent<TMP_Text>();
        if (labelText != null)
        {
            labelText.text = "新手教學";
            labelText.raycastTarget = false;
        }

        Button resetBtn = newRow.GetComponentInChildren<Button>(true);
        TMP_Text resetBtnText = resetBtn != null ? resetBtn.GetComponentInChildren<TMP_Text>(true) : null;

        if (resetBtnText != null)
        {
            resetBtnText.text = "重置新手教學";
            resetBtnText.raycastTarget = false;
        }

        if (resetBtn != null)
        {
            int persistentCount = resetBtn.onClick.GetPersistentEventCount();
            for (int i = persistentCount - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(resetBtn.onClick, i);
            }
            UnityEventTools.AddPersistentListener(resetBtn.onClick, settings.ResetTutorial);
        }

        settingsSO.FindProperty("resetTutorialButton").objectReferenceValue = resetBtn;
        settingsSO.FindProperty("resetTutorialButtonText").objectReferenceValue = resetBtnText;
        settingsSO.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
    }
    #endregion

    #region 輔助方法
    private static TMP_FontAsset LoadChineseFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font != null) return font;

        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.Contains("LiberationSans")) continue;
            TMP_FontAsset candidate = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(p);
            if (candidate != null) return candidate;
        }
        return null;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string content, float fontSize, Color color, TextAlignmentOptions alignment, Material customMaterial = null)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        if (customMaterial != null) tmp.fontSharedMaterial = customMaterial;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    private static void EnsureFolder(string assetFolderPath)
    {
        if (AssetDatabase.IsValidFolder(assetFolderPath)) return;

        string[] parts = assetFolderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
    #endregion
}
