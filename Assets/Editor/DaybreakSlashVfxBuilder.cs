#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 破曉 (Daybreak) 輕型單手劍刀光特效一鍵建置工具
/// 風格：破曉晨曦與輕盈疾風 ——
///   針對輕型單手劍定位，縮短滯留時間 (0.13s~0.19s)、收束刀光寬幅並強化尾端流線收尖，呈現俐落輕巧的揮砍手感；
///   同時將刀光完整包覆於攻擊判定球 (AttackPoint -0.452m + attackRadius 0.55m = 1.002m) 外緣，並維持高亮度與清晰度：
///   Layer 1：黑曜暮影風帶 (Alpha Blend 鏤空流線暗影風帶，呼應劍背黑曜護脊與護手紫紅寶石，襯托金光且不顯厚重)
///   Layer 2：破曉後掠羽刃 (Additive 流線後掠羽翼鋸齒與破風流線槽，還原劍刃金色鋸齒造型，包裹 1.00m 攻擊外緣)
///   Layer 3：破曉晨曦極光 (Additive 雙軌極細高光細芒與晨星節點，勾勒單手劍極速破空的銳利軌跡)
///   Layer 4：破曉羽芒星屑 (世界空間無重力微幅上揚的菱羽四芒星粒子，隨揮砍輕盈飄散)
/// </summary>
public static class DaybreakSlashVfxBuilder
{
    private const string PrefabPath = "Assets/Prefab/Weapon/Daybreak.prefab";
    private const string VfxFolder = "Assets/VFX/Daybreak";

    private const string VeilName = "Trail_Daybreak_Veil";
    private const string EdgeName = "Trail_Daybreak_Edge";
    private const string RayName = "Trail_Daybreak_Ray";
    private const string SparksName = "PS_Daybreak_Sparks";
    private const string LegacyTrailPointName = "TrailPoint";
    private const string LegacyVfxName = "vfx";

    // Daybreak 攻擊範圍半徑 attackRadius = 0.55m，center 在 AttackPoint (-0.452, 0.012, 0)
    // 劍身沿本地 -X 軸延伸，攻擊球體外緣達 0.452 + 0.55 = 1.002m
    // 外圈破曉羽刃配置於 x = -1.04m (寬 0.46m，涵蓋 0.81m~1.27m)，輕巧包覆 1.002m 的攻擊範圍外緣：
    private static readonly Vector3 BladeTipPos = new Vector3(-1.04f, 0.01f, 0f);  // 1.04m: 外圈破曉後掠羽刃
    private static readonly Vector3 BladeRayPos = new Vector3(-0.94f, 0.01f, 0f);  // 0.94m: 破曉晨曦極光細芒
    private static readonly Vector3 BladeVeilPos = new Vector3(-0.82f, 0.01f, 0f); // 0.82m: 輕盈黑曜暮影風帶
    private static readonly Vector3 BladeMidPos = new Vector3(-0.76f, 0.01f, 0f);  // 0.76m: 羽芒星屑粒子中心

    [MenuItem("Tools/VFX/Daybreak/Build Slash VFX")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(VfxFolder))
            AssetDatabase.CreateFolder("Assets/VFX", "Daybreak");

        // 生成破曉專屬的 4 張流線羽刃與晨曦貼圖
        DaybreakTextureGenerator.GenerateAll();
        Texture2D texVeil = AssetDatabase.LoadAssetAtPath<Texture2D>(DaybreakTextureGenerator.VeilPath);
        Texture2D texEdge = AssetDatabase.LoadAssetAtPath<Texture2D>(DaybreakTextureGenerator.EdgePath);
        Texture2D texRay = AssetDatabase.LoadAssetAtPath<Texture2D>(DaybreakTextureGenerator.RayPath);
        Texture2D texSpark = AssetDatabase.LoadAssetAtPath<Texture2D>(DaybreakTextureGenerator.SparkPath);
        if (texVeil == null || texEdge == null || texRay == null || texSpark == null)
        {
            Debug.LogError("[Daybreak VFX] 破曉刀光貼圖生成失敗，請檢查 Assets/VFX/Daybreak。");
            return;
        }

        // ---------- 材質 ----------
        Material matVeil = EnsureMaterial("Mat_VFX_Daybreak_DuskVeil", texVeil, new Color(1.35f, 1.25f, 1.45f, 1f), additive: false, queueOffset: -1);
        Material matEdge = EnsureMaterial("Mat_VFX_Daybreak_FeatherEdge", texEdge, new Color(2.10f, 1.85f, 0.92f, 1f), additive: true, queueOffset: 0);
        Material matRay = EnsureMaterial("Mat_VFX_Daybreak_SolarRay", texRay, new Color(2.40f, 2.15f, 1.35f, 1f), additive: true, queueOffset: 1);
        Material matSpark = EnsureMaterial("Mat_VFX_Daybreak_DawnSpark", texSpark, new Color(2.10f, 1.90f, 1.25f, 1f), additive: true, queueOffset: 2);

        // ---------- 修改 Prefab ----------
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Weapon weapon = root.GetComponent<Weapon>();
            if (weapon == null)
            {
                Debug.LogError("[Daybreak VFX] Prefab 根物件上找不到 Weapon 元件。");
                return;
            }

            // 清除舊的測試版 vfx 與先前生成的物件（支援重複執行）
            DestroyChild(root.transform, LegacyTrailPointName);
            DestroyChild(root.transform, LegacyVfxName);
            DestroyChild(root.transform, VeilName);
            DestroyChild(root.transform, EdgeName);
            DestroyChild(root.transform, RayName);
            DestroyChild(root.transform, SparksName);

            // Layer 1：黑曜暮影風帶（time 0.19s, width 0.54m，流線鏤空風帶，黑曜暗灰漸變至破曉紫紅）
            TrailRenderer veil = CreateTrail(root.transform, VeilName, BladeVeilPos, matVeil,
                time: 0.19f, width: 0.54f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.18f)),
                gradient: MakeGradient(
                    new[] { Key(0.16f, 0.17f, 0.22f, 0f), Key(0.28f, 0.20f, 0.32f, 0.48f), Key(0.48f, 0.22f, 0.40f, 1f) },
                    new[] { Alpha(1.0f, 0f), Alpha(0.90f, 0.45f), Alpha(0f, 1f) }));

            // Layer 2：破曉後掠羽刃（位於 1.04m，time 0.16s, width 0.46m，輕盈包覆 1.002m 攻擊範圍外緣）
            TrailRenderer edge = CreateTrail(root.transform, EdgeName, BladeTipPos, matEdge,
                time: 0.16f, width: 0.46f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 0.88f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0.12f)),
                gradient: MakeGradient(
                    new[] { Key(1.00f, 0.96f, 0.68f, 0f), Key(0.92f, 0.79f, 0.24f, 0.42f), Key(0.76f, 0.52f, 0.22f, 1f) },
                    new[] { Alpha(1.0f, 0f), Alpha(0.95f, 0.50f), Alpha(0f, 1f) }));

            // Layer 3：破曉晨曦極光（位於 0.94m，time 0.13s, width 0.22m，極速銳利的晨曦金白閃光細線）
            TrailRenderer ray = CreateTrail(root.transform, RayName, BladeRayPos, matRay,
                time: 0.13f, width: 0.22f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.25f, 0.82f), new Keyframe(1f, 0.08f)),
                gradient: MakeGradient(
                    new[] { Key(1.00f, 0.99f, 0.86f, 0f), Key(1.00f, 0.86f, 0.35f, 0.45f), Key(0.92f, 0.42f, 0.68f, 1f) },
                    new[] { Alpha(1.0f, 0f), Alpha(0.95f, 0.50f), Alpha(0f, 1f) }));

            // Layer 4：破曉羽芒星屑（輕盈無下墜感、微幅隨風上揚的晨星與紫紅寶石星屑）
            ParticleSystem sparks = CreateSparkParticles(root.transform, matSpark);

            weapon.weaponTrails = new[] { veil, edge, ray };
            weapon.weaponEffect = sparks;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("<color=#FFE066>[Daybreak VFX]</color> 破曉 (Daybreak) 輕型單手劍刀光已建置完成：3 層疾風晨曦 Trail + 破曉羽芒粒子，並已綁定至 Sword.weaponTrails / weaponEffect。");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ===================================================================
    #region Trail

    private static TrailRenderer CreateTrail(Transform parent, string name, Vector3 localPos, Material mat,
        float time, float width, AnimationCurve widthCurve, Gradient gradient)
    {
        GameObject go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.sharedMaterial = mat;
        tr.time = time;
        tr.widthMultiplier = width;
        tr.widthCurve = widthCurve;
        tr.colorGradient = gradient;
        tr.minVertexDistance = 0.012f;
        tr.numCornerVertices = 4;
        tr.numCapVertices = 2;
        tr.alignment = LineAlignment.View;
        tr.textureMode = LineTextureMode.Stretch;
        tr.shadowCastingMode = ShadowCastingMode.Off;
        tr.receiveShadows = false;
        tr.lightProbeUsage = LightProbeUsage.Off;
        tr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        tr.generateLightingData = false;
        tr.autodestruct = false;
        tr.emitting = false; // 由 CharacterCombatBase 在攻擊判定開啟時打開
        return tr;
    }

    #endregion

    // ===================================================================
    #region Particles

    private static ParticleSystem CreateSparkParticles(Transform parent, Material mat)
    {
        GameObject go = new GameObject(SparksName);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = BladeMidPos;
        // 讓發射盒的 Z 軸對齊 Daybreak 劍身延伸方向 (-1, 0, 0)
        go.transform.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.45f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f); // 短促俐落，不拖泥帶水
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.058f);  // 輕巧精緻的羽芒尺寸
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        // 雙色隨機：破曉晨光金 (#FFE661) 與 護手晨星紫紅 (#FF7AC6)
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1.0f, 0.90f, 0.38f, 1f),
            new Color(1.0f, 0.48f, 0.78f, 1f));
        main.gravityModifier = -0.08f; // 微幅向上飄升，營造輕盈無重力的晨曦微風感
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 18f;
        emission.rateOverDistance = 36f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.01f, 8, 12) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.06f, 0.06f, 0.85f); // 涵蓋 0.33m~1.18m 輕型單手劍揮砍範圍

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.22f;
        noise.frequency = 2.4f;
        noise.scrollSpeed = 0.85f;
        noise.damping = true;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.18f;
        limit.dampen = 0.32f; // 高速噴出後迅速在空氣中懸停閃爍

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.25f), new Keyframe(0.12f, 1f), new Keyframe(0.60f, 0.65f), new Keyframe(1f, 0f)));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 1.5f, Mathf.PI * 1.5f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGradient(
            new[] { Key(1f, 0.98f, 0.82f, 0f), Key(0.96f, 0.84f, 0.32f, 0.35f), Key(0.88f, 0.40f, 0.68f, 0.78f), Key(0.55f, 0.22f, 0.45f, 1f) },
            new[] { Alpha(0f, 0f), Alpha(1f, 0.08f), Alpha(0.90f, 0.58f), Alpha(0f, 1f) }));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = mat;
        renderer.maxParticleSize = 0.35f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return ps;
    }

    #endregion

    // ===================================================================
    #region Helpers

    private static Material EnsureMaterial(string name, Texture2D tex, Color baseColor, bool additive, int queueOffset)
    {
        string path = $"{VfxFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

        // URP Particles/Unlit：Transparent + (Additive / Alpha) + 雙面
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", additive ? 2f : 0f);
        mat.SetFloat("_Cull", 0f);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_QueueOffset", queueOffset);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent + queueOffset;

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void DestroyChild(Transform parent, string childName)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform c = parent.GetChild(i);
            if (c.name == childName) Object.DestroyImmediate(c.gameObject);
        }
    }

    private static GradientColorKey Key(float r, float g, float b, float t) => new GradientColorKey(new Color(r, g, b), t);
    private static GradientAlphaKey Alpha(float a, float t) => new GradientAlphaKey(a, t);

    private static Gradient MakeGradient(GradientColorKey[] colors, GradientAlphaKey[] alphas)
    {
        var g = new Gradient();
        g.SetKeys(colors, alphas);
        return g;
    }

    #endregion
}
#endif
