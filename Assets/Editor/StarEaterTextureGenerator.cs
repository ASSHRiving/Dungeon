#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 噬星者 (StarEater) 專屬刀光貼圖程序化生成器
/// 與 RoyalSword 的「平滑聖光」做出區隔：全部採用鋸齒 / 撕裂 / 破碎的造型語言，呼應劍身的紫晶倒刺。
///
/// UV 約定（TrailRenderer Stretch 模式）：
///   U = 沿刀光長度，0 = 刀頭 (最新)，1 = 刀尾 (最舊)
///   V = 刀光寬度方向，圖案以 V=0.5 為中心上下近似對稱（避免左右揮砍時 UV 翻轉造成外觀不一致）
/// </summary>
public static class StarEaterTextureGenerator
{
    public const string Folder = "Assets/VFX/StarEater";
    public const string EdgePath = Folder + "/Tex_StarEater_Slash_Jagged.png";
    public const string ShadowPath = Folder + "/Tex_StarEater_Abyss_Torn.png";
    public const string CorePath = Folder + "/Tex_StarEater_Core_Crack.png";
    public const string ShardPath = Folder + "/Tex_StarEater_StarShard.png";

    [MenuItem("Tools/VFX/StarEater/Regenerate Textures")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/VFX", "StarEater");

        Save(EdgePath, 512, 128, JaggedEdge);
        Save(ShadowPath, 512, 128, AbyssTorn);
        Save(CorePath, 512, 64, CoreCrack);
        Save(ShardPath, 128, 128, StarShard, clamp: true);

        Debug.Log("<color=#B266FF>[StarEater VFX]</color> 已生成 4 張噬星者專屬鋸齒刀光貼圖。");
    }

    // =====================================================================
    #region 1. 鋸齒刃緣 (主刀光) —— 鯊鰭狀倒刺，前緣陡、後緣緩，形成「撕裂前進」的方向感

    private static float JaggedEdge(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        float aa = 1.5f / h;

        float taper = Mathf.Lerp(1f, 0.3f, Mathf.Pow(u, 0.8f));

        // 上下兩側倒刺錯開半個齒距 → 整體呈鋸齒交錯 (Z 字感)
        // front=0.78：緩升、陡降 → 尖端朝刀尾方向「向後掃」，與劍身紫晶倒刺同方向
        const float teeth = 7f;
        float phase = u * teeth + (upper ? 0f : 0.5f);
        float tooth = SharkTooth(phase, 0.78f, 1.35f) * Mathf.Lerp(0.55f, 1f, Hash((int)Mathf.Floor(phase), upper ? 11 : 23));

        float halfW = (0.06f + 0.25f * tooth) * taper;

        float body = 1f - SmoothStep(halfW - aa, halfW + aa, d);
        float core = Mathf.Exp(-Sq(d / (0.03f * taper)));                   // 中央高熱線
        float rim = Mathf.Exp(-Sq((d - halfW) / (0.012f + aa))) * 0.55f;     // 齒緣描邊，讓鋸齒更銳利可讀
        float glow = Mathf.Exp(-Mathf.Max(0f, d - halfW) / 0.035f) * 0.22f;  // 外圍微光

        float a = Mathf.Max(body * 0.62f + core * 0.5f + rim * body, glow);

        // 刀尾細碎刮痕
        a = Mathf.Max(a, Scratches(u, v, h, 31, 7, 0.35f) * (1f - body));

        a *= SmoothStep(0f, 0.035f, u) * Mathf.Pow(1f - u, 0.8f);
        a *= Erode(u, v, 41, 0.5f, 36f, 6f);
        return Mathf.Clamp01(a);
    }

    #endregion

    // =====================================================================
    #region 2. 深淵撕裂 (陰影本體) —— 邊緣如被撕開的布料，內部有拉絲紋理，刀尾碎裂成孔洞

    private static float AbyssTorn(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        int seed = upper ? 101 : 211;

        float baseW = Mathf.Lerp(0.36f, 0.20f, u);
        float ragged = (Fbm1(u * 14f, seed, 3) - 0.5f) * 0.10f;
        // 不規則間距的尖銳三角齒 → 撕裂邊
        float teethPhase = u * 15f + (upper ? 0.3f : 0.8f) + Fbm1(u * 6f, seed + 3, 2) * 0.8f;
        float teeth = SharkTooth(teethPhase, 0.72f, 1.1f) * Mathf.Lerp(0.04f, 0.12f, Hash((int)Mathf.Floor(teethPhase), seed + 5));
        float halfW = baseW + ragged + teeth;

        float body = 1f - SmoothStep(halfW - 0.02f, halfW + 0.004f, d);
        float streak = Noise2(u * 5f, v * 34f, seed + 7);
        float density = Mathf.Lerp(0.5f, 1f, streak) * Mathf.Lerp(1f, 0.75f, d / 0.5f);

        float a = body * density;
        a *= SmoothStep(0f, 0.05f, u) * Mathf.Pow(1f - u, 0.85f);
        a *= Erode(u, v, 307, 0.3f, 22f, 9f);
        return Mathf.Clamp01(a);
    }

    #endregion

    // =====================================================================
    #region 3. 星裂核心 —— 尖角折線（閃電/裂痕），刀尾斷裂成碎段

    private static float CoreCrack(float u, float v, int w, int h)
    {
        const int segs = 11;
        float aspect = (float)w / h;                 // 在等比例空間計算真正的點到線段距離，轉角不會斷裂
        Vector2 p = new Vector2(u * aspect, v);
        int k = Mathf.Min((int)Mathf.Floor(u * segs), segs - 1);

        float d = float.MaxValue;
        float segMask = 0f;
        for (int s = Mathf.Max(0, k - 1); s <= Mathf.Min(segs - 1, k + 1); s++)
        {
            Vector2 a0 = new Vector2((float)s / segs * aspect, CrackKnot(s));
            Vector2 a1 = new Vector2((float)(s + 1) / segs * aspect, CrackKnot(s + 1));
            float t;
            float ds = SegmentDistance(p, a0, a1, out t);
            if (ds < d)
            {
                d = ds;
                // 越靠刀尾越容易整段斷掉；斷點在線段兩端平滑淡出，不會出現硬切邊
                float broken = Hash(s, 503) < ((float)s / segs) * 0.8f - 0.12f ? 1f : 0f;
                float endFade = SmoothStep(0f, 0.25f, t) * SmoothStep(1f, 0.75f, t);
                segMask = Mathf.Lerp(1f, 0.12f, broken * endFade);
            }
        }

        float th = Mathf.Lerp(0.09f, 0.02f, u);
        float aa = 1.5f / h;
        float line = 1f - SmoothStep(th * 0.5f - aa, th * 0.5f + aa, d);
        float glow = Mathf.Exp(-d / 0.07f) * 0.45f;

        float alpha = Mathf.Max(line, glow) * segMask;
        alpha *= SmoothStep(0f, 0.03f, u) * Mathf.Pow(1f - u, 1.1f);
        return Mathf.Clamp01(alpha);
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static float CrackKnot(int k)
    {
        if (k == 0) return 0.5f;
        float sign = (k % 2 == 0) ? 1f : -1f;
        return 0.5f + sign * Mathf.Lerp(0.08f, 0.26f, Hash(k, 601));
    }

    #endregion

    // =====================================================================
    #region 4. 星屑碎晶 —— 長短不一的四芒尖星 + 細斜刺，比 HolyStar 更銳利、更像晶體碎片

    private static float StarShard(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * 2f - 1f, v * 2f - 1f);
        float r = p.magnitude;

        float a = 0f;
        a = Mathf.Max(a, Spike(p, new Vector2(0f, 1f), 0.95f, 0.10f));
        a = Mathf.Max(a, Spike(p, new Vector2(0f, -1f), 0.80f, 0.10f));
        a = Mathf.Max(a, Spike(p, new Vector2(1f, 0f), 0.55f, 0.08f));
        a = Mathf.Max(a, Spike(p, new Vector2(-1f, 0f), 0.48f, 0.08f));
        float diag = 0.70710678f;
        a = Mathf.Max(a, Spike(p, new Vector2(diag, diag), 0.28f, 0.05f) * 0.8f);
        a = Mathf.Max(a, Spike(p, new Vector2(-diag, -diag), 0.24f, 0.05f) * 0.8f);
        a = Mathf.Max(a, Spike(p, new Vector2(-diag, diag), 0.20f, 0.05f) * 0.7f);
        a = Mathf.Max(a, Spike(p, new Vector2(diag, -diag), 0.18f, 0.05f) * 0.7f);

        float core = Mathf.Exp(-Sq(r / 0.09f));
        float glow = Mathf.Exp(-r / 0.16f) * 0.35f;
        a = Mathf.Max(a, Mathf.Max(core, glow));
        a *= 1f - SmoothStep(0.92f, 1f, r);
        return Mathf.Clamp01(a);
    }

    private static float Spike(Vector2 p, Vector2 dir, float len, float width)
    {
        float along = Vector2.Dot(p, dir);
        if (along <= 0f || along >= len) return 0f;
        float perp = Mathf.Abs(p.x * dir.y - p.y * dir.x);
        float wAt = width * Mathf.Pow(1f - along / len, 1.4f);
        return 1f - SmoothStep(wAt * 0.55f, wAt, perp);
    }

    #endregion

    // =====================================================================
    #region 共用工具

    /// <summary>鯊鰭倒刺：前緣在 front 比例內陡升，之後緩降；pow 讓尖端更銳利</summary>
    private static float SharkTooth(float x, float front, float sharp)
    {
        float s = x - Mathf.Floor(x);
        float t = s < front ? s / front : 1f - (s - front) / (1f - front);
        return Mathf.Pow(Mathf.Clamp01(t), sharp);
    }

    /// <summary>刀尾侵蝕：越靠近刀尾，被噪聲吃掉的比例越高 → 破碎消散感</summary>
    private static float Erode(float u, float v, int seed, float start, float fu, float fv)
    {
        float threshold = SmoothStep(start, 1f, u) * 0.85f;
        float n = Noise2(u * fu, v * fv, seed) * 0.7f + Noise2(u * fu * 2.3f, v * fv * 2.3f, seed + 1) * 0.3f;
        return SmoothStep(threshold - 0.06f, threshold + 0.06f, n);
    }

    private static float Scratches(float u, float v, int h, int seed, int count, float strength)
    {
        float a = 0f;
        for (int i = 0; i < count; i++)
        {
            float vk = Mathf.Lerp(0.18f, 0.82f, Hash(i, seed));
            float u0 = Mathf.Lerp(0.15f, 0.6f, Hash(i, seed + 1));
            float u1 = u0 + Mathf.Lerp(0.1f, 0.35f, Hash(i, seed + 2));
            if (u < u0 || u > u1) continue;
            float along = (u - u0) / (u1 - u0);
            float line = Mathf.Exp(-Sq((v - vk) * h / 0.9f));
            a = Mathf.Max(a, line * Mathf.Sin(along * Mathf.PI) * strength);
        }
        return a;
    }

    private delegate float PixelFunc(float u, float v, int w, int h);

    private static void Save(string path, int w, int h, PixelFunc func, bool clamp = true)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(func(u, v, w, h)) * 255f);
                pixels[y * w + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());

        // 黑底預覽圖（存到專案 Temp，不會進版控），方便肉眼檢查造型
        var preview = new Color32[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte a = pixels[i].a;
            preview[i] = new Color32(a, a, a, 255);
        }
        tex.SetPixels32(preview);
        tex.Apply();
        string previewDir = Path.Combine(Application.dataPath, "../Temp/StarEaterPreview");
        Directory.CreateDirectory(previewDir);
        File.WriteAllBytes(Path.Combine(previewDir, Path.GetFileName(path)), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }

    private static float Sq(float x) => x * x;

    private static float SmoothStep(float a, float b, float x)
    {
        if (Mathf.Approximately(a, b)) return x < a ? 0f : 1f;
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    private static float Hash(int x, int seed)
    {
        unchecked
        {
            uint hsh = (uint)(x * 374761393 + seed * 668265263);
            hsh = (hsh ^ (hsh >> 13)) * 1274126177u;
            hsh ^= hsh >> 16;
            return (hsh & 0xFFFFFF) / 16777215f;
        }
    }

    private static float Hash2(int x, int y, int seed) => Hash(x * 73856093 ^ y * 19349663, seed);

    private static float Noise1(float x, int seed)
    {
        int i = Mathf.FloorToInt(x);
        float f = x - i;
        f = f * f * (3f - 2f * f);
        return Mathf.Lerp(Hash(i, seed), Hash(i + 1, seed), f);
    }

    private static float Fbm1(float x, int seed, int octaves)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        for (int o = 0; o < octaves; o++)
        {
            sum += Noise1(x, seed + o * 17) * amp;
            norm += amp;
            x *= 2.1f;
            amp *= 0.5f;
        }
        return sum / norm;
    }

    private static float Noise2(float x, float y, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash2(ix, iy, seed), b = Hash2(ix + 1, iy, seed);
        float c = Hash2(ix, iy + 1, seed), d = Hash2(ix + 1, iy + 1, seed);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    #endregion
}
#endif
