#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 破曉 (Daybreak) 專屬輕型單手劍刀光貼圖程序化生成器
/// 視覺語言取自 Daybreak 武器外觀（黑曜暗脊 + 金色後掠羽齒刃 + 護手紫紅晨星寶石）與「輕盈、疾速、破曉晨光」定位：
///   1. 破曉後掠羽刃 (`Tex_Daybreak_Feather_Edge`)：流線型後掠羽翼鋸齒與破風流線槽，前中段銳利飽滿、尾段化作疾風細絲消散
///   2. 黑曜暮影風帶 (`Tex_Daybreak_Dusk_Veil`)：呼應劍背黑曜暗脊，以多道鏤空流線風帶取代笨重實心岩塊，兼具高對比襯底與輕盈速度感
///   3. 破曉晨曦極光 (`Tex_Daybreak_Solar_Ray`)：纖細銳利的雙軌破曉光束與晨星稜鏡節點，勾勒出單手劍極速揮砍的破空閃光
///   4. 破曉羽芒星屑 (`Tex_Daybreak_DawnSpark`)：修長流線菱羽四芒星粒子，隨揮砍輕盈飄散
///
/// UV 約定（TrailRenderer Stretch 模式）：
///   U = 沿刀光長度，0 = 刀頭 (最新)，1 = 刀尾 (最舊)
///   V = 刀光寬度方向，圖案以 V=0.5 為中心上下近似對稱（避免左右揮砍時 UV 翻轉造成外觀不一致）
/// </summary>
public static class DaybreakTextureGenerator
{
    public const string Folder = "Assets/VFX/Daybreak";
    public const string VeilPath = Folder + "/Tex_Daybreak_Dusk_Veil.png";
    public const string EdgePath = Folder + "/Tex_Daybreak_Feather_Edge.png";
    public const string RayPath = Folder + "/Tex_Daybreak_Solar_Ray.png";
    public const string SparkPath = Folder + "/Tex_Daybreak_DawnSpark.png";

    [MenuItem("Tools/VFX/Daybreak/Regenerate Textures")]
    public static void GenerateAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/VFX", "Daybreak");

        Save(VeilPath, 512, 128, DuskVeil);
        Save(EdgePath, 512, 128, FeatherEdge);
        Save(RayPath, 512, 64, SolarRay);
        Save(SparkPath, 128, 128, DawnSpark, clamp: true);

        Debug.Log("<color=#FFE066>[Daybreak VFX]</color> 已生成 4 張破曉 (Daybreak) 專屬輕盈疾風刀光貼圖。");
    }

    // =====================================================================
    #region 1. 破曉後掠羽刃 (主刀光) —— 流線後掠羽翼刃齒 + 破風流線血槽 + 疾風細絲收尾

    private static float FeatherEdge(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        float aa = 1.5f / h;

        // 輕型單手劍修長比例：刀頭俐落展開，往刀尾流線收尖
        float taper = Mathf.Lerp(1.0f, 0.16f, Mathf.Pow(u, 0.78f));

        // 呼應 Daybreak 劍刃的 4~5 段後掠式金色羽齒 (Swept-back Feather Serrations)
        const float barbs = 5.0f;
        float phase = u * barbs + (upper ? 0.0f : 0.35f);
        int barbIdx = Mathf.FloorToInt(phase);
        float s = phase - barbIdx;

        float barb = SweptFeatherBarb(s, barbIdx, upper ? 23 : 67);
        float halfW = (0.055f + 0.24f * barb) * taper;

        // 銳利流線刃體與高亮破風外鋒線
        float body = 1f - SmoothStep(halfW - aa, halfW + aa, d);
        float outerRim = Mathf.Exp(-Sq((d - halfW) / (0.010f + aa))) * 0.95f;
        float innerBevel = SmoothStep(halfW - 0.055f * taper, halfW - 0.006f, d) * body;
        float coreSpine = Mathf.Exp(-Sq(d / (0.028f * taper))) * 0.90f;

        // 內部破風流線槽（鏤空感速度線，讓刀光呈現輕盈羽翼與疾風層次，而非死板色塊）
        float windGroove1 = Mathf.Exp(-Sq((d - halfW * 0.46f) / (0.012f * taper + aa)));
        float windGroove2 = Mathf.Exp(-Sq((d - halfW * 0.72f) / (0.009f * taper + aa)));
        float grooveCarve = Mathf.Clamp01((windGroove1 * 0.55f + windGroove2 * 0.45f) * SmoothStep(0.08f, 0.35f, u));

        // 橫向高速拉絲紋理
        float windStreak = Mathf.Lerp(0.88f, 1.04f, Noise2(u * 6f, v * 36f, 101));

        // 前中段保持高達 0.92~1.0 的清晰度，同時透過流線槽維持輕盈通透感
        float inner = body * windStreak * (1f - grooveCarve * 0.52f) + innerBevel * 0.45f + coreSpine * 0.65f;
        float a = Mathf.Max(inner, outerRim * (body + 0.35f));

        // 外側高速氣流細絲
        a = Mathf.Max(a, WindFilaments(u, v, h, 131, 12, 0.72f) * (1f - body * 0.5f));

        // 刀頭瞬間切入，前中段 (u=0~0.50) 清晰飽滿，後段 (u>0.50) 順暢化為疾風細絲消散
        float lengthFade = 1f - SmoothStep(0.48f, 0.96f, u);
        a *= SmoothStep(0f, 0.018f, u) * lengthFade;

        // 尾段流線風絲侵蝕（沿 U 方向拉長的高速氣流條紋消散，絕不產生笨重碎塊）
        float erode = Mathf.Lerp(1.0f, WindStreamErode(u, v, 173, 0.38f), SmoothStep(0.35f, 0.88f, u));
        a *= erode;
        return Mathf.Clamp01(a);
    }

    /// <summary>
    /// 後掠式羽翼刃齒：緩斜向後揚起、末端銳利收尖並帶內凹弧線，重現 Daybreak 金色鋸齒刃的流線輪廓
    /// </summary>
    private static float SweptFeatherBarb(float s, int idx, int seed)
    {
        float peak = Mathf.Lerp(0.72f, 1.0f, Hash(idx, seed));
        // 前緣平滑流線揚起 (0 -> 0.68)，後緣俐落內凹斜切 (0.68 -> 0.96)
        float rise = SmoothStep(0.0f, 0.68f, s);
        float riseCurve = Mathf.Pow(rise, 0.75f);
        float cutBack = 1f - SmoothStep(0.68f, 0.95f, s);
        // 內凹破風弧度
        float concave = Mathf.Pow(cutBack, 1.65f);
        return riseCurve * concave * peak;
    }

    #endregion

    // =====================================================================
    #region 2. 黑曜暮影風帶 (輕盈暗色襯底) —— 多道流線破風暗帶 + 呼應劍背黑曜折線

    private static float DuskVeil(float u, float v, int w, int h)
    {
        float d = Mathf.Abs(v - 0.5f);
        bool upper = v >= 0.5f;
        float aa = 1.5f / h;

        // 較窄且流線的收束比例，避免巨劍般的厚重寬幅
        float baseW = Mathf.Lerp(0.32f, 0.09f, Mathf.Pow(u, 0.80f));

        // 呼應劍背黑曜護脊的流線階梯折角（幅度輕巧）
        float spineStep = u * 7f + (upper ? 0.0f : 0.4f);
        int stepIdx = Mathf.FloorToInt(spineStep);
        float stepFrac = SmoothStep(0.0f, 0.25f, spineStep - stepIdx);
        float spineNotch = (Mathf.Lerp(Hash(stepIdx, 211), Hash(stepIdx + 1, 211), stepFrac) - 0.45f) * 0.055f;

        float halfW = Mathf.Max(0.04f, baseW + spineNotch);
        float envelope = 1f - SmoothStep(halfW - 0.018f, halfW + aa, d);

        // 將暗影襯底設計為 3 道交錯的「流線型破風緞帶」，中間保留呼吸感氣流縫隙，既有高對比度又不顯笨重
        float normD = d / Mathf.Max(0.001f, halfW);
        float ribbon1 = Mathf.Exp(-Sq((normD - 0.16f) / 0.22f)); // 內側主風帶
        float ribbon2 = Mathf.Exp(-Sq((normD - 0.58f) / 0.18f)); // 中層破風帶
        float ribbon3 = Mathf.Exp(-Sq((normD - 0.88f) / 0.12f)); // 外緣細風線

        float waveShift = Mathf.Sin(u * 14f + (upper ? 0f : 1.2f)) * 0.08f;
        float ribbons = Mathf.Clamp01(ribbon1 * 0.95f + ribbon2 * (0.85f + waveShift) + ribbon3 * 0.78f);

        // 高速橫向流線紋理
        float airGrain = Mathf.Lerp(0.82f, 1.0f, Noise2(u * 5f, v * 28f, 257));

        float a = envelope * ribbons * airGrain;
        float lengthFade = 1f - SmoothStep(0.45f, 0.94f, u);
        a *= SmoothStep(0f, 0.02f, u) * lengthFade;
        a *= Mathf.Lerp(1.0f, WindStreamErode(u, v, 293, 0.32f), SmoothStep(0.30f, 0.85f, u));
        return Mathf.Clamp01(a);
    }

    #endregion

    // =====================================================================
    #region 3. 破曉晨曦極光 (核心高光細芒) —— 雙軌交織破空細線 + 破曉晨星稜鏡節點

    private static float SolarRay(float u, float v, int w, int h)
    {
        float aa = 1.5f / h;
        float taper = Mathf.Lerp(1.0f, 0.22f, Mathf.Pow(u, 0.75f));

        // 中央極細銳利破曉主光線
        float dCenter = Mathf.Abs(v - 0.5f);
        float coreWidth = 0.042f * taper;
        float mainRay = 1f - SmoothStep(coreWidth - aa, coreWidth + aa, dCenter);
        float mainGlow = Mathf.Exp(-Sq(dCenter / (0.11f * taper))) * 0.80f;

        // 雙軌交織的流線副光絲（隨 U 呈輕盈正弦包絡交會，營造疾速刺破黑暗的動感）
        float waveOffset = Mathf.Sin(u * Mathf.PI * 3.5f) * 0.14f * taper;
        float dSub1 = Mathf.Abs((v - 0.5f) - waveOffset);
        float dSub2 = Mathf.Abs((v - 0.5f) + waveOffset * 0.75f);
        float subWidth = 0.022f * taper;
        float subRay1 = (1f - SmoothStep(subWidth - aa, subWidth + aa, dSub1)) * 0.88f;
        float subRay2 = (1f - SmoothStep(subWidth - aa, subWidth + aa, dSub2)) * 0.72f;

        // 沿光束分佈的 4 顆「破曉晨星」菱形閃光節點
        float[] starPos = { 0.10f, 0.26f, 0.44f, 0.62f };
        float starFlare = 0f;
        for (int i = 0; i < starPos.Length; i++)
        {
            float du = Mathf.Abs(u - starPos[i]) / 0.032f;
            float dv = dCenter / (0.22f * taper);
            // 菱形十字星芒包絡
            float diamond = Mathf.Clamp01(1f - (du + dv));
            float crossSpike = Mathf.Exp(-Sq(du * 2.2f)) * Mathf.Exp(-Sq(dv * 0.65f));
            starFlare = Mathf.Max(starFlare, (diamond * 0.9f + crossSpike * 0.75f) * (1f - i * 0.14f));
        }

        float a = Mathf.Max(Mathf.Max(mainRay, mainGlow), Mathf.Max(subRay1, subRay2));
        a = Mathf.Clamp01(a + starFlare * 0.85f);

        float lengthFade = 1f - SmoothStep(0.50f, 0.95f, u);
        a *= SmoothStep(0f, 0.015f, u) * lengthFade;
        return Mathf.Clamp01(a);
    }

    #endregion

    // =====================================================================
    #region 4. 破曉羽芒星屑 (粒子貼圖) —— 修長流線菱羽四芒星 + 晨光切面核心

    private static float DawnSpark(float u, float v, int w, int h)
    {
        // 以中心為原點 (-1 ~ 1)，橫向修長、縱向輕巧，形似破曉晨星與光羽
        float x = (u - 0.5f) * 2f;
        float y = (v - 0.5f) * 2f;
        float r = Mathf.Sqrt(x * x + y * y);

        // 略微後掠的流線羽翼座標變換
        float sweptY = y - Mathf.Abs(x) * 0.12f;

        // 1. 中心高亮度菱形晶核
        float diamondDist = Mathf.Abs(x) * 1.15f + Mathf.Abs(sweptY) * 2.10f;
        float diamondCore = 1f - SmoothStep(0.18f, 0.48f, diamondDist);

        // 2. 橫向修長的破風主光針與縱向俐落短星芒
        float horizSpike = Mathf.Exp(-Sq(sweptY * 9.5f)) * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(x)), 1.25f);
        float vertSpike = Mathf.Exp(-Sq(x * 11.0f)) * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Abs(y) * 1.35f), 1.6f);

        // 3. 柔和中心晨曦光暈
        float centerGlow = Mathf.Exp(-r * r * 9.0f) * 0.85f;

        float edgeMask = 1f - SmoothStep(0.72f, 0.98f, r);
        float val = Mathf.Clamp01((diamondCore * 1.1f + horizSpike * 1.15f + vertSpike * 0.85f + centerGlow) * edgeMask);
        return val;
    }

    #endregion

    // =====================================================================
    #region 共用工具與流線風絲演算法

    /// <summary>
    /// 流線風絲侵蝕：刀尾沿揮砍方向分解為纖細的高速氣流絲線，呈現輕盈破風感
    /// </summary>
    private static float WindStreamErode(float u, float v, int seed, float start)
    {
        if (u <= start) return 1f;
        float progress = SmoothStep(start, 1f, u);
        // 高頻縱向 V 與低頻橫向 U，形成細長的破風流線條紋
        float streaks = Noise2(u * 4.5f, v * 38f, seed) * 0.65f
                      + Noise2(u * 9.0f, v * 72f, seed + 29) * 0.35f;
        float threshold = progress * 0.92f;
        return SmoothStep(threshold - 0.06f, threshold + 0.06f, streaks);
    }

    private static float WindFilaments(float u, float v, int h, int seed, int count, float strength)
    {
        float a = 0f;
        for (int i = 0; i < count; i++)
        {
            float vk = Mathf.Lerp(0.12f, 0.88f, Hash(i, seed));
            float u0 = Mathf.Lerp(0.08f, 0.52f, Hash(i, seed + 1));
            float u1 = u0 + Mathf.Lerp(0.14f, 0.36f, Hash(i, seed + 2));
            if (u < u0 || u > u1) continue;
            float along = (u - u0) / (u1 - u0);
            // 隨流線微幅向中心收攏的氣流絲
            float curveV = vk + (0.5f - vk) * along * 0.18f;
            float line = Mathf.Exp(-Sq((v - curveV) * h / 1.05f));
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

        // 黑底預覽圖（存到專案 Temp/DaybreakPreview，方便檢視造型）
        var preview = new Color32[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte a = pixels[i].a;
            preview[i] = new Color32(a, a, a, 255);
        }
        tex.SetPixels32(preview);
        tex.Apply();
        string previewDir = Path.Combine(Application.dataPath, "../Temp/DaybreakPreview");
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
