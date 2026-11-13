#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 岩峰 (Crag) 巨劍刀光特效一鍵建置工具
/// 風格：玄武斷層與金珀礦脈 ——
///   Layer 1：玄武暗岩殘影 (Alpha Blend 暗褐炭黑岩層本體，賦予巨劍沉重質量感，刀尾呈 Voronoi 碎塊崩解)
///   Layer 2：岩峰崩口刃緣 (Additive 梯形崩口石齒，砂金過渡至冷灰岩色，呼應劍刃下緣的崩裂石齒)
///   Layer 3：金珀礦脈核心 (Additive 階梯狀黃褐色折線礦脈，直接還原劍身中央鑲嵌的金色岩脈)
///   Layer 4：崩裂岩晶碎塊 (世界空間低多邊形立體切面礦石粒子，隨揮砍噴濺並受重力下墜)
/// </summary>
public static class CragSlashVfxBuilder
{
    private const string PrefabPath = "Assets/Prefab/Weapon/Crag.prefab";
    private const string VfxFolder = "Assets/VFX/Crag";

    private const string SlabName = "Trail_Basalt_Slab";
    private const string EdgeName = "Trail_Crag_Edge";
    private const string VeinName = "Trail_Ochre_Vein";
    private const string DebrisName = "PS_Crag_Debris";
    private const string LegacyTrailPointName = "TrailPoint"; // 舊的測試版刀光物件
    private const string LegacyVfxName = "vfx";

    // Crag 巨劍攻擊範圍半徑 attackRadius = 0.75m，center 在 AttackPoint (-0.858, -0.091, 0.131)
    // 劍身方向單位向量：(-0.9798, -0.1564, 0.1246)
    // 攻擊球體外緣最遠達 distance ≈ 1.623m，為將刀光特效完整包裹在攻擊範圍外側，
    // 外圈石齒刃緣配置於 1.70m (包覆 1.62m 的 Hitbox 球緣)，內側礦脈與厚重岩層相應外移並拓寬：
    private static readonly Vector3 BladeTipPos = new Vector3(-1.666f, -0.266f, 0.212f);  // 1.70m: 外圈包裹石齒刃緣
    private static readonly Vector3 BladeVeinPos = new Vector3(-1.450f, -0.231f, 0.184f); // 1.48m: 金珀礦脈核心
    private static readonly Vector3 BladeSlabPos = new Vector3(-1.254f, -0.200f, 0.159f); // 1.28m: 厚重玄武岩層殘影
    private static readonly Vector3 BladeMidPos = new Vector3(-1.127f, -0.180f, 0.143f);  // 1.15m: 碎岩粒子中心

    [MenuItem("Tools/VFX/Crag/Build Slash VFX")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(VfxFolder))
            AssetDatabase.CreateFolder("Assets/VFX", "Crag");

        // 生成岩峰專屬的 4 張岩石與礦脈程序化貼圖
        CragTextureGenerator.GenerateAll();
        Texture2D texSlab = AssetDatabase.LoadAssetAtPath<Texture2D>(CragTextureGenerator.SlabPath);
        Texture2D texEdge = AssetDatabase.LoadAssetAtPath<Texture2D>(CragTextureGenerator.EdgePath);
        Texture2D texVein = AssetDatabase.LoadAssetAtPath<Texture2D>(CragTextureGenerator.VeinPath);
        Texture2D texChunk = AssetDatabase.LoadAssetAtPath<Texture2D>(CragTextureGenerator.ChunkPath);
        if (texSlab == null || texEdge == null || texVein == null || texChunk == null)
        {
            Debug.LogError("[Crag VFX] 岩峰刀光貼圖生成失敗，請檢查 Assets/VFX/Crag。");
            return;
        }

        // ---------- 材質 ----------
        // 大幅提升 HDR 亮度與不透明度，徹底解決刀光看不太清的問題
        Material matSlab = EnsureMaterial("Mat_VFX_Crag_BasaltSlab", texSlab, new Color(1.35f, 1.20f, 1.05f, 1f), additive: false, queueOffset: -1);
        Material matEdge = EnsureMaterial("Mat_VFX_Crag_FractureEdge", texEdge, new Color(2.0f, 1.68f, 0.95f, 1f), additive: true, queueOffset: 0);
        Material matVein = EnsureMaterial("Mat_VFX_Crag_OchreVein", texVein, new Color(2.2f, 1.85f, 0.85f, 1f), additive: true, queueOffset: 1);
        Material matDebris = EnsureMaterial("Mat_VFX_Crag_RockChunk", texChunk, new Color(1.8f, 1.50f, 0.85f, 1f), additive: true, queueOffset: 2);

        // ---------- 修改 Prefab ----------
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Weapon weapon = root.GetComponent<Weapon>();
            if (weapon == null)
            {
                Debug.LogError("[Crag VFX] Prefab 根物件上找不到 Weapon 元件。");
                return;
            }

            // 清除舊的測試版 TrailPoint 與先前生成的物件（支援重複執行）
            DestroyChild(root.transform, LegacyTrailPointName);
            DestroyChild(root.transform, LegacyVfxName);
            DestroyChild(root.transform, SlabName);
            DestroyChild(root.transform, EdgeName);
            DestroyChild(root.transform, VeinName);
            DestroyChild(root.transform, DebrisName);

            // Layer 1：玄武岩層殘影（寬度 1.30m 拓寬鋪底，厚重暖岩色，覆蓋 0.63m~1.93m 攻擊全域）
            TrailRenderer slab = CreateTrail(root.transform, SlabName, BladeSlabPos, matSlab,
                time: 0.38f, width: 1.30f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 0.92f), new Keyframe(0.20f, 1f), new Keyframe(1f, 0.55f)),
                gradient: MakeGradient(
                    new[] { Key(0.42f, 0.33f, 0.24f, 0f), Key(0.52f, 0.40f, 0.28f, 0.45f), Key(0.24f, 0.18f, 0.12f, 1f) },
                    new[] { Alpha(1.0f, 0f), Alpha(0.95f, 0.50f), Alpha(0f, 1f) }));

            // Layer 2：岩峰崩口刃緣（位於 1.70m，寬度 0.92m，完整包覆 1.62m 的 Hitbox 球形攻擊範圍）
            TrailRenderer edge = CreateTrail(root.transform, EdgeName, BladeTipPos, matEdge,
                time: 0.32f, width: 0.92f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.20f, 1f), new Keyframe(1f, 0.35f)),
                gradient: MakeGradient(
                    new[] { Key(1.00f, 0.92f, 0.55f, 0f), Key(0.95f, 0.78f, 0.32f, 0.40f), Key(0.55f, 0.45f, 0.28f, 1f) },
                    new[] { Alpha(1.0f, 0f), Alpha(0.95f, 0.55f), Alpha(0f, 1f) }));

            // Layer 3：金珀礦脈核心（位於 1.48m，寬度 0.40m，耀眼金珀折線光澤）
            TrailRenderer vein = CreateTrail(root.transform, VeinName, BladeVeinPos, matVein,
                time: 0.24f, width: 0.40f,
                widthCurve: new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.35f, 0.88f), new Keyframe(1f, 0.22f)),
                gradient: MakeGradient(
                    new[] { Key(1.00f, 0.98f, 0.75f, 0f), Key(1.00f, 0.82f, 0.25f, 0.45f), Key(0.70f, 0.45f, 0.10f, 1f) },
                    new[] { Alpha(1.0f, 0f), Alpha(0.95f, 0.55f), Alpha(0f, 1f) }));

            // Layer 4：崩裂岩晶碎塊（沿 1.45m 長發射盒迸發，覆蓋巨劍揮砍全路徑並帶重力下墜）
            ParticleSystem debris = CreateDebrisParticles(root.transform, matDebris);

            weapon.weaponTrails = new[] { slab, edge, vein };
            weapon.weaponEffect = debris;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("<color=#D4AF37>[Crag VFX]</color> 岩峰 (Crag) 巨劍刀光已建置完成：3 層岩脈 Trail + 崩裂岩晶粒子，並已綁定至 Sword.weaponTrails / weaponEffect。");
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

    private static ParticleSystem CreateDebrisParticles(Transform parent, Material mat)
    {
        GameObject go = new GameObject(DebrisName);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = BladeMidPos;
        // 讓發射盒的 Z 軸精確對齊 Crag 巨劍劍身走向 (-1.428, -0.247, 0.192)
        go.transform.localRotation = Quaternion.LookRotation(BladeTipPos.normalized, Vector3.up);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.52f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.20f, 0.95f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.11f); // 塊狀立體礦石碎塊尺寸
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        // 雙色隨機：耀眼金珀色 (#FFDF66) 與 砂岩暖金色 (#CBB282)
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1.0f, 0.88f, 0.42f, 1f),
            new Color(0.78f, 0.70f, 0.55f, 1f));
        main.gravityModifier = 0.65f; // 具碎石重量感，噴出後微幅下墜
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 22f;
        emission.rateOverDistance = 42f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 12, 18) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.10f, 0.12f, 1.45f); // 覆蓋 0.42m~1.88m 揮砍路徑全域

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.14f;
        noise.frequency = 1.4f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.50f;
        limit.dampen = 0.28f;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.35f), new Keyframe(0.10f, 1f), new Keyframe(0.65f, 0.75f), new Keyframe(1f, 0f)));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 1.2f, Mathf.PI * 1.2f); // 碎石翻滾旋轉

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGradient(
            new[] { Key(1f, 0.95f, 0.65f, 0f), Key(0.95f, 0.78f, 0.28f, 0.25f), Key(0.58f, 0.50f, 0.40f, 0.70f), Key(0.28f, 0.24f, 0.20f, 1f) },
            new[] { Alpha(0f, 0f), Alpha(1f, 0.06f), Alpha(0.95f, 0.65f), Alpha(0f, 1f) }));

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
