#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 噬星者 (StarEater) 刀光特效一鍵建置工具
/// 風格：深淵虛空 —— 暗紫黑色的「吞噬陰影」本體 + 亮紫色虛空刃緣 + 白紫星芒核心 + 被吞噬的星屑粒子
/// 架構參考 RoyalSword（多層 TrailRenderer + 世界空間火花粒子），可重複執行（會先清除舊的生成物件）。
/// </summary>
public static class StarEaterSlashVfxBuilder
{
    private const string PrefabPath = "Assets/Prefab/Weapon/StarEater.prefab";
    private const string VfxFolder = "Assets/VFX/StarEater";

    private const string ShadowName = "Trail_Abyss_Shadow";
    private const string EdgeName = "Trail_Void_Edge";
    private const string CoreName = "Trail_Star_Core";
    private const string SparkName = "PS_Devoured_Stars";
    private const string LegacyVfxName = "vfx"; // 舊的測試版刀光物件

    // 噬星者劍身沿 -X 延伸：護手約 x≈-0.12，劍尖約 x≈-0.95
    private const float BladeTipX = -0.90f;
    private const float BladeMidX = -0.55f;

    [MenuItem("Tools/VFX/StarEater/Build Slash VFX")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(VfxFolder))
            AssetDatabase.CreateFolder("Assets/VFX", "StarEater");

        // 噬星者專屬鋸齒貼圖（由 StarEaterTextureGenerator 程序化生成，與 RoyalSword 的平滑聖光區隔）
        StarEaterTextureGenerator.GenerateAll();
        Texture2D texShadow = AssetDatabase.LoadAssetAtPath<Texture2D>(StarEaterTextureGenerator.ShadowPath);
        Texture2D texEdge = AssetDatabase.LoadAssetAtPath<Texture2D>(StarEaterTextureGenerator.EdgePath);
        Texture2D texCore = AssetDatabase.LoadAssetAtPath<Texture2D>(StarEaterTextureGenerator.CorePath);
        Texture2D texStar = AssetDatabase.LoadAssetAtPath<Texture2D>(StarEaterTextureGenerator.ShardPath);
        if (texShadow == null || texEdge == null || texCore == null || texStar == null)
        {
            Debug.LogError("[StarEater VFX] 噬星者刀光貼圖生成失敗，請檢查 Assets/VFX/StarEater。");
            return;
        }

        // ---------- 材質 ----------
        // 陰影本體用 Alpha Blend（真正的「暗」，不會被 Additive 吃掉），其餘用 Additive 發光
        // 調低 HDR 強度，消除像「烈火」般過曝的高熱白光，轉向深邃虛空冷紫、暗蝕氣息
        Material matShadow = EnsureMaterial("Mat_VFX_StarEater_AbyssShadow", texShadow, new Color(1f, 1f, 1f, 1f), additive: false, queueOffset: -1);
        Material matEdge = EnsureMaterial("Mat_VFX_StarEater_VoidEdge", texEdge, new Color(1.3f, 0.7f, 1.8f, 1f), additive: true, queueOffset: 0);
        Material matCore = EnsureMaterial("Mat_VFX_StarEater_StarCore", texCore, new Color(1.2f, 0.9f, 1.6f, 1f), additive: true, queueOffset: 1);
        Material matSpark = EnsureMaterial("Mat_VFX_StarEater_StarSpark", texStar, new Color(1.5f, 1.0f, 2.0f, 1f), additive: true, queueOffset: 2);

        // ---------- 修改 Prefab ----------
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Weapon weapon = root.GetComponent<Weapon>();
            if (weapon == null)
            {
                Debug.LogError("[StarEater VFX] Prefab 根物件上找不到 Weapon 元件。");
                return;
            }

            // 清除舊的測試版 vfx 與先前生成的物件（可重複執行）
            DestroyChild(root.transform, LegacyVfxName);
            DestroyChild(root.transform, ShadowName);
            DestroyChild(root.transform, EdgeName);
            DestroyChild(root.transform, CoreName);
            DestroyChild(root.transform, SparkName);

            // Layer 1：深淵陰影本體（寬、暗紫黑、Alpha Blend）—— 加強吞噬暗色，構成刀光的主體深度
            TrailRenderer shadow = CreateTrail(root.transform, ShadowName, new Vector3(-0.64f, 0f, 0f), matShadow,
                time: 0.32f, width: 0.70f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 0.9f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.45f)),
                gradient: MakeGradient(
                    new[] { Key(0.08f, 0.00f, 0.16f, 0f), Key(0.14f, 0.02f, 0.26f, 0.45f), Key(0.03f, 0.00f, 0.06f, 1f) },
                    new[] { Alpha(0.98f, 0f), Alpha(0.85f, 0.45f), Alpha(0f, 1f) }));

            // Layer 2：虛空鋸齒刃緣（鯊鰭倒刺貼圖、深邃冷紫、Additive）—— 鋸齒輪廓發出幽暗紫光，取代刺眼強光
            TrailRenderer edge = CreateTrail(root.transform, EdgeName, new Vector3(BladeTipX, 0f, 0f), matEdge,
                time: 0.26f, width: 0.54f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 0.75f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0.25f)),
                gradient: MakeGradient(
                    new[] { Key(0.70f, 0.35f, 0.95f, 0f), Key(0.45f, 0.10f, 0.75f, 0.30f), Key(0.20f, 0.02f, 0.45f, 1f) },
                    new[] { Alpha(0.9f, 0f), Alpha(0.65f, 0.35f), Alpha(0f, 1f) }));

            // Layer 3：星裂核心（折線裂痕貼圖、虛空暗芒）—— 降低過曝白色，改為纖細冷紫電隙，不再像燃燒火焰
            TrailRenderer core = CreateTrail(root.transform, CoreName, new Vector3(-0.78f, 0f, 0f), matCore,
                time: 0.16f, width: 0.18f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f)),
                gradient: MakeGradient(
                    new[] { Key(0.85f, 0.65f, 1.00f, 0f), Key(0.60f, 0.25f, 0.90f, 0.40f), Key(0.30f, 0.05f, 0.60f, 1f) },
                    new[] { Alpha(0.8f, 0f), Alpha(0.5f, 0.4f), Alpha(0f, 1f) }));

            // Layer 4：被吞噬的星屑（世界空間，沿劍身灑落後縮小消失）
            ParticleSystem sparks = CreateSparks(root.transform, matSpark);

            weapon.weaponTrails = new[] { shadow, edge, core };
            weapon.weaponEffect = sparks;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("<color=#B266FF>[StarEater VFX]</color> 噬星者刀光已重建完成：3 層 Trail + 星屑粒子，並已綁定到 Sword.weaponTrails / weaponEffect。");
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
        tr.minVertexDistance = 0.015f;
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

    private static ParticleSystem CreateSparks(Transform parent, Material mat)
    {
        GameObject go = new GameObject(SparkName);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(BladeMidX - 0.08f, 0f, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // 讓 Shape 的 Z 軸對齊劍身 (X 軸)

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 1f, 1f), new Color(0.65f, 0.35f, 1f, 1f));
        main.gravityModifier = -0.03f; // 微微上飄，像被吸走的星塵
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 25f;
        emission.rateOverDistance = 45f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 8, 14) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.03f, 0.03f, 0.70f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 1.6f;
        noise.scrollSpeed = 0.6f;
        noise.damping = true;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.6f;
        limit.dampen = 0.25f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f), new Keyframe(0.12f, 1f), new Keyframe(0.55f, 0.7f), new Keyframe(1f, 0f)));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGradient(
            new[] { Key(1f, 1f, 1f, 0f), Key(0.95f, 0.75f, 1f, 0.15f), Key(0.6f, 0.2f, 1f, 0.55f), Key(0.3f, 0.05f, 0.6f, 1f) },
            new[] { Alpha(0f, 0f), Alpha(1f, 0.1f), Alpha(0.8f, 0.55f), Alpha(0f, 1f) }));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = mat;
        renderer.maxParticleSize = 0.5f;
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
