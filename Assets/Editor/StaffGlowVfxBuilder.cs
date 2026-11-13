#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StaffGlowVfxBuilder
{
    private const string BaseFolder = "Assets/VFX/StaffGlow";

    [MenuItem("Tools/VFX/一鍵生成法杖施法發光特效 (Staff_Glow_VFX)")]
    public static void BuildStaffGlowVfx()
    {
        if (!Directory.Exists(BaseFolder))
        {
            Directory.CreateDirectory(BaseFolder);
        }

        // 1. 自動生成兩張零邊界程序化粒子貼圖 (.png)
        Texture2D flareTex = CreateAndSaveFlareTexture($"{BaseFolder}/StaffFlare_Core.png");
        Texture2D runeTex = CreateAndSaveRuneTexture($"{BaseFolder}/MiniRune_Ring.png");

        // 2. 建立 URP Additive 材質球 (.mat)
        Shader glowShader = Shader.Find("Custom/URP_NecroStaffGlow");
        if (glowShader == null) glowShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (glowShader == null) glowShader = Shader.Find("Particles/Standard Unlit");

        Material flareMat = CreateAdditiveMaterial($"{BaseFolder}/Mat_StaffFlare.mat", glowShader, flareTex, new Color(1.8f, 0.4f, 3.2f, 1f));
        Material runeMat = CreateAdditiveMaterial($"{BaseFolder}/Mat_MiniRune.mat", glowShader, runeTex, new Color(0.4f, 2.5f, 1.1f, 1f));

        // 3. 建立根物件與 Runtime 控制器
        GameObject root = new GameObject("Staff_Glow_VFX");
        StaffGlowController controller = root.AddComponent<StaffGlowController>();

        // --- Layer 1: Core_StarFlare (紫水晶核心十字星芒) ---
        GameObject coreObj = new GameObject("Core_StarFlare");
        coreObj.transform.SetParent(root.transform, false);
        ParticleSystem corePS = coreObj.AddComponent<ParticleSystem>();
        ConfigureCoreFlarePS(corePS, flareMat);

        // --- Layer 2: Inward_SoulVortex (向心吸入死靈微粒) ---
        GameObject vortexObj = new GameObject("Inward_SoulVortex");
        vortexObj.transform.SetParent(root.transform, false);
        ParticleSystem vortexPS = vortexObj.AddComponent<ParticleSystem>();
        ConfigureInwardVortexPS(vortexPS, flareMat);

        // --- Layer 3: Rune_Halo_Ring (法杖頂端旋轉微型法陣環) ---
        GameObject runeObj = new GameObject("Rune_Halo_Ring");
        runeObj.transform.SetParent(root.transform, false);
        ParticleSystem runePS = runeObj.AddComponent<ParticleSystem>();
        ConfigureRuneRingPS(runePS, runeMat);

        // --- Layer 4: Staff_PointLight (動態紫光點光源) ---
        GameObject lightObj = new GameObject("Staff_PointLight");
        lightObj.transform.SetParent(root.transform, false);
        Light ptLight = lightObj.AddComponent<Light>();
        ptLight.type = LightType.Point;
        ptLight.color = new Color(0.72f, 0.25f, 1.0f);
        ptLight.range = 4.2f;
        ptLight.intensity = 3.5f;
        ptLight.shadows = LightShadows.None; // 關閉陰影確保零效能負擔

        controller.staffLight = ptLight;
        controller.maxLightIntensity = 3.5f;

        // 4. 儲存為 Prefab 並掛載到當前選中的法杖物件下
        string prefabPath = $"{BaseFolder}/Staff_Glow_VFX.prefab";
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);

        if (Selection.activeTransform != null)
        {
            root.transform.SetParent(Selection.activeTransform, false);
            root.transform.localPosition = Vector3.zero;
        }

        // 預設先設為關閉，方便直接在 Animation 視窗錄製打勾啟用！
        root.SetActive(false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        Debug.Log($"[StaffGlowVfxBuilder] 已成功生成 {prefabPath}！現在可直接在 Animation 視窗錄製 Staff_Glow_VFX 的啟用開關。");
    }

    private static void ConfigureCoreFlarePS(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.loop = true;
        main.duration = 1.0f;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 0.75f;
        main.startColor = new Color(0.85f, 0.4f, 1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 4;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.enabled = false; // 鎖定在水晶正中心

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.5f, 1.15f),
            new Keyframe(1f, 0.0f)
        );
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void ConfigureInwardVortexPS(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.45f;
        main.startSpeed = 0f; // 由 VelocityOverLifetime 的 Radial 負責向心吸入
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
        main.startColor = new Color(0.55f, 1.0f, 0.75f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 35f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.55f;
        shape.radiusThickness = 1.0f; // 只在球殼外圍生成，往中心吸

        var velOL = ps.velocityOverLifetime;
        velOL.enabled = true;
        velOL.space = ParticleSystemSimulationSpace.Local;
        velOL.radial = new ParticleSystem.MinMaxCurve(-1.25f); // 負向徑向速度 = 強力向中心吸入
        velOL.orbitalZ = new ParticleSystem.MinMaxCurve(3.2f); // 伴隨旋轉渦流

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.1f),
            new Keyframe(0.3f, 1.0f),
            new Keyframe(1f, 0.0f)
        ));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
    }

    private static void ConfigureRuneRingPS(ParticleSystem ps, Material mat)
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.8f;
        main.startSpeed = 0f;
        main.startSize = 0.65f;
        main.startColor = new Color(0.75f, 0.35f, 1.0f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 2;

        var emission = ps.emission;
        emission.rateOverTime = 2.5f;

        var shape = ps.shape;
        shape.enabled = false;

        var rotOL = ps.rotationOverLifetime;
        rotOL.enabled = true;
        rotOL.z = new ParticleSystem.MinMaxCurve(160f * Mathf.Deg2Rad);

        var colOL = ps.colorOverLifetime;
        colOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) }
        );
        colOL.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static Material CreateAdditiveMaterial(string path, Shader shader, Texture2D tex, Color hdrColor)
    {
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdrColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdrColor);

        // 設定 URP Particles/Unlit 的 Transparent + Additive + Both
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);     // Additive
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);       // Both
        mat.renderQueue = 3000;

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Texture2D CreateAndSaveFlareTexture(string path)
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
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float edgeMask = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Min(1f, dist), 2.2f));
                float core = Mathf.Exp(-dist * 5.2f);
                float spike = Mathf.Exp(-Mathf.Abs(ny) * 24f) * Mathf.Clamp01(1f - Mathf.Abs(nx))
                            + Mathf.Exp(-Mathf.Abs(nx) * 24f) * Mathf.Clamp01(1f - Mathf.Abs(ny));
                float a = dist >= 0.97f ? 0f : Mathf.Clamp01((core * 1.2f + spike * 0.75f) * edgeMask);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
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

    private static Texture2D CreateAndSaveRuneTexture(string path)
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
                float angle = Mathf.Atan2(ny, nx);

                float outerRing = Mathf.Exp(-Mathf.Abs(r - 0.76f) * 38f);
                float innerRing = Mathf.Exp(-Mathf.Abs(r - 0.58f) * 42f);
                float spokes = (r > 0.58f && r < 0.76f) ? Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 6f)), 14f) : 0f;
                float a = r >= 0.96f ? 0f : Mathf.Clamp01(outerRing + innerRing + spokes);
                pixels[y * size + x] = new Color(1f, 0.9f, 1f, a);
            }
        }
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