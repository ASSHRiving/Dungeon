#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public static class RoyalSwordTextureGenerator
{
    private const string OutputFolder = "Assets/VFX_RoyalSword_Textures";

    // 正確的 GLSL smoothstep 函式：當 x >= edge1 時保證回傳 1.0 (100% 不透明)
    private static float GlslSmoothstep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    [MenuItem("Tools/Royal Sword VFX/1. 一鍵生成 4 張特效貼圖 (PNG)")]
    public static void GenerateAllTextures()
    {
        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        SaveTextureAsPNG(GenerateStarGlowTexture(256), "Tex_Particle_HolyStar.png");
        SaveTextureAsPNG(GenerateDiamondFacetTexture(256), "Tex_Particle_DiamondFacet.png");
        SaveTextureAsPNG(GenerateSlashEdgeTrailTexture(1024, 256), "Tex_Trail_HolySlash_Edge.png");
        SaveTextureAsPNG(GenerateSlashCoreTrailTexture(1024, 256), "Tex_Trail_HolySlash_Core.png");

        AssetDatabase.Refresh();
        ConfigureTextureImporters();
        Debug.Log($"<color=#00E5FF>[RoyalSword VFX]</color> 4 張 100% 實心高亮特效貼圖已重新生成於：{OutputFolder}");
    }

    [MenuItem("Tools/Royal Sword VFX/2. 自動建立 URP 特效材質球 (Materials)")]
    public static void GenerateURPMaterials()
    {
        GenerateAllTextures();

        Shader urpParticleUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpParticleUnlit == null)
        {
            urpParticleUnlit = Shader.Find("Sprites/Default"); // 備用保底 Shader
        }

        CreateAdditiveParticleMaterial(urpParticleUnlit, "Tex_Particle_HolyStar.png", "Mat_VFX_HolyStar.mat");
        CreateAdditiveParticleMaterial(urpParticleUnlit, "Tex_Particle_DiamondFacet.png", "Mat_VFX_DiamondFacet.mat");
        CreateAdditiveParticleMaterial(urpParticleUnlit, "Tex_Trail_HolySlash_Edge.png", "Mat_VFX_Trail_GoldEdge.mat");
        CreateAdditiveParticleMaterial(urpParticleUnlit, "Tex_Trail_HolySlash_Core.png", "Mat_VFX_Trail_CyanCore.mat");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"<color=#FFD700>[RoyalSword VFX]</color> URP 特效材質球與 GPU Blend 狀態已更新完畢！");
    }

    [MenuItem("Tools/Royal Sword VFX/3. 修正選中 TrailRenderer 的過大尺寸與顏色")]
    public static void FixSelectedTrailRenderer()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("請先在 Hierarchy 選中您的 TrailRenderer 物件！");
            return;
        }

        TrailRenderer[] trails = selected.GetComponentsInChildren<TrailRenderer>(true);
        if (trails.Length == 0)
        {
            Debug.LogWarning("選中的物件或其子物件中找不到 TrailRenderer！");
            return;
        }

        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>($"{OutputFolder}/Mat_VFX_Trail_GoldEdge.mat");
        Material cyanMat = AssetDatabase.LoadAssetAtPath<Material>($"{OutputFolder}/Mat_VFX_Trail_CyanCore.mat");

        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer tr = trails[i];
            Undo.RecordObject(tr, "Fix Royal Sword Trail");

            bool isCore = tr.name.ToLower().Contains("cyan") || tr.name.ToLower().Contains("core") || i == 1;

            // 1. 修正過寬與過長問題：外層寬 0.36m (滯留 0.12秒)，內層寬 0.22m (滯留 0.16秒)
            tr.time = isCore ? 0.16f : 0.12f;
            tr.widthMultiplier = isCore ? 0.22f : 0.36f;

            // 寬度曲線：出刀飽滿，尾端平滑收尖
            AnimationCurve widthCurve = new AnimationCurve();
            widthCurve.AddKey(0.0f, 0.9f);
            widthCurve.AddKey(0.2f, 1.0f);
            widthCurve.AddKey(1.0f, 0.15f);
            tr.widthCurve = widthCurve;

            // 2. 設定細膩圓滑度與貼圖延展模式
            tr.minVertexDistance = 0.015f;
            tr.numCornerVertices = 4;
            tr.numCapVertices = 2;
            tr.textureMode = LineTextureMode.Stretch;
            tr.alignment = LineAlignment.View;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;

            // 3. 設定金藍聖光漸層色
            Gradient grad = new Gradient();
            if (!isCore)
            {
                // 外層燦金鋒芒 (#FFF8E1 -> #FFD700 -> #FF8F00)
                grad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1f, 0.97f, 0.88f), 0.0f),
                        new GradientColorKey(new Color(1f, 0.84f, 0.0f), 0.35f),
                        new GradientColorKey(new Color(1f, 0.56f, 0.0f), 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.95f, 0.4f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                if (goldMat != null) tr.sharedMaterial = goldMat;
            }
            else
            {
                // 內層青藍聖光 (#00E5FF -> #00B0FF -> #1A237E)
                grad.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(0.0f, 0.90f, 1.0f), 0.0f),
                        new GradientColorKey(new Color(0.0f, 0.69f, 1.0f), 0.5f),
                        new GradientColorKey(new Color(0.10f, 0.14f, 0.49f), 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1.0f, 0.0f),
                        new GradientAlphaKey(0.85f, 0.5f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                if (cyanMat != null) tr.sharedMaterial = cyanMat;
            }
            tr.colorGradient = grad;
            EditorUtility.SetDirty(tr);
        }

        Debug.Log("<color=#00E5FF>[RoyalSword VFX]</color> 已自動將 TrailRenderer 調整為緊湊單手劍比例（寬度 0.36m / 時間 0.12s）並綁定材質！");
    }
    [MenuItem("Tools/Royal Sword VFX/4. 一鍵建立揮砍星芒粒子 (PS_Slash_Sparks)")]
    public static void CreateSlashSparksParticleSystem()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogWarning("請先在 Hierarchy 選中您的武器物件 (Weapon_03_RoyalSword 或 Blade_Tip)！");
            return;
        }

        // 建立或取得 PS_Slash_Sparks 物件
        Transform existing = selected.transform.Find("PS_Slash_Sparks");
        GameObject psObj = existing != null ? existing.gameObject : new GameObject("PS_Slash_Sparks");
        psObj.transform.SetParent(selected.transform, false);
        // 預設對齊劍身中上段 (距握把約 0.65m 處)
        psObj.transform.localPosition = new Vector3(0f, 0f, 0.65f);
        psObj.transform.localRotation = Quaternion.identity;

        ParticleSystem ps = psObj.GetComponent<ParticleSystem>();
        if (ps == null) ps = psObj.AddComponent<ParticleSystem>();

        // 先停止播放以設定 Duration
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // 1. Main Module (主模組)
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = true;
        main.playOnAwake = false; // 由 RoyalSwordVFXController 在揮砍時控制 Play()/Stop()
        main.simulationSpace = ParticleSystemSimulationSpace.World; // 關鍵：設為 World 才能留在揮砍軌跡上！
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.6f); // 微幅向外迸發的速度
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f); // 精緻星芒尺寸，不過度遮擋刀光
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = 0.08f; // 極輕微下墜感

        // 雙色隨機：燦金 (#FFD700) 與 青藍聖光 (#00E5FF)
        Color goldColor = new Color(1.0f, 0.843f, 0.0f, 1.0f);
        Color cyanColor = new Color(0.0f, 0.898f, 1.0f, 1.0f);
        main.startColor = new ParticleSystem.MinMaxGradient(goldColor, cyanColor);

        // 2. Emission Module (發射模組：利用 Distance 隨揮動距離自動噴發！)
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 25f;     // 基礎發射率
        emission.rateOverDistance = 35f; // 關鍵：揮越快、移動距離越長，拉出的星芒越密集！
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.02f, 8, 14) // 出刀瞬間小爆發
        });

        // 3. Shape Module (形狀模組：貼合 0.213m ~ 0.965m 的劍刃區間)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        // 寬 0.04m、厚 0.04m、長 0.55m (涵蓋劍身主要揮砍段)
        shape.scale = new Vector3(0.04f, 0.04f, 0.55f);

        // 4. Velocity over Lifetime (空氣阻力減速：迸發後迅速停留在空中閃爍)
        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = 0.05f;
        limitVel.dampen = 0.25f;

        // 5. Color over Lifetime (生命週期色彩與閃爍消散)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(new Color(1f, 0.92f, 0.5f), 0.5f),
                new GradientColorKey(new Color(0f, 0.7f, 1f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.12f), // 快速亮起
                new GradientAlphaKey(0.85f, 0.55f),
                new GradientAlphaKey(0.0f, 1.0f)   // 尾端柔和消散
            }
        );
        col.color = grad;

        // 6. Size over Lifetime (尺寸曲線：瞬間放大成星芒 -> 緩慢收縮成光點消失)
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.2f);
        sizeCurve.AddKey(0.15f, 1.0f);
        sizeCurve.AddKey(0.6f, 0.65f);
        sizeCurve.AddKey(1.0f, 0.0f);
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // 7. Rotation over Lifetime (自轉產生十字星芒閃耀感)
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        // 8. Noise Module (神聖氣流擾動，讓星塵軌跡不死板)
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 2.5f;
        noise.scrollSpeed = 1.0f;
        noise.damping = true;

        // 9. Renderer Module (綁定材質球)
        var renderer = psObj.GetComponent<ParticleSystemRenderer>();
        Material starMat = AssetDatabase.LoadAssetAtPath<Material>($"{OutputFolder}/Mat_VFX_HolyStar.mat");
        if (starMat != null)
        {
            renderer.sharedMaterial = starMat;
        }
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Selection.activeGameObject = psObj;
        EditorUtility.SetDirty(psObj);
        Debug.Log("<color=#FFD700>[RoyalSword VFX]</color> PS_Slash_Sparks 揮砍星芒粒子已自動建立並配置完成！");
    }

    // =========================================================================
    // 貼圖 1：四角星芒聖光貼圖
    // =========================================================================
    private static Texture2D GenerateStarGlowTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)(size - 1)) * 2f - 1f;
                float v = (y / (float)(size - 1)) * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);

                float core = Mathf.Exp(-r * r * 12f) * 1.3f;
                float spikeX = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(v) * 8f), 2.2f) * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(u)), 1.4f);
                float spikeY = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(u) * 8f), 2.2f) * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(v)), 1.4f);

                float diagU = (u + v) * 0.7071f;
                float diagV = (u - v) * 0.7071f;
                float diag1 = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(diagV) * 12f), 2.2f) * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(diagU) * 1.3f), 1.6f);
                float diag2 = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(diagU) * 12f), 2.2f) * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(diagV) * 1.3f), 1.6f);

                float edgeMask = 1.0f - GlslSmoothstep(0.05f, 0.98f, r);
                float val = Mathf.Clamp01((core + (spikeX + spikeY) * 1.2f + (diag1 + diag2) * 0.6f) * edgeMask);

                // 同時寫入 RGB 與 Alpha，確保不論 Shader 用讀 RGB 還是 Alpha 都能 100% 發光
                tex.SetPixel(x, y, new Color(val, val, val, val));
            }
        }
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 貼圖 2：幾何菱形切面微粒貼圖
    // =========================================================================
    private static Texture2D GenerateDiamondFacetTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)(size - 1)) * 2f - 1f;
                float v = (y / (float)(size - 1)) * 2f - 1f;

                float diamondDist = Mathf.Abs(u) * 1.35f + Mathf.Abs(v) * 0.9f;
                float quadrantShade = (u > 0f ? 1.0f : 0.85f) * (v > 0f ? 1.0f : 0.9f);
                float innerFacet = diamondDist < 0.5f ? (1f - (diamondDist / 0.5f) * 0.2f) * quadrantShade : 0f;

                float ridge = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(u) * 10f), 1.5f) * Mathf.Max(0f, 1f - diamondDist);
                float outerGlow = Mathf.Exp(-diamondDist * diamondDist * 4.0f) * 0.7f;

                float edgeMask = 1.0f - GlslSmoothstep(0.65f, 1.0f, diamondDist);
                float val = Mathf.Clamp01((Mathf.Max(innerFacet * 1.15f, outerGlow) + ridge * 0.5f) * edgeMask);

                tex.SetPixel(x, y, new Color(val, val, val, val));
            }
        }
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 貼圖 3：外層燦金拉絲刀光貼圖 (修正 GlslSmoothstep 後 100% 飽滿可見)
    // =========================================================================
    private static Texture2D GenerateSlashEdgeTrailTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);

            // 上下邊緣柔和收邊，中間 (V = 0.15 ~ 0.88) 保持 1.0 滿亮度
            float edgeFadeBottom = GlslSmoothstep(0.02f, 0.18f, v);
            float edgeFadeTop = 1.0f - GlslSmoothstep(0.86f, 0.99f, v);
            float verticalMask = edgeFadeBottom * edgeFadeTop;

            // 靠近劍尖側 (V 較大處) 更亮
            float tipBoost = Mathf.Lerp(0.45f, 1.15f, GlslSmoothstep(0.1f, 0.85f, v));

            // 6 道清晰劍氣橫向拉絲
            float streak1 = Mathf.Sin(v * Mathf.PI * 12f) * 0.5f + 0.5f;
            float streak2 = Mathf.Sin(v * Mathf.PI * 30f + 0.8f) * 0.5f + 0.5f;
            float baseStreak = Mathf.Lerp(0.4f, 1.0f, streak1 * 0.65f + streak2 * 0.35f);

            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);

                // U=0 (刀頭) 迅速達到 1.0 滿亮度，往 U=1 (刀尾) 平滑消散
                float headAttack = GlslSmoothstep(0.0f, 0.03f, u);
                float tailFade = Mathf.Pow(1.0f - u, 1.2f);

                float noise = Mathf.PerlinNoise(u * 4.5f + 10f, v * 16f);
                float dynamicStreak = Mathf.Lerp(baseStreak, baseStreak * (0.5f + noise * 0.8f), u * 0.65f);

                // 劍尖最外圍的高亮銳利鋒線 (位於 V = 0.82)
                float sharpEdgeLine = Mathf.Exp(-Mathf.Pow((v - 0.82f) * 14f, 2f)) * Mathf.Pow(1.0f - u, 0.8f) * 1.4f;

                float val = Mathf.Clamp01((dynamicStreak * tipBoost + sharpEdgeLine) * verticalMask * headAttack * tailFade);

                tex.SetPixel(x, y, new Color(val, val, val, val));
            }
        }
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 貼圖 4：內層青藍核心刀光貼圖 (修正 GlslSmoothstep 後 100% 飽滿可見)
    // =========================================================================
    private static Texture2D GenerateSlashCoreTrailTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);

            // 聚焦於中央的厚實光束
            float coreBeam = Mathf.Exp(-Mathf.Pow((v - 0.5f) * 2.4f, 2f)) * 1.35f;
            float edgeFeather = GlslSmoothstep(0.02f, 0.15f, v) * (1.0f - GlslSmoothstep(0.85f, 0.98f, v));

            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);

                float headAttack = GlslSmoothstep(0.0f, 0.03f, u);
                float tailFade = Mathf.Pow(1.0f - u, 1.0f);

                float energyWave = Mathf.PerlinNoise(u * 6.0f + 3.3f, v * 9.0f);
                float waveMod = Mathf.Lerp(0.75f, 1.3f, energyWave);

                float val = Mathf.Clamp01(coreBeam * edgeFeather * headAttack * tailFade * waveMod);

                tex.SetPixel(x, y, new Color(val, val, val, val));
            }
        }
        tex.Apply();
        return tex;
    }

    // =========================================================================
    // 儲存與材質球 GPU 混合參數強制綁定
    // =========================================================================
    private static void SaveTextureAsPNG(Texture2D tex, string fileName)
    {
        string fullPath = Path.Combine(OutputFolder, fileName);
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void ConfigureTextureImporters()
    {
        string[] files = {
            "Tex_Particle_HolyStar.png",
            "Tex_Particle_DiamondFacet.png",
            "Tex_Trail_HolySlash_Edge.png",
            "Tex_Trail_HolySlash_Core.png"
        };

        foreach (string file in files)
        {
            string assetPath = $"{OutputFolder}/{file}";
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }
    }

    private static void CreateAdditiveParticleMaterial(Shader shader, string texFileName, string matFileName)
    {
        string texPath = $"{OutputFolder}/{texFileName}";
        string matPath = $"{OutputFolder}/{matFileName}";

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        mat.SetTexture("_BaseMap", tex);
        mat.SetTexture("_MainTex", tex);
        // 給予 1.8 倍 HDR 亮度強化，讓 URP Bloom 直接亮起來
        mat.SetColor("_BaseColor", new Color(1.8f, 1.8f, 1.8f, 1f));
        mat.SetColor("_Color", new Color(1.8f, 1.8f, 1.8f, 1f));

        // 強制寫入 URP Transparent + Additive 的底層 GPU Render States
        mat.SetFloat("_Surface", 1f); // 1 = Transparent
        mat.SetFloat("_Blend", 2f);   // 2 = Additive
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); // 5
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);      // 1
        mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One); // 1
        mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One); // 1
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", 0f);    // 0 = Off (雙面渲染)

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
    }
}
#endif