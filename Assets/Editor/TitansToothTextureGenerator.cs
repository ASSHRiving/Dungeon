#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 泰坦之牙 (TitansTooth) 專屬『碎岩裂隙』刀光與碎石粒子程序化貼圖生成器
/// 風格定調：沉穩暗沉的遠古厚重岩刃，融入豐富的『天然土黃色／泥岩沉積 (Earthy Ochre)』地質元素：
///   1. Tex_Slash_Crag.png (1024x256, RGBA32)：
///      - 針對泰坦之牙不規則缺口的粗獷岩骨巨刃，結合多尺度 Voronoi 裂紋雜訊 (Voronoi Fracture Fissures)、
///        不對稱巨獸獠牙崩口邊緣 (Torn Crag-Bone Saw-Teeth) 與高頻方向性鋸齒刷痕 (Sawtooth Brush Streaks)。
///      - 降低整體過曝白光，整體色相轉為厚重暗沉的『泥褐深岩 -> 沉穩土黃礦岩 -> 古樸象牙砂黃亮邊』，
///        後緣呈鋸齒刷痕與 Voronoi 碎岩崩解消散。
///   2. Tex_Particle_RockShard.png (256x256, RGBA32)：
///      - 不規則多邊形菱形碎岩貼圖，呈現立體土黃石塊切面、暗褐泥岩底面與砂黃石稜紋理。
///   3. Tex_Slash_Shockwave.png (512x128, RGBA32)：
///      - 外側重擊殘影／寬幅破空氣流貼圖，呈現沉重微透明的黃土泥塵破空壓浪。
///   4. Tex_Slash_Spine.png (512x64, RGBA32)：
///      - 能量撕裂亮邊／脊骨裂核貼圖，呈現暗沉沉穩的土黃色折線地裂裂隙。
/// </summary>
public static class TitansToothTextureGenerator
{
    public const string Folder = "Assets/VFX/TitansTooth";
    public const string CragSlashPath = Folder + "/Tex_Slash_Crag.png";
    public const string RockShardPath = Folder + "/Tex_Particle_RockShard.png";
    public const string ShockwavePath = Folder + "/Tex_Slash_Shockwave.png";
    public const string SpinePath = Folder + "/Tex_Slash_Spine.png";

    private delegate Color PixelColorFunc(float u, float v, int w, int h);

    [MenuItem("Tools/VFX/TitansTooth/Regenerate Textures")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder("Assets/VFX"))
            AssetDatabase.CreateFolder("Assets", "VFX");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/VFX", "TitansTooth");

        SaveColor(CragSlashPath, 1024, 256, EvalSlashCrag, clamp: true);
        SaveColor(RockShardPath, 256, 256, EvalRockShard, clamp: true);
        SaveColor(ShockwavePath, 512, 128, EvalShockwave, clamp: true);
        SaveColor(SpinePath, 512, 64, EvalSpineCrack, clamp: true);

        Debug.Log("<color=#B89758>[TitansTooth VFX]</color> 已生成泰坦之牙專屬貼圖（暗沉沉穩土黃礦岩風格）：Tex_Slash_Crag.png、Tex_Particle_RockShard.png、Tex_Slash_Shockwave.png、Tex_Slash_Spine.png。");
    }

    // =====================================================================
    #region 1. 主刀光貼圖 Tex_Slash_Crag.png —— 撕裂鋸齒邊緣 + Voronoi 岩石崩裂紋理 + 土黃色岩質

    private static Color EvalSlashCrag(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        float aa = 1.5f / h;

        float taper = Mathf.Lerp(1.0f, 0.34f, Mathf.Pow(u, 0.82f));

        // 1) 撕裂鋸齒邊緣：7 組粗獷不規則岩骨崩口與倒刺缺口
        const float toothCount = 7.0f;
        float toothCoord = u * toothCount + (upper ? 0.0f : 0.37f);
        int toothIdx = Mathf.FloorToInt(toothCoord);
        float s = toothCoord - toothIdx;

        float mainTooth = TornCragBoneTooth(s, toothIdx, upper ? 29 : 67);

        // 次級微缺口
        float microPhase = u * 23f + (upper ? 0.15f : 0.63f);
        int microIdx = Mathf.FloorToInt(microPhase);
        float microFrac = microPhase - microIdx;
        float microSaw = (1f - microFrac) * Mathf.Lerp(0.015f, 0.052f, Hash(microIdx, upper ? 101 : 139));

        float halfW = (0.09f + 0.28f * mainTooth + microSaw) * taper;

        // 2) 鋸齒刷痕
        float brushSaw = SawtoothBrushPattern(u, v, upper ? 173 : 229);
        float streakExtend = brushSaw * Mathf.Lerp(0.02f, 0.11f, SmoothStep(0.18f, 0.88f, u)) * taper;
        float effectiveHalfW = halfW + streakExtend * 0.45f;

        float body = 1f - SmoothStep(effectiveHalfW - aa, effectiveHalfW + aa, d);

        // 3) 多尺度 Voronoi 岩石崩裂紋理
        float warpU = (Noise2(u * 10f, v * 10f, 307) - 0.5f) * 0.035f;
        float warpV = (Noise2(u * 10f, v * 10f, 331) - 0.5f) * 0.035f;

        float vEdgePrimary = VoronoiCellEdge((u + warpU) * 13.5f, (v + warpV) * 4.2f, 409);
        float vEdgeSecondary = VoronoiCellEdge((u - warpU * 0.6f) * 27f, (v + warpV * 0.6f) * 8.5f, 461);
        float vCellVal = VoronoiCellValue((u + warpU) * 13.5f, (v + warpV) * 4.2f, 409);

        float primaryCrack = 1f - SmoothStep(0.008f, 0.075f, vEdgePrimary);
        float secondaryCrack = (1f - SmoothStep(0.005f, 0.055f, vEdgeSecondary)) * 0.65f;
        float crackMask = Mathf.Clamp01(primaryCrack + secondaryCrack);

        float rockStrata = Noise2(u * 8.5f + d * 3.5f, v * 26f, 521);
        float brushFiber = Mathf.Lerp(0.62f, 1.0f, brushSaw);

        // 4) 前緣與外刃能量撕裂亮邊（暗沉收斂，避免刺眼過曝死白）
        float outerTearRim = Mathf.Exp(-Sq((d - halfW * 0.88f) / (0.022f * taper + aa))) * body;
        float sharpCutEdge = SmoothStep(halfW * 0.62f, halfW * 0.96f, d) * body;
        float leadingFront = Mathf.Exp(-u / 0.18f) * body;
        float spineCore = Mathf.Exp(-Sq(d / (0.048f * taper))) * body;

        float energyHighlight = Mathf.Clamp01(
            outerTearRim * 1.05f +
            sharpCutEdge * 0.52f +
            leadingFront * 0.75f +
            spineCore * 0.68f +
            crackMask * Mathf.Lerp(0.85f, 0.30f, u) * body);

        // 5) 刀尾與後緣消散
        float tailErode = VoronoiSawtoothErode(u, v, brushSaw, vCellVal, 613);
        float headAttack = SmoothStep(0.0f, 0.022f, u);
        float lengthEnvelope = 1f - SmoothStep(0.58f, 0.99f, u);
        float flyingStreaks = GravelSlashStreaks(u, v, h, 677, 12, 0.72f) * (1f - body * 0.65f);

        float innerDensity = body * Mathf.Lerp(0.78f, 1.0f, rockStrata * brushFiber);
        float crackAlphaMod = Mathf.Lerp(1.0f + crackMask * 0.25f, 1.0f - crackMask * 0.65f, SmoothStep(0.30f, 0.85f, u));
        float rawAlpha = Mathf.Max(innerDensity * crackAlphaMod + outerTearRim * 0.45f + spineCore * 0.35f, flyingStreaks);
        float alpha = Mathf.Clamp01(rawAlpha * headAttack * lengthEnvelope * tailErode);

        // 6) 暗沉沉穩的【土黃色系 (Earthy Ochre)】地質漸層（無刺眼白光，無火紅橘光）：
        Color colDeepEarth = new Color(0.20f, 0.17f, 0.13f);      // 暗沉泥岩黑褐底色（暗部與後緣）
        Color colRawOchre = new Color(0.48f, 0.38f, 0.22f);       // 沉穩原生土黃岩質（主體岩身）
        Color colWarmSandstone = new Color(0.68f, 0.56f, 0.35f);  // 粗獷砂岩土黃層（受光中調）
        Color colMutedOchreHighlight = new Color(0.86f, 0.78f, 0.58f); // 古樸象牙砂黃亮邊（前緣高光，暗沉而不刺眼）

        float rockToneBlend = Mathf.Clamp01(
            (1f - u * 0.72f) * 0.58f +
            sharpCutEdge * 0.42f +
            vCellVal * 0.22f +
            brushSaw * 0.18f -
            (1f - rockStrata) * 0.25f);

        Color baseGrad = Color.Lerp(colDeepEarth, colRawOchre, SmoothStep(0.0f, 0.42f, rockToneBlend));
        baseGrad = Color.Lerp(baseGrad, colWarmSandstone, SmoothStep(0.30f, 0.82f, rockToneBlend));

        // 疊加古樸土黃亮邊（最高亮度維持在柔和沉穩的 0.86，不超亮過曝）
        Color finalRgb = Color.Lerp(baseGrad, colMutedOchreHighlight, SmoothStep(0.20f, 0.90f, energyHighlight) * 0.85f);

        return new Color(finalRgb.r, finalRgb.g, finalRgb.b, alpha);
    }

    private static float TornCragBoneTooth(float s, int idx, int seed)
    {
        float peakH = Mathf.Lerp(0.55f, 1.0f, Hash(idx, seed));
        float skew = Mathf.Lerp(0.14f, 0.32f, Hash(idx, seed + 3));

        float rise = Mathf.Clamp01((s - 0.01f) / skew);
        float fall = Mathf.Clamp01((0.96f - s) / (0.96f - skew));
        float sawShape = Mathf.Min(rise, Mathf.Pow(fall, 0.78f)) * peakH;

        float chip1Pos = Mathf.Lerp(0.28f, 0.52f, Hash(idx, seed + 7));
        float chip1Depth = Hash(idx, seed + 11) > 0.22f ? 0.42f : 0.0f;
        float notch1 = 1f - chip1Depth * Mathf.Clamp01(1f - Mathf.Abs(s - chip1Pos) / 0.075f);

        float chip2Pos = Mathf.Lerp(0.58f, 0.80f, Hash(idx, seed + 17));
        float chip2Depth = Hash(idx, seed + 23) > 0.35f ? 0.35f : 0.0f;
        float notch2 = 1f - chip2Depth * Mathf.Clamp01(1f - Mathf.Abs(s - chip2Pos) / 0.065f);

        return sawShape * notch1 * notch2;
    }

    private static float SawtoothBrushPattern(float u, float v, int seed)
    {
        float d = Mathf.Abs(v - 0.5f);
        float coord1 = d * 38f - u * 4.2f + Hash(Mathf.FloorToInt(d * 38f), seed) * 0.5f;
        float saw1 = 1f - Mathf.Abs(Frac(coord1) * 2f - 1f);
        saw1 = Mathf.Pow(saw1, 1.6f);

        float coord2 = d * 72f - u * 7.5f + Noise2(u * 6f, v * 18f, seed + 19) * 0.8f;
        float saw2 = 1f - Mathf.Abs(Frac(coord2) * 2f - 1f);

        int stripeIdx = Mathf.FloorToInt(d * 38f);
        float stripeReach = Mathf.Lerp(0.35f, 0.98f, Hash(stripeIdx, seed + 41));
        float stripeMask = 1f - SmoothStep(stripeReach - 0.12f, stripeReach + 0.04f, u);

        return Mathf.Clamp01((saw1 * 0.68f + saw2 * 0.32f) * stripeMask);
    }

    private static float VoronoiSawtoothErode(float u, float v, float brushSaw, float voronoiCell, int seed)
    {
        if (u <= 0.25f) return 1f;
        float progress = SmoothStep(0.25f, 0.96f, u);
        float fineCrag = Noise2(u * 28f, v * 16f, seed);
        float composite = voronoiCell * 0.50f + brushSaw * 0.35f + fineCrag * 0.15f;
        float threshold = progress * 0.88f;
        return SmoothStep(threshold - 0.04f, threshold + 0.04f, composite);
    }

    #endregion

    // =====================================================================
    #region 2. 碎岩／土黃岩粒貼圖 Tex_Particle_RockShard.png (256x256, RGBA32)

    private static Color EvalRockShard(float u, float v, int w, int h)
    {
        Vector2 p = new Vector2((u * 2f - 1f) * 1.12f, (v * 2f - 1f) * 0.95f);
        float aa = 2.5f / w;

        Vector2[] normals =
        {
            new Vector2( 0.85f,  0.52f),
            new Vector2( 0.96f, -0.28f),
            new Vector2( 0.42f, -0.91f),
            new Vector2(-0.46f, -0.89f),
            new Vector2(-0.97f, -0.24f),
            new Vector2(-0.82f,  0.57f),
            new Vector2( 0.05f,  0.99f)
        };
        float[] offsets = { 0.68f, 0.64f, 0.72f, 0.69f, 0.65f, 0.70f, 0.74f };

        float edgeChip = (Noise2(u * 14f, v * 14f, 809) - 0.5f) * 0.055f;

        float maxEdgeDist = -10f;
        for (int i = 0; i < normals.Length; i++)
        {
            float d = Vector2.Dot(p, normals[i]) - (offsets[i] + edgeChip);
            if (d > maxEdgeDist) maxEdgeDist = d;
        }

        float inside = 1f - SmoothStep(-aa, aa, maxEdgeDist);
        if (inside <= 0.001f) return new Color(0f, 0f, 0f, 0f);

        Vector2 apex = new Vector2(0.05f, -0.03f);
        Vector2 rel = p - apex;
        float angle = Mathf.Atan2(rel.y, rel.x);

        float facetLight;
        if (angle > 0.25f && angle <= 2.45f)
            facetLight = 0.92f; // 受光切面（砂質土黃岩面）
        else if (angle <= 0.25f && angle > -1.80f)
            facetLight = 0.58f; // 中調切面（深褐土岩面）
        else
            facetLight = 0.28f; // 背光切面（黑褐泥岩暗面）

        Vector2 r1 = new Vector2(Mathf.Cos(0.25f), Mathf.Sin(0.25f));
        Vector2 r2 = new Vector2(Mathf.Cos(2.45f), Mathf.Sin(2.45f));
        Vector2 r3 = new Vector2(Mathf.Cos(-1.80f), Mathf.Sin(-1.80f));
        float dRidge = Mathf.Min(RayDistance(rel, r1), Mathf.Min(RayDistance(rel, r2), RayDistance(rel, r3)));

        float vCrackEdge = VoronoiCellEdge(u * 5.5f, v * 5.5f, 853);
        float stoneCrack = 1f - SmoothStep(0.01f, 0.08f, vCrackEdge);
        float rockGrain = Noise2(u * 18f, v * 18f, 887);

        float ridgeGlow = Mathf.Exp(-Sq(dRidge / 0.055f));
        float rimHighlight = SmoothStep(-0.14f, -0.01f, maxEdgeDist) * SmoothStep(-0.2f, 0.7f, p.x + p.y * 0.6f);

        // 配色：暗沉黑褐泥岩 + 沉穩土黃礦岩 + 砂黃切面 + 象牙砂白高光
        Color colDeepMud = new Color(0.20f, 0.17f, 0.13f);
        Color colEarthOchre = new Color(0.50f, 0.40f, 0.24f);
        Color colSandstone = new Color(0.70f, 0.58f, 0.38f);
        Color colEdgeOchre = new Color(0.85f, 0.78f, 0.60f);

        float rockTone = Mathf.Clamp01(facetLight * Mathf.Lerp(0.82f, 1.12f, rockGrain));
        Color rockCol = Color.Lerp(colDeepMud, colEarthOchre, SmoothStep(0.20f, 0.75f, rockTone));
        rockCol = Color.Lerp(rockCol, colSandstone, SmoothStep(0.68f, 0.98f, rockTone));

        float highlightStrength = Mathf.Clamp01(ridgeGlow * 0.80f + stoneCrack * 0.70f + rimHighlight * 0.65f);
        Color finalRgb = Color.Lerp(rockCol, colSandstone, SmoothStep(0.15f, 0.65f, highlightStrength));
        finalRgb = Color.Lerp(finalRgb, colEdgeOchre, SmoothStep(0.55f, 0.95f, highlightStrength) * 0.85f);

        return new Color(finalRgb.r, finalRgb.g, finalRgb.b, inside);
    }

    #endregion

    // =====================================================================
    #region 3. 重擊殘影／衝擊波層貼圖 Tex_Slash_Shockwave.png (512x128, RGBA32)

    private static Color EvalShockwave(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        int seed = upper ? 911 : 947;

        float outerW = Mathf.Lerp(0.46f, 0.28f, Mathf.Pow(u, 0.75f));
        float sawMod = SawtoothBrushPattern(u * 0.85f, v, seed) * 0.06f;
        float halfW = outerW + sawMod;

        float outerMask = 1f - SmoothStep(halfW - 0.035f, halfW + 0.015f, d);
        float outerWaveRing = Mathf.Exp(-Sq((d - halfW * 0.82f) / 0.085f));
        float secondWaveRing = Mathf.Exp(-Sq((d - halfW * 0.52f) / 0.065f)) * 0.65f;

        float windShear = Noise2(u * 6.5f - d * 4f, v * 28f, 977);
        float voronoiWake = VoronoiCellValue(u * 10f, v * 4f, 991);
        float tailErode = Mathf.Lerp(1.0f, SmoothStep(u * 0.85f - 0.1f, u * 0.85f + 0.15f, windShear * 0.6f + voronoiWake * 0.4f), SmoothStep(0.25f, 0.95f, u));

        float headIn = SmoothStep(0.0f, 0.03f, u);
        float tailOut = 1f - SmoothStep(0.50f, 0.99f, u);

        float alpha = Mathf.Clamp01((outerMask * 0.42f + outerWaveRing * 0.46f + secondWaveRing * 0.25f)
                                    * Mathf.Lerp(0.70f, 1.0f, windShear) * headIn * tailOut * tailErode);

        // 氣流色彩：暗褐岩土氣浪過渡至微暗沉土黃塵色
        Color colDeepEarth = new Color(0.22f, 0.19f, 0.15f);
        Color colEarthyDust = new Color(0.58f, 0.48f, 0.32f);
        Color rgb = Color.Lerp(colDeepEarth, colEarthyDust, Mathf.Clamp01(outerWaveRing * 0.85f + (1f - u) * 0.35f));

        return new Color(rgb.r, rgb.g, rgb.b, alpha);
    }

    #endregion

    // =====================================================================
    #region 4. 能量撕裂亮邊脊核貼圖 Tex_Slash_Spine.png (512x64, RGBA32)

    private static Color EvalSpineCrack(float u, float v, int w, int h)
    {
        const int segs = 10;
        float aspect = (float)w / h;
        Vector2 p = new Vector2(u * aspect, v);
        int k = Mathf.Min(Mathf.FloorToInt(u * segs), segs - 1);

        float minDist = float.MaxValue;
        for (int s = Mathf.Max(0, k - 2); s <= Mathf.Min(segs - 1, k + 1); s++)
        {
            Vector2 a0 = new Vector2((float)s / segs * aspect, SpineKnot(s));
            Vector2 a1 = new Vector2((float)(s + 1) / segs * aspect, SpineKnot(s + 1));
            float t;
            float ds = SegmentDistance(p, a0, a1, out t);
            if (ds < minDist) minDist = ds;
        }

        float th = Mathf.Lerp(0.14f, 0.035f, Mathf.Pow(u, 0.80f));
        float aa = 1.5f / h;
        float coreLine = 1f - SmoothStep(th * 0.48f - aa, th * 0.48f + aa, minDist);
        float glow = Mathf.Exp(-minDist / 0.070f) * 0.65f;

        float branchAlpha = 0f;
        for (int s = Mathf.Max(1, k - 2); s <= Mathf.Min(segs - 2, k + 1); s++)
        {
            if (Hash(s, 733) <= 0.30f) continue;
            Vector2 a0 = new Vector2((float)s / segs * aspect, SpineKnot(s));
            float dirY = (s % 2 == 0) ? 1f : -1f;
            Vector2 bEnd = a0 + new Vector2(0.55f, dirY * 0.20f);
            float tb;
            float db = SegmentDistance(p, a0, bEnd, out tb);
            float bTh = Mathf.Lerp(0.050f, 0.005f, tb) * (1f - u * 0.45f);
            float bLine = (1f - SmoothStep(bTh * 0.35f, bTh + aa, db)) * (1f - SmoothStep(0.65f, 1.0f, tb));
            branchAlpha = Mathf.Max(branchAlpha, bLine * 0.92f);
        }

        float alpha = Mathf.Max(coreLine + glow * 0.5f, branchAlpha);
        alpha *= SmoothStep(0f, 0.02f, u) * (1f - SmoothStep(0.58f, 0.98f, u));
        alpha = Mathf.Clamp01(alpha);

        // 沉穩土黃礦脈裂紋（柔和古樸，非刺眼熾白）
        Color colCore = Color.Lerp(new Color(0.68f, 0.55f, 0.35f), new Color(0.86f, 0.78f, 0.58f), coreLine);
        return new Color(colCore.r, colCore.g, colCore.b, alpha);
    }

    private static float SpineKnot(int k)
    {
        float[] pattern = { 0.50f, 0.38f, 0.58f, 0.35f, 0.62f, 0.40f, 0.57f, 0.39f, 0.54f, 0.47f, 0.50f };
        return pattern[Mathf.Clamp(k, 0, pattern.Length - 1)];
    }

    #endregion

    // =====================================================================
    #region 數學與程序化雜訊工具

    private static float GravelSlashStreaks(float u, float v, int h, int seed, int count, float strength)
    {
        float a = 0f;
        for (int i = 0; i < count; i++)
        {
            float vk = Mathf.Lerp(0.10f, 0.90f, Hash(i, seed));
            float u0 = Mathf.Lerp(0.10f, 0.62f, Hash(i, seed + 1));
            float u1 = u0 + Mathf.Lerp(0.10f, 0.32f, Hash(i, seed + 2));
            if (u < u0 || u > u1) continue;
            float along = (u - u0) / (u1 - u0);
            float line = Mathf.Exp(-Sq((v - vk) * h / 1.35f));
            a = Mathf.Max(a, line * Mathf.Sin(along * Mathf.PI) * strength);
        }
        return a;
    }

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

    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b, out float t)
    {
        Vector2 ab = b - a;
        t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private static float RayDistance(Vector2 p, Vector2 dir)
    {
        float proj = Mathf.Max(0f, Vector2.Dot(p, dir));
        return Vector2.Distance(p, dir * proj);
    }

    private static void SaveColor(string path, int w, int h, PixelColorFunc func, bool clamp = true)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w;
                Color c = func(u, v, w, h);
                byte r = (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f);
                byte g = (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f);
                byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f);
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f);
                pixels[y * w + x] = new Color32(r, g, b, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
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
    private static float Frac(float x) => x - Mathf.Floor(x);

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
