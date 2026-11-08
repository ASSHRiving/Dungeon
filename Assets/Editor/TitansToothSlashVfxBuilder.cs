#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 大型雙手重武器【泰坦之牙 (TitansTooth)】專屬特殊刀光一鍵建置工具
/// 風格特點：暗沉厚重、融入豐富的土黃色 (Earthy Ochre) 遠古礦岩地質元素，無刺眼死白、無火紅橘光：
///   - 絕不添加武器常駐呼吸光、劍柄拖尾或模型動畫，100% 聚焦於揮砍瞬間的特殊刀光層次。
///   - 主刀光層 (Core Slash - Trail_Titans_Edge)：套用專屬材質 Mat_Slash_Crag.mat (Tex_Slash_Crag.png)，
///     呈現暗沉沉穩的土黃礦岩與泥岩深褐漸層，前緣帶柔和古樸的象牙砂黃撕裂刃邊，後緣呈 Voronoi 碎岩與鋸齒刷痕消散。
///   - 重擊殘影／衝擊波層 (Shockwave Sub-layer - Trail_Titans_Aura)：在主刀光外側疊加微透明、稍慢消散 (0.35s) 的黃土泥塵破空壓浪 (1.22m)。
///   - 能量撕裂亮邊脊核 (Energy Tear Ridge - Trail_Titans_Spine)：暗沉沉穩的土黃色折線地裂裂隙 (0.32m)。
///   - 刀鋒瞬間飛濺碎屑 (Burst Shards along Slash - PS_Titans_Shards)：套用 Tex_Particle_RockShard.png，
///     僅隨揮砍軌跡瞬間噴發少量具重力下墜感 (0.50G) 的土黃碎岩與深褐碎石，刀光結束即消失，絕不常駐。
/// </summary>
public static class TitansToothSlashVfxBuilder
{
    private const string PrefabPath = "Assets/Prefab/Weapon/TitansTooth.prefab";
    private const string VfxFolder = "Assets/VFX/TitansTooth";

    private const string ShockwaveName = "Trail_Titans_Aura";
    private const string CoreSlashName = "Trail_Titans_Edge";
    private const string SpineCrackName = "Trail_Titans_Spine";
    private const string BurstShardsName = "PS_Titans_Shards";

    private const string LegacyVfxName = "vfx";
    private const string LegacyTrailPointName = "TrailPoint";

    // 依據實測之泰坦之牙局部座標與刀身向量：
    private static readonly Vector3 BladeForward = new Vector3(-0.984871f, -0.152965f, 0.081430f);
    private static readonly Vector3 BladeUp = new Vector3(-0.080465f, -0.012497f, -0.996679f);

    // 關鍵掛載座標 (局部空間)：
    private static readonly Vector3 CoreSlashTipPos = new Vector3(-1.580f, -0.253f, 0.152f);
    private static readonly Vector3 ShockwaveOuterPos = new Vector3(-1.420f, -0.228f, 0.140f);
    private static readonly Vector3 SpineCrackPos = new Vector3(-1.380f, -0.222f, 0.136f);
    private static readonly Vector3 BladeMidPos = new Vector3(-1.050f, -0.171f, 0.108f);

    [MenuItem("Tools/VFX/TitansTooth/Build Slash VFX")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/VFX"))
            AssetDatabase.CreateFolder("Assets", "VFX");
        if (!AssetDatabase.IsValidFolder(VfxFolder))
            AssetDatabase.CreateFolder("Assets/VFX", "TitansTooth");

        // 1. 生成程序化貼圖（暗沉土黃地質配色）
        TitansToothTextureGenerator.GenerateAll();

        Texture2D texSlashCrag = AssetDatabase.LoadAssetAtPath<Texture2D>(TitansToothTextureGenerator.CragSlashPath);
        Texture2D texRockShard = AssetDatabase.LoadAssetAtPath<Texture2D>(TitansToothTextureGenerator.RockShardPath);
        Texture2D texShockwave = AssetDatabase.LoadAssetAtPath<Texture2D>(TitansToothTextureGenerator.ShockwavePath);
        Texture2D texSpine = AssetDatabase.LoadAssetAtPath<Texture2D>(TitansToothTextureGenerator.SpinePath);

        if (texSlashCrag == null || texRockShard == null || texShockwave == null || texSpine == null)
        {
            Debug.LogError("[TitansTooth VFX] 貼圖生成或載入失敗，請檢查 Assets/VFX/TitansTooth。");
            return;
        }

        // 2. 建立或更新 URP 專屬材質球（降低 HDR 倍率，抑制刺眼強光，融入土黃色系）
        Material matSlashCrag = EnsureMaterial(
            "Mat_Slash_Crag",
            texSlashCrag,
            new Color(1.05f, 0.98f, 0.82f, 1f),
            additive: false,
            queueOffset: 0);

        Material matShockwave = EnsureMaterial(
            "Mat_Slash_Shockwave",
            texShockwave,
            new Color(0.95f, 0.88f, 0.72f, 1f),
            additive: false,
            queueOffset: -1);

        Material matSpine = EnsureMaterial(
            "Mat_Slash_Spine",
            texSpine,
            new Color(1.25f, 1.15f, 0.85f, 1f),
            additive: true,
            queueOffset: 1);

        Material matRockShard = EnsureMaterial(
            "Mat_Particle_RockShard",
            texRockShard,
            new Color(1.05f, 0.96f, 0.78f, 1f),
            additive: false,
            queueOffset: 2);

        // 3. 掛載與對齊至 TitansTooth.prefab
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Weapon weapon = root.GetComponent<Weapon>();
            if (weapon == null)
            {
                Debug.LogError("[TitansTooth VFX] TitansTooth.prefab 根節點找不到 Weapon (Sword) 元件。");
                return;
            }

            DestroyChild(root.transform, LegacyVfxName);
            DestroyChild(root.transform, LegacyTrailPointName);
            DestroyChild(root.transform, ShockwaveName);
            DestroyChild(root.transform, CoreSlashName);
            DestroyChild(root.transform, SpineCrackName);
            DestroyChild(root.transform, BurstShardsName);

            SerializedObject soWeapon = new SerializedObject(weapon);
            SerializedProperty propGemLight = soWeapon.FindProperty("gemPointLight");
            if (propGemLight != null) propGemLight.objectReferenceValue = null;
            soWeapon.ApplyModifiedPropertiesWithoutUndo();

            // -------------------------------------------------------------------
            // Layer 1：重擊殘影／衝擊波層 (Shockwave Sub-layer - Trail_Titans_Aura)
            // 暗沉黃土塵霧風壓，微透明低調包覆（Alpha 0.40 -> 0）
            // -------------------------------------------------------------------
            TrailRenderer shockwaveTrail = CreateTrail(
                root.transform,
                ShockwaveName,
                ShockwaveOuterPos,
                matShockwave,
                time: 0.35f,
                width: 1.22f,
                widthCurve: new AnimationCurve(
                    new Keyframe(0.00f, 0.90f),
                    new Keyframe(0.22f, 1.00f),
                    new Keyframe(1.00f, 0.42f)),
                gradient: MakeGradient(
                    new[]
                    {
                        Key(0.62f, 0.52f, 0.36f, 0.00f), // 起刀土塵風壓
                        Key(0.40f, 0.33f, 0.24f, 0.45f), // 中段暗沉泥岩塵色
                        Key(0.18f, 0.15f, 0.12f, 1.00f)  // 尾端消散深土灰
                    },
                    new[]
                    {
                        Alpha(0.40f, 0.00f),
                        Alpha(0.25f, 0.50f),
                        Alpha(0.00f, 1.00f)
                    }));

            // -------------------------------------------------------------------
            // Layer 2：主刀光層 (Core Slash - Trail_Titans_Edge)
            // 沉穩象牙土黃刃邊 -> 厚重土黃礦岩 -> 暗沉泥岩深褐漸層
            // -------------------------------------------------------------------
            TrailRenderer coreSlashTrail = CreateTrail(
                root.transform,
                CoreSlashName,
                CoreSlashTipPos,
                matSlashCrag,
                time: 0.28f,
                width: 0.82f,
                widthCurve: new AnimationCurve(
                    new Keyframe(0.00f, 0.82f),
                    new Keyframe(0.20f, 1.00f),
                    new Keyframe(1.00f, 0.25f)),
                gradient: MakeGradient(
                    new[]
                    {
                        Key(0.86f, 0.78f, 0.58f, 0.00f), // 前緣：沉穩象牙砂黃亮邊（柔和古樸不刺眼）
                        Key(0.65f, 0.52f, 0.32f, 0.36f), // 中前段：厚重沉穩土黃色
                        Key(0.38f, 0.30f, 0.20f, 0.72f), // 中後段：暗沉泥褐岩色
                        Key(0.18f, 0.15f, 0.12f, 1.00f)  // 後緣：深沉岩灰土灰消散
                    },
                    new[]
                    {
                        Alpha(0.92f, 0.00f),
                        Alpha(0.85f, 0.52f),
                        Alpha(0.00f, 1.00f)
                    }));

            // -------------------------------------------------------------------
            // Layer 3：能量撕裂亮邊脊核 (Energy Tear Ridge - Trail_Titans_Spine)
            // 暗沉柔和的古老土黃地裂裂紋 (寬度 0.32m，滯留 0.20s)
            // -------------------------------------------------------------------
            TrailRenderer spineTrail = CreateTrail(
                root.transform,
                SpineCrackName,
                SpineCrackPos,
                matSpine,
                time: 0.20f,
                width: 0.32f,
                widthCurve: new AnimationCurve(
                    new Keyframe(0.00f, 1.00f),
                    new Keyframe(0.30f, 0.85f),
                    new Keyframe(1.00f, 0.18f)),
                gradient: MakeGradient(
                    new[]
                    {
                        Key(0.90f, 0.82f, 0.62f, 0.00f), // 象牙砂黃裂核
                        Key(0.72f, 0.58f, 0.36f, 0.45f), // 沉穩土黃礦脈
                        Key(0.35f, 0.28f, 0.18f, 1.00f)  // 暗褐收斂
                    },
                    new[]
                    {
                        Alpha(0.80f, 0.00f),
                        Alpha(0.65f, 0.50f),
                        Alpha(0.00f, 1.00f)
                    }));

            // -------------------------------------------------------------------
            // Layer 4：刀鋒瞬間飛濺碎屑 (Burst Shards along Slash - PS_Titans_Shards)
            // 噴發帶重力下墜感的菱形土黃碎岩與暗褐石屑
            // -------------------------------------------------------------------
            ParticleSystem burstShards = CreateBurstShardsParticles(root.transform, matRockShard);

            weapon.weaponTrails = new[] { shockwaveTrail, coreSlashTrail, spineTrail };
            weapon.weaponEffect = burstShards;
            EditorUtility.SetDirty(weapon);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("<color=#B89758>[TitansTooth VFX]</color> 泰坦之牙 (TitansTooth) 刀光已成功更新為暗沉沉穩、富含土黃色礦岩元素的專屬特效！");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ===================================================================
    #region Trail 建置

    private static TrailRenderer CreateTrail(
        Transform parent,
        string name,
        Vector3 localPos,
        Material mat,
        float time,
        float width,
        AnimationCurve widthCurve,
        Gradient gradient)
    {
        GameObject go = new GameObject(name);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.LookRotation(BladeForward, BladeUp);

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
        tr.emitting = false;
        return tr;
    }

    #endregion

    // ===================================================================
    #region 刀鋒瞬間飛濺碎屑粒子系統

    private static ParticleSystem CreateBurstShardsParticles(Transform parent, Material mat)
    {
        GameObject go = new GameObject(BurstShardsName);
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = BladeMidPos;
        go.transform.localRotation = Quaternion.LookRotation(BladeForward, BladeUp);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.45f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.40f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.85f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.040f, 0.095f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        // 雙色隨機：厚重土黃碎岩 (#A38A59) 與 暗沉泥褐玄武岩 (#4F4435)
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.62f, 0.52f, 0.35f, 1f),
            new Color(0.28f, 0.24f, 0.19f, 1f));
        main.gravityModifier = 0.50f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 120;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 10f;
        emission.rateOverDistance = 26f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.01f, 10, 15) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.08f, 0.08f, 1.45f);

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.40f;
        limit.dampen = 0.28f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.14f;
        noise.frequency = 1.4f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0.00f, 0.40f),
            new Keyframe(0.10f, 1.00f),
            new Keyframe(0.68f, 0.72f),
            new Keyframe(1.00f, 0.00f)));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 1.0f, Mathf.PI * 1.0f);

        // 生命週期色彩：淡砂黃高光 -> 厚重土黃碎岩 -> 暗沉泥褐石塊消散
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(MakeGradient(
            new[]
            {
                Key(0.82f, 0.72f, 0.52f, 0.00f), // 噴濺砂黃切面
                Key(0.58f, 0.48f, 0.32f, 0.28f), // 拋射中段厚重土黃色
                Key(0.35f, 0.28f, 0.19f, 0.68f), // 下墜深褐土石
                Key(0.18f, 0.15f, 0.12f, 1.00f)  // 消散暗泥岩灰
            },
            new[]
            {
                Alpha(0.00f, 0.00f),
                Alpha(0.90f, 0.06f),
                Alpha(0.80f, 0.62f),
                Alpha(0.00f, 1.00f)
            }));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = mat;
        renderer.maxParticleSize = 0.45f;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return ps;
    }

    #endregion

    // ===================================================================
    #region 材質與輔助工具

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
