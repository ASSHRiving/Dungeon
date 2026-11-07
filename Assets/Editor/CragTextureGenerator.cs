#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 岩峰 (Crag) 專屬巨劍刀光貼圖程序化生成器
/// 視覺語言取自 Crag 武器外觀（玄武暗岩巨劍 + 崩口石刃 + 劍身中央彎折金珀礦脈）：
///   1. 階梯狀崩口岩刃（梯形/多邊形碎石切面與節理裂紋，與 StarEater 的弧形鯊鰭倒刺、RoyalSword 的平滑聖光明確區隔）
///   2. 玄武岩層殘影（厚重岩塊層理 + 跨中心無縫 Voronoi 龜裂網格 + 刀尾多邊形崩解碎石侵蝕）
///   3. 金珀礦脈核心（呼應劍身中央鑲嵌的階梯狀黃褐色礦脈與完整收尖的分支細裂隙）
///   4. 低多邊形立體切面岩晶碎塊（具三切面明暗與稜線高光的不規則礦石碎塊）
///
/// UV 約定（TrailRenderer Stretch 模式）：
///   U = 沿刀光長度，0 = 刀頭 (最新)，1 = 刀尾 (最舊)
///   V = 刀光寬度方向，圖案以 V=0.5 為中心上下近似對稱（避免左右揮砍時 UV 翻轉造成外觀不一致）
/// </summary>
public static class CragTextureGenerator
{
    public const string Folder = "Assets/VFX/Crag";
    public const string SlabPath = Folder + "/Tex_Crag_Basalt_Slab.png";
    public const string EdgePath = Folder + "/Tex_Crag_Fracture_Edge.png";
    public const string VeinPath = Folder + "/Tex_Crag_Ochre_Vein.png";
    public const string ChunkPath = Folder + "/Tex_Crag_RockChunk.png";

    [MenuItem("Tools/VFX/Crag/Regenerate Textures")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/VFX", "Crag");

        Save(SlabPath, 512, 128, BasaltSlab);
        Save(EdgePath, 512, 128, FractureEdge);
        Save(VeinPath, 512, 64, OchreVein);
        Save(ChunkPath, 128, 128, RockChunk, clamp: true);

        Debug.Log("<color=#D4AF37>[Crag VFX]</color> 已生成 4 張岩峰 (Crag) 專屬巨劍岩脈刀光貼圖。");
    }

    // =====================================================================
    #region 1. 岩峰崩口刃緣 (主刀光) —— 梯形平頂岩齒、銳利 V 型崩口與石質節理裂紋

    private static float FractureEdge(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        float aa = 1.5f / h;

        // 巨劍厚重比例：前段寬闊飽滿，後段階梯式收窄
        float taper = Mathf.Lerp(1.0f, 0.36f, Mathf.Pow(u, 0.85f));

        // 梯形崩口石齒（平頂 + 陡峭斜切邊 + 銳利 V 型缺口），呼應劍刃下緣的灰岩崩口
        const float blocks = 6.0f;
        float phase = u * blocks + (upper ? 0.0f : 0.42f);
        int blockIdx = Mathf.FloorToInt(phase);
        float s = phase - blockIdx;

        float tooth = ChippedStoneTooth(s, blockIdx, upper ? 17 : 53);
        float halfW = (0.08f + 0.26f * tooth) * taper;

        // 岩刃本體與硬朗切面斜角 (Bevel Rim)
        float body = 1f - SmoothStep(halfW - aa, halfW + aa, d);
        float bevel = SmoothStep(halfW - 0.065f * taper, halfW - 0.008f, d) * body; // 外緣石質斜切面增亮
        float rim = Mathf.Exp(-Sq((d - halfW) / (0.011f + aa))) * 0.85f;            // 銳利石稜描邊
        float spine = Mathf.Exp(-Sq(d / (0.042f * taper))) * 0.70f;                 // 中央厚重岩脊

        // 內部岩石節理紋理（跨中心連續岩紋 + 斜向斷層裂隙）
        float strata = Noise2(u * 9f + d * 4f, v * 24f, 79);
        float rockGrain = Mathf.Lerp(0.85f, 1.05f, strata);
        float cleavage = CleavageCracks(u, v, upper ? 113 : 149);

        // 岩刃實體厚度：本體保持飽滿的高不透明度（0.92~1.0），硬朗切面與石齒稜線高光
        float inner = body * Mathf.Lerp(0.88f, 1.0f, rockGrain) * (1f - cleavage * 0.30f) + bevel * 0.40f + spine * 0.55f;
        float a = Mathf.Max(inner, rim * body * 1.25f);
        // 外側微光
        a = Mathf.Max(a, Mathf.Exp(-Mathf.Max(0f, d - halfW) / 0.032f) * 0.35f);

        // 刀尾飛散的剛性碎石刮痕
        a = Mathf.Max(a, GravelStreaks(u, v, h, 83, 9, 0.60f) * (1f - body * 0.7f));

        // 刀頭瞬間展開、前中段飽滿扎實 (u=0~0.55 保持高不透明度)，後段 (u>0.55) 漸進收斂
        float lengthFade = 1f - SmoothStep(0.55f, 0.98f, u);
        a *= SmoothStep(0f, 0.02f, u) * lengthFade;
        // Voronoi 崩解只在後段生效，前端與中段保持鋒利石齒完整廓形
        float erode = Mathf.Lerp(1.0f, VoronoiRockErode(u, v, 97, 0.46f, 18f, 5.5f), SmoothStep(0.35f, 0.85f, u));
        a *= erode;
        return Mathf.Clamp01(a);
    }

    /// <summary>
    /// 梯形平頂崩口石齒：具備陡峭線性斜切斷面、傾斜平頂與銳利 V 型崩缺，呈現低多邊形巨岩切削感
    /// </summary>
    private static float ChippedStoneTooth(float s, int idx, int seed)
    {
        float h0 = Mathf.Lerp(0.60f, 1.0f, Hash(idx, seed));
        float tilt = (Hash(idx, seed + 1) - 0.5f) * 0.28f;

        // 線性斜切前緣與斷崖後緣（硬朗折角，避免圓滑過渡）
        float up = Mathf.Clamp01((s - 0.02f) / 0.11f);
        float down = Mathf.Clamp01((0.93f - s) / 0.14f);
        float plateau = Mathf.Min(up, down) * Mathf.Clamp01(h0 + (s - 0.45f) * tilt);

        // 石齒頂部的銳利 V 型崩口（折線三角缺口）
        float chipCenter = Mathf.Lerp(0.34f, 0.62f, Hash(idx, seed + 2));
        float chipDepth = Hash(idx, seed + 3) > 0.30f ? 0.38f : 0.0f;
        float vNotch = Mathf.Clamp01(1f - Mathf.Abs(s - chipCenter) / 0.085f);
        float chip = 1f - chipDepth * vNotch;

        return plateau * chip;
    }

    /// <summary>岩石斜向節理裂隙</summary>
    private static float CleavageCracks(float u, float v, int seed)
    {
        float d = Mathf.Abs(v - 0.5f);
        float slant = u * 11f - d * 6.5f;
        float cell = Mathf.Abs((slant - Mathf.Floor(slant)) - 0.5f) * 2f;
        float line = SmoothStep(0.08f, 0.0f, cell) * SmoothStep(0.04f, 0.28f, d);
        float mask = Noise2(u * 14f, v * 14f, seed) > 0.42f ? 1f : 0f;
        return line * mask;
    }

    #endregion

    // =====================================================================
    #region 2. 玄武岩層殘影 (陰影與厚重岩體) —— 階梯狀斷崖邊緣 + 連續 Voronoi 龜裂岩體 + 碎石崩解

    private static float BasaltSlab(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        int edgeSeed = upper ? 211 : 317;

        // 巨劍厚重寬幅基底
        float baseW = Mathf.Lerp(0.41f, 0.23f, Mathf.Pow(u, 0.85f));

        // 階梯狀岩層斷口（量化成硬朗的平台階梯，而非平滑波浪）
        float stepCoord = u * 9f + (upper ? 0.0f : 0.35f);
        int stepIdx = Mathf.FloorToInt(stepCoord);
        float stepFrac = SmoothStep(0.0f, 0.16f, stepCoord - stepIdx);
        float stepH = Mathf.Lerp(Hash(stepIdx, edgeSeed), Hash(stepIdx + 1, edgeSeed), stepFrac);
        float cragLedge = (stepH - 0.45f) * 0.11f;

        // 次級多邊形石角凸起
        float subPhase = u * 17f + (upper ? 0.2f : 0.7f);
        float subBlock = SmoothStep(0.08f, 0.20f, subPhase - Mathf.Floor(subPhase))
                       * (1f - SmoothStep(0.76f, 0.90f, subPhase - Mathf.Floor(subPhase)));
        float subJut = subBlock * Mathf.Lerp(0.02f, 0.07f, Hash(Mathf.FloorToInt(subPhase), edgeSeed + 9));

        float halfW = baseW + cragLedge + subJut;

        // 硬邊岩體遮罩
        float body = 1f - SmoothStep(halfW - 0.015f, halfW + 0.004f, d);

        // 內部使用跨中心連續的 Seed (241)，讓玄武岩層理與 Voronoi 龜裂網格完整貫穿上下、無中心接縫
        float strata = Noise2(u * 4.5f, v * 22f, 223);
        float voronoiEdge = VoronoiCellEdge(u * 13f, v * 4.2f, 241);
        float crackDarken = SmoothStep(0.0f, 0.13f, voronoiEdge);
        // 實心厚重玄武岩本體：基礎 density 保持在 0.88 ~ 1.0，暗縫清晰而不掏空岩體
        float density = Mathf.Lerp(0.88f, 1.0f, strata) * Mathf.Lerp(0.72f, 1.0f, crackDarken);

        float a = body * density;
        // 刀頭快速展開，前中段厚實扎實，尾段自然崩解
        float lengthFade = 1f - SmoothStep(0.55f, 0.98f, u);
        a *= SmoothStep(0f, 0.02f, u) * lengthFade;
        // 刀尾以塊狀碎石崩解取代煙霧消散，前段保持厚實
        float erode = Mathf.Lerp(1.0f, VoronoiRockErode(u, v, 401, 0.34f, 14f, 4.5f), SmoothStep(0.30f, 0.85f, u));
        a *= erode;
        return Mathf.Clamp01(a);
    }

    #endregion

    // =====================================================================
    #region 3. 金珀礦脈核心 —— 呼應劍身中央的階梯狀黃褐色礦脈與完整收尖的分支裂隙

    private static float OchreVein(float u, float v, int w, int h)
    {
        const int segs = 9;
        float aspect = (float)w / h;
        Vector2 p = new Vector2(u * aspect, v);
        int k = Mathf.Min(Mathf.FloorToInt(u * segs), segs - 1);

        float minDist = float.MaxValue;
        float segMask = 1f;

        // 搜尋範圍涵蓋 k-2 ~ k+1，確保向後延伸的分支細礦脈完整收尖不被截斷
        for (int s = Mathf.Max(0, k - 2); s <= Mathf.Min(segs - 1, k + 1); s++)
        {
            Vector2 a0 = new Vector2((float)s / segs * aspect, VeinKnot(s));
            Vector2 a1 = new Vector2((float)(s + 1) / segs * aspect, VeinKnot(s + 1));
            float t;
            float ds = SegmentDistance(p, a0, a1, out t);
            if (ds < minDist)
            {
                minDist = ds;
                // 礦脈前中段連貫厚實，僅在刀尾 (s >= 6) 出現岩脈斷裂感
                float broken = (s >= 6 && Hash(s, 701) > 0.35f) ? 0.78f : 0f;
                float endFade = SmoothStep(0f, 0.22f, t) * SmoothStep(1f, 0.78f, t);
                segMask = 1f - broken * endFade;
            }
        }

        float th = Mathf.Lerp(0.135f, 0.042f, Mathf.Pow(u, 0.85f));
        float aa = 1.5f / h;
        float coreLine = 1f - SmoothStep(th * 0.50f - aa, th * 0.50f + aa, minDist);
        float innerRidge = Mathf.Exp(-Sq(minDist / (th * 0.28f))) * 0.85f;
        float glow = Mathf.Exp(-minDist / 0.075f) * 0.65f;

        // 獨立計算分支細礦脈（由節點向斜後方自然收尖淡出）
        float branchAlpha = 0f;
        for (int s = Mathf.Max(1, k - 2); s <= Mathf.Min(segs - 2, k + 1); s++)
        {
            if (Hash(s, 733) <= 0.35f) continue;
            Vector2 a0 = new Vector2((float)s / segs * aspect, VeinKnot(s));
            float branchDirY = (s % 2 == 0) ? 1f : -1f;
            Vector2 bEnd = a0 + new Vector2(0.58f, branchDirY * 0.18f);
            float tb;
            float db = SegmentDistance(p, a0, bEnd, out tb);
            float branchTh = Mathf.Lerp(0.048f, 0.005f, tb) * (1f - u * 0.45f);
            float bLine = (1f - SmoothStep(branchTh * 0.35f, branchTh + aa, db)) * (1f - SmoothStep(0.65f, 1.0f, tb));
            branchAlpha = Mathf.Max(branchAlpha, bLine * 0.95f);
        }

        float alpha = Mathf.Max((Mathf.Max(coreLine, glow) + innerRidge) * segMask, branchAlpha);
        float lengthFade = 1f - SmoothStep(0.60f, 0.98f, u);
        alpha *= SmoothStep(0f, 0.02f, u) * lengthFade;
        return Mathf.Clamp01(alpha);
    }

    /// <summary>
    /// 礦脈節點：交替產生「水平岩層段」與「斜切斷層階梯」，還原 Crag 劍身上的折線黃色礦脈造型
    /// </summary>
    private static float VeinKnot(int k)
    {
        if (k <= 0) return 0.50f;
        float[] pattern = { 0.50f, 0.36f, 0.39f, 0.62f, 0.58f, 0.35f, 0.41f, 0.60f, 0.48f, 0.50f };
        int idx = Mathf.Clamp(k, 0, pattern.Length - 1);
        return pattern[idx];
    }

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    #endregion

    // =====================================================================
    #region 4. 崩裂岩晶碎塊 (粒子貼圖) —— 不規則多邊形低模礦石 + 三切面明暗與稜線高光

    private static float RockChunk(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2(u * 2f - 1f, v * 2f - 1f);
        float aa = 3.0f / w;

        // 以 7 個半平面交集切割出不規則低多邊形 (Low-Poly) 岩石外輪廓
        Vector2[] normals =
        {
            new Vector2( 0.00f,  1.00f),
            new Vector2( 0.82f,  0.57f),
            new Vector2( 0.96f, -0.28f),
            new Vector2( 0.38f, -0.92f),
            new Vector2(-0.55f, -0.83f),
            new Vector2(-0.98f, -0.18f),
            new Vector2(-0.74f,  0.67f)
        };
        float[] offsets = { 0.78f, 0.74f, 0.70f, 0.76f, 0.72f, 0.75f, 0.73f };

        float maxEdgeDist = -10f;
        for (int i = 0; i < normals.Length; i++)
        {
            float d = Vector2.Dot(p, normals[i]) - offsets[i];
            if (d > maxEdgeDist) maxEdgeDist = d;
        }

        // 外輪廓遮罩 (maxEdgeDist <= 0 為岩塊內部)
        float inside = 1f - SmoothStep(-aa, aa, maxEdgeDist);
        if (inside <= 0.001f) return 0f;

        // 內部三切面分割（以中心點 (0.06, -0.04) 向三個方向延伸的 Y 字形石稜線）
        Vector2 apex = new Vector2(0.06f, -0.04f);
        Vector2 rel = p - apex;
        float angle = Mathf.Atan2(rel.y, rel.x); // -pi ~ pi

        // 三個切面各自具備不同明暗度，直接呈現 Low-Poly 3D 礦石切面立體感
        float facetShade;
        if (angle > 0.35f && angle <= 2.55f)
            facetShade = 0.94f; // 左上受光切面（亮金珀色）
        else if (angle <= 0.35f && angle > -1.75f)
            facetShade = 0.72f; // 右側中調切面
        else
            facetShade = 0.54f; // 左下背光切面

        // 計算到三條內部稜線的距離，繪製高光石稜線
        Vector2 r1 = new Vector2(Mathf.Cos(0.35f), Mathf.Sin(0.35f));
        Vector2 r2 = new Vector2(Mathf.Cos(2.55f), Mathf.Sin(2.55f));
        Vector2 r3 = new Vector2(Mathf.Cos(-1.75f), Mathf.Sin(-1.75f));

        float dRidge = Mathf.Min(
            RayDistance(rel, r1),
            Mathf.Min(RayDistance(rel, r2), RayDistance(rel, r3)));

        float ridgeHighlight = Mathf.Exp(-Sq(dRidge / 0.055f)) * 0.42f;
        float outerRim = Mathf.Exp(-Sq((maxEdgeDist + 0.03f) / 0.045f)) * 0.35f;

        float val = Mathf.Clamp01((facetShade + ridgeHighlight + outerRim) * inside);
        return val;
    }

    private static float RayDistance(Vector2 p, Vector2 dir)
    {
        float proj = Mathf.Max(0f, Vector2.Dot(p, dir));
        return Vector2.Distance(p, dir * proj);
    }

    #endregion

    // =====================================================================
    #region 共用工具與地質噪聲

    /// <summary>
    /// Voronoi 多邊形岩塊侵蝕：刀尾崩解為硬邊多邊形石塊，而非平滑煙霧
    /// </summary>
    private static float VoronoiRockErode(float u, float v, int seed, float start, float freqU, float freqV)
    {
        if (u <= start) return 1f;
        float progress = SmoothStep(start, 1f, u);
        float cellVal = VoronoiCellValue(u * freqU, v * freqV, seed);
        float fineNoise = Noise2(u * freqU * 2.2f, v * freqV * 2.2f, seed + 19);
        float rockMask = cellVal * 0.72f + fineNoise * 0.28f;
        float threshold = progress * 0.90f;
        return SmoothStep(threshold - 0.045f, threshold + 0.045f, rockMask);
    }

    /// <summary>回傳每個 Voronoi 晶胞的獨立固定隨機值（形成塊狀岩石分佈）</summary>
    private static float VoronoiCellValue(float x, float y, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float minDist = 10f;
        int bestX = ix, bestY = iy;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = ix + dx, cy = iy + dy;
                float px = cx + Hash2(cx, cy, seed) * 0.8f + 0.1f;
                float py = cy + Hash2(cx, cy, seed + 7) * 0.8f + 0.1f;
                float d = Sq(x - px) + Sq(y - py);
                if (d < minDist)
                {
                    minDist = d;
                    bestX = cx;
                    bestY = cy;
                }
            }
        }
        return Hash2(bestX, bestY, seed + 31);
    }

    /// <summary>回傳到 Voronoi 晶胞邊界（岩石裂縫）的距離</summary>
    private static float VoronoiCellEdge(float x, float y, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float d1 = 10f, d2 = 10f;
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int cx = ix + dx, cy = iy + dy;
                float px = cx + Hash2(cx, cy, seed) * 0.8f + 0.1f;
                float py = cy + Hash2(cx, cy, seed + 11) * 0.8f + 0.1f;
                float d = Mathf.Sqrt(Sq(x - px) + Sq(y - py));
                if (d < d1) { d2 = d1; d1 = d; }
                else if (d < d2) { d2 = d; }
            }
        }
        return d2 - d1;
    }

    private static float GravelStreaks(float u, float v, int h, int seed, int count, float strength)
    {
        float a = 0f;
        for (int i = 0; i < count; i++)
        {
            float vk = Mathf.Lerp(0.14f, 0.86f, Hash(i, seed));
            float u0 = Mathf.Lerp(0.12f, 0.58f, Hash(i, seed + 1));
            float u1 = u0 + Mathf.Lerp(0.08f, 0.28f, Hash(i, seed + 2));
            if (u < u0 || u > u1) continue;
            float along = (u - u0) / (u1 - u0);
            float line = Mathf.Exp(-Sq((v - vk) * h / 1.15f));
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

        // 黑底預覽圖（存到專案 Temp/CragPreview，方便檢視造型）
        var preview = new Color32[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte a = pixels[i].a;
            preview[i] = new Color32(a, a, a, 255);
        }
        tex.SetPixels32(preview);
        tex.Apply();
        string previewDir = Path.Combine(Application.dataPath, "../Temp/CragPreview");
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
