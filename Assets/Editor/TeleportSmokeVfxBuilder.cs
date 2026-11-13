#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TeleportSmokeVfxBuilder
{
    private const string FolderPath = "Assets/VFX/TeleportSmoke";

    [MenuItem("Tools/VFX/一鍵生成瞬移黑紫煙霧特效組 (Vanish & Appear)")]
    public static void BuildTeleportSmokePrefabs()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }

        // 1. 自動生成 3 張零邊界程序化貼圖 (.png)
        Texture2D smokeTex = CreateSmokeTexture($"{FolderPath}/NecroSmoke_Erosion.png");
        Texture2D slitTex  = CreateSlitTexture($"{FolderPath}/VoidSlit_Flare.png");
        Texture2D ringTex  = CreateRingTexture($"{FolderPath}/ShadowRing_Wave.png");

        // 2. 建立「深淵黑煙 (Alpha Blended)」與「死靈紫芒 (Additive)」材質球
        Shader smokeShader = Shader.Find("Custom/URP_NecroSmokeDissolve");
        Shader urpParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpParticleShader == null) urpParticleShader = Shader.Find("Particles/Standard Unlit");

        Material darkSmokeMat = CreateParticleMaterial(
            $"{FolderPath}/Mat_AbyssSmoke_Alpha.mat",
            smokeShader != null ? smokeShader : urpParticleShader,
            smokeTex,
            new Color(0.11f, 0.03f, 0.18f, 0.94f),
            isAdditive: false
        );

        Material purpleGlowMat = CreateParticleMaterial(
            $"{FolderPath}/Mat_NecroGlow_Additive.mat",
            urpParticleShader,
            smokeTex,
            new Color(1.6f, 0.35f, 2.8f, 0.85f),
            isAdditive: true
        );

        Material slitFlashMat = CreateParticleMaterial(
            $"{FolderPath}/Mat_VoidSlit_Additive.mat",
            urpParticleShader,
            slitTex,
            new Color(2.2f, 0.6f, 3.5f, 1.0f),
            isAdditive: true
        );

        Material groundRingMat = CreateParticleMaterial(
            $"{FolderPath}/Mat_ShadowRing_Additive.mat",
            urpParticleShader,
            ringTex,
            new Color(1.5f, 0.25f, 2.6f, 0.9f),
            isAdditive: true
        );

        // 3. 分別建立 Vanish (向內坍縮消失) 與 Appear (向外爆發出現) 兩顆 Prefab
        GameObject vanishPrefab = BuildSingleTeleportPrefab("Teleport_Vanish_VFX", isVanish: true, darkSmokeMat, purpleGlowMat, slitFlashMat, groundRingMat);
        GameObject appearPrefab = BuildSingleTeleportPrefab("Teleport_Appear_VFX", isVanish: false, darkSmokeMat, purpleGlowMat, slitFlashMat, groundRingMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = vanishPrefab;

        Debug.Log($"[TeleportSmokeVfxBuilder] 成功生成瞬移特效組！請至 {FolderPath} 將 Teleport_Vanish_VFX 與 Teleport_Appear_VFX 拖入技能 SO。");
    }

    private static GameObject BuildSingleTeleportPrefab(
        string prefabName,
        bool isVanish,
        Material darkSmokeMat,
        Material purpleGlowMat,
        Material slitFlashMat,
        Material groundRingMat)
    {
        GameObject root = new GameObject(prefabName);
        TeleportSmokeVfxController ctrl = root.AddComponent<TeleportSmokeVfxController>();

        // --- Layer 1A: Abyss_Dark_Smoke (深淵黑紫濃煙主體 - Alpha Blended) ---
        GameObject darkSmokeObj = new GameObject("Abyss_Dark_Smoke");
        darkSmokeObj.transform.SetParent(root.transform, false);
        ParticleSystem darkPS = darkSmokeObj.AddComponent<ParticleSystem>();
        ConfigureSmokeBillowPS(darkPS, darkSmokeMat, isVanish, isCoreDark: true);

        // --- Layer 1B: Necro_Rim_Mist (外圍死靈紫霧高光 - Additive) ---
        GameObject rimSmokeObj = new GameObject("Necro_Rim_Mist");
        rimSmokeObj.transform.SetParent(root.transform, false);
        ParticleSystem rimPS = rimSmokeObj.AddComponent<ParticleSystem>();
        ConfigureSmokeBillowPS(rimPS, purpleGlowMat, isVanish, isCoreDark: false);

        // --- Layer 2: Void_Slit_Flash (空間裂隙垂直坍縮閃光) ---
        GameObject slitObj = new GameObject("Void_Slit_Flash");
        slitObj.transform.SetParent(root.transform, false);
        ParticleSystem slitPS = slitObj.AddComponent<ParticleSystem>();
        ConfigureVoidSlitPS(slitPS, slitFlashMat, isVanish);

        // --- Layer 3: Warp_Soul_Streaks (向心吸入 / 向外噴射流光微粒) ---
        GameObject streaksObj = new GameObject("Warp_Soul_Streaks");
        streaksObj.transform.SetParent(root.transform, false);
        ParticleSystem streaksPS = streaksObj.AddComponent<ParticleSystem>();
        ConfigureSoulStreaksPS(streaksPS, slitFlashMat, isVanish);

        // --- Layer 4: Ground_Shadow_Ring (向下偏移 -0.88m 貼齊石磚地板的暗影環) ---
        GameObject ringObj = new GameObject("Ground_Shadow_Ring");
        ringObj.transform.SetParent(root.transform, false);
        ringObj.transform.localPosition = new Vector3(0f, -0.88f, 0f); // 補償 TeleportEscapeAbility 的 +0.9f 胸口高度
        ParticleSystem ringPS = ringObj.AddComponent<ParticleSystem>();
        ConfigureGroundRingPS(ringPS, groundRingMat, isVanish);

        // --- 動態紫光點光源閃爍 ---
        GameObject lightObj = new GameObject("Teleport_FlashLight");
        lightObj.transform.SetParent(root.transform, false);
        Light ptLight = lightObj.AddComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.color = new Color(0.75f, 0.2f, 1.0f);
        ptLight.range = 5.5f;
        ptLight.intensity = 4.5f;
        ptLight.shadows = LightShadows.None;
        ctrl.flashLight = ptLight;

        string assetPath = $"{FolderPath}/{prefabName}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);
        return savedPrefab;
    }

    private static void ConfigureSmokeBillowPS(ParticleSystem ps, Material mat, bool isVanish, bool isCoreDark)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 1.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
        main.startSpeed = isVanish ? new ParticleSystem.MinMaxCurve(0.1f, 0.3f) : new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
        main.startSize = isCoreDark ? new ParticleSystem.MinMaxCurve(1.1f, 1.65f) : new ParticleSystem.MinMaxCurve(0.85f, 1.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = isCoreDark ? new Color(0.12f, 0.04f, 0.18f, 0.95f) : new Color(0.78f, 0.28f, 1.0f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, isCoreDark ? (short)18 : (short)10) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = isVanish ? 0.95f : 0.25f; // 消失時從全身外圍往中心吸；現身時從中心往外炸

        var velOL = ps.velocityOverLifetime;
        velOL.enabled = true;
        velOL.space = ParticleSystemSimulationSpace.Local;
        velOL.radial = new ParticleSystem.MinMaxCurve(isVanish ? -2.8f : 0.4f); // 消失時強烈向心坍縮
        velOL.y = new ParticleSystem.MinMaxCurve(isVanish ? 0.3f : 0.8f);

        // 現身爆發時加上空氣阻力急停，營造煙霧炸開後懸浮消散的厚重感
        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = !isVanish;
        limitVel.limit = 0.6f;
        limitVel.dampen = 0.25f;

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, isVanish
            ? new AnimationCurve(new Keyframe(0f, 1.15f), new Keyframe(0.6f, 0.85f), new Keyframe(1f, 0.05f))
            : new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.35f, 1.1f), new Keyframe(1f, 1.35f)));

        var colOL = ps.colorOverLifetime;
        colOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.2f, 0.9f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.18f), new GradientAlphaKey(0.85f, 0.55f), new GradientAlphaKey(0f, 1f) }
        );
        colOL.color = grad;

        var rotOL = ps.rotationOverLifetime;
        rotOL.enabled = true;
        rotOL.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

        // 啟用 Custom1.x 頂點流傳遞溶解進度給 URP_NecroSmokeDissolve Shader
        var customData = ps.customData;
        customData.enabled = true;
        customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
        customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1);
        customData.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.35f, 0.15f),
            new Keyframe(1f, 1f)
        )));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = isCoreDark ? 0 : 1;
        List<ParticleSystemVertexStream> streams = new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.Custom1X
        };
        renderer.SetActiveVertexStreams(streams);
    }

    private static void ConfigureVoidSlitPS(ParticleSystem ps, Material mat, bool isVanish)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.5f;
        main.startDelay = isVanish ? 0.12f : 0f; // 消失時在濃煙縮到中心的那一刻閃光；現身時第一幀閃光
        main.startLifetime = 0.28f;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = 0.9f;
        main.startSizeY = 2.6f; // 垂直拉長包覆骷髏法師全身高度
        main.startSizeZ = 1.0f;
        main.startColor = new Color(0.85f, 0.5f, 1.0f, 1.0f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });

        var shape = ps.shape;
        shape.enabled = false;

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.separateAxes = true;
        sizeOL.x = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1.2f), new Keyframe(0.5f, 0.35f), new Keyframe(1f, 0f)));
        sizeOL.y = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.35f, 1.25f), new Keyframe(1f, 0f)));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.sortingOrder = 2;
    }

    private static void ConfigureSoulStreaksPS(ParticleSystem ps, Material mat, bool isVanish)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = isVanish ? 0f : new ParticleSystem.MinMaxCurve(5.0f, 9.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.startColor = new Color(0.55f, 1.0f, 0.75f, 1f); // 帶一點死靈幽綠與亮紫對比

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = isVanish ? 1.35f : 0.15f;
        shape.radiusThickness = 1.0f;

        var velOL = ps.velocityOverLifetime;
        velOL.enabled = isVanish;
        velOL.radial = new ParticleSystem.MinMaxCurve(-4.8f); // 急速向中心坍縮

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale = 2.4f;
    }

    private static void ConfigureGroundRingPS(ParticleSystem ps, Material mat, bool isVanish)
    {
        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = 0.48f;
        main.startSpeed = 0f;
        main.startSize = 2.8f;
        main.startColor = new Color(0.75f, 0.25f, 1.0f, 0.9f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, isVanish
            ? new AnimationCurve(new Keyframe(0f, 1.15f), new Keyframe(1f, 0.1f))   // 消失時地面圓環向內收縮
            : new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(1f, 1.25f))); // 現身時地面圓環向外震開

        var colOL = ps.colorOverLifetime;
        colOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        colOL.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard; // 強制平貼地板
    }

    private static Material CreateParticleMaterial(string path, Shader shader, Texture2D tex, Color color, bool isAdditive)
    {
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", isAdditive ? 2f : 0f); // 2=Additive, 0=Alpha
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)(isAdditive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
        mat.renderQueue = 3000;

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Texture2D CreateSmokeTexture(string path)
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / 127.5f) - 1f;
                float ny = (y / 127.5f) - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float mask = r >= 0.95f ? 0f : Mathf.Pow(1f - r / 0.95f, 1.6f);

                float n1 = Mathf.PerlinNoise(x * 0.035f + 1.3f, y * 0.035f + 2.1f);
                float n2 = Mathf.PerlinNoise(x * 0.075f + 5.7f, y * 0.075f + 4.3f);
                float billow = Mathf.Clamp01(mask * (0.5f + 0.7f * n1));
                float rim = Mathf.Clamp01(Mathf.Sin(billow * Mathf.PI) * n2);
                float a = r >= 0.95f ? 0f : Mathf.Clamp01(mask * (0.35f + 0.85f * n1));

                pixels[y * size + x] = new Color(billow, n2, rim, a);
            }
        }
        return SaveAndImportTexture(tex, pixels, path);
    }

    private static Texture2D CreateSlitTexture(string path)
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / 127.5f) - 1f;
                float ny = (y / 127.5f) - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float edge = d >= 0.96f ? 0f : Mathf.Pow(1f - d / 0.96f, 2f);
                float vSlit = Mathf.Exp(-Mathf.Abs(nx) * 26f) * Mathf.Clamp01(1f - Mathf.Abs(ny));
                float hCross = Mathf.Exp(-Mathf.Abs(ny) * 34f) * Mathf.Clamp01(1f - Mathf.Abs(nx * 2f));
                float core = Mathf.Exp(-d * 6f);
                float a = Mathf.Clamp01((vSlit * 1.1f + hCross * 0.5f + core * 0.8f) * edge);
                pixels[y * size + x] = new Color(1f, 0.85f, 1f, a);
            }
        }
        return SaveAndImportTexture(tex, pixels, path);
    }

    private static Texture2D CreateRingTexture(string path)
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / 127.5f) - 1f;
                float ny = (y / 127.5f) - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float n = Mathf.PerlinNoise(nx * 2.5f + 3f, ny * 2.5f + 3f) * 0.08f;
                float ring = Mathf.Exp(-Mathf.Pow((r - (0.68f + n)) * 11f, 2f));
                float a = r >= 0.95f ? 0f : Mathf.Clamp01(ring * (1f - Mathf.Pow(r, 4f)));
                pixels[y * size + x] = new Color(0.85f, 0.5f, 1f, a);
            }
        }
        return SaveAndImportTexture(tex, pixels, path);
    }

    private static Texture2D SaveAndImportTexture(Texture2D tex, Color[] pixels, string path)
    {
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif