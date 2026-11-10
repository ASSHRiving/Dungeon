using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dungeon.Editor
{
    /// <summary>
    /// 嚴格遵循 VFX_Spec_Katana.md 規格之太刀程序化貼圖生成器：
    /// 1. Tex_Katana_Crescent_Edge.png (512x128): 極度銳利優雅的單側柳葉弦月刀刃光弧，帶有微細拉絲破風槽，首尾針尖收束。
    /// 2. Tex_Katana_Gale_Shadow.png (512x128): 半月型破空真空流體風壓帶，中心飽和、外緣羽化、尾端非線性尖角。
    /// 3. Tex_Katana_Plasma_Core.png (512x64): 8:1 高密度超純聚焦電漿雷射核心，中央純白高斯極致鋒芒。
    /// 4. Tex_Katana_Micro_Spark.png (128x128): 鋒銳四角菱形星芒與水平切片刀芒。
    /// </summary>
    public static class KatanaTextureGenerator
    {
        private const string OutputFolder = "Assets/VFX/Katana";

        [MenuItem("Tools/VFX/Katana/Generate Textures")]
        public static void GenerateAllTextures()
        {
            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            string crescentPath = Path.Combine(OutputFolder, "Tex_Katana_Crescent_Edge.png");
            string galePath     = Path.Combine(OutputFolder, "Tex_Katana_Gale_Shadow.png");
            string plasmaPath   = Path.Combine(OutputFolder, "Tex_Katana_Plasma_Core.png");
            string sparkPath    = Path.Combine(OutputFolder, "Tex_Katana_Micro_Spark.png");

            GenerateCrescentEdgeTexture(crescentPath, 512, 128);
            GenerateGaleShadowTexture(galePath, 512, 128);
            GeneratePlasmaCoreTexture(plasmaPath, 512, 64);
            GenerateMicroSparkTexture(sparkPath, 128, 128);

            AssetDatabase.Refresh();

            ConfigureTextureImport(crescentPath, TextureWrapMode.Clamp, 512);
            ConfigureTextureImport(galePath,     TextureWrapMode.Clamp, 512);
            ConfigureTextureImport(plasmaPath,   TextureWrapMode.Clamp, 512);
            ConfigureTextureImport(sparkPath,    TextureWrapMode.Clamp, 128);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[KatanaTextureGenerator] 依照 VFX_Spec_Katana.md 規格生成 4 張貼圖成功！");
        }

        /// <summary>
        /// 1. Tex_Katana_Crescent_Edge.png: 極致流暢鋒銳的太刀柳葉弦月刃 (無粗劣鋸齒，純粹居合刀光)
        /// </summary>
        public static void GenerateCrescentEdgeTexture(string path, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float distCenter = Mathf.Abs(v - 0.5f);

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;

                    // 頭部起刀極速展開 (0~0.03)，尾梢優雅收束為針尖 (0.45~1.0)
                    float headFade = SmoothStep(0.0f, 0.03f, u);
                    float tailFade = Mathf.Pow(Mathf.Clamp01(1.0f - SmoothStep(0.40f, 0.99f, u)), 1.4f);

                    // 柳葉太刀完美半月弧形刃部半寬度
                    float bladeHalfW = Mathf.Lerp(0.36f, 0.015f, Mathf.Pow(u, 0.85f));

                    // 外刃極薄超銳利切面 (Razor Bevel Rim)
                    float rimDist = Mathf.Abs(distCenter - bladeHalfW * 0.90f);
                    float outerBladeBevel = Mathf.Exp(- (rimDist * rimDist) / 0.00035f) * 1.25f;

                    // 刀身內部實體羽化光面
                    float bodyFill = 1.0f - SmoothStep(bladeHalfW * 0.40f, bladeHalfW + 0.01f, distCenter);

                    // 微細縱向拉絲破風流線 (居合極速破空絲痕)
                    float brushLines = Mathf.Sin((v * 36.0f + u * 2.0f) * Mathf.PI * 2.0f) * 0.15f + 0.85f;

                    // 刃脊中心高斯亮光
                    float centerSpine = Mathf.Exp(- (distCenter * distCenter) / (2.0f * (bladeHalfW * 0.28f) * (bladeHalfW * 0.28f)));

                    float combined = (outerBladeBevel + bodyFill * brushLines * 0.70f + centerSpine * 0.90f);
                    float alpha = Mathf.Clamp01(combined * headFade * tailFade);

                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 2. Tex_Katana_Gale_Shadow.png: 半月型破空真空流體風壓帶 (中心深沉，外緣柔和漸變)
        /// </summary>
        public static void GenerateGaleShadowTexture(string path, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float distCenter = Mathf.Abs(v - 0.5f);

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;

                    float headFade = SmoothStep(0.0f, 0.05f, u);
                    float tailFade = Mathf.Pow(Mathf.Clamp01(1.0f - SmoothStep(0.35f, 0.98f, u)), 1.2f);

                    // 氣流寬幅半月輪廓
                    float galeHalfW = Mathf.Lerp(0.44f, 0.08f, Mathf.Pow(u, 0.75f));

                    // 平滑真空破風剖面
                    float profile = 1.0f - SmoothStep(0.0f, galeHalfW, distCenter);
                    // 柔和雙側風動絲流
                    float flow = Mathf.Sin((v * 16.0f + u * 3.0f) * Mathf.PI * 2.0f) * 0.12f + 0.88f;

                    float alpha = Mathf.Clamp01(profile * flow * headFade * tailFade);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 3. Tex_Katana_Plasma_Core.png: 8:1 超窄高密度純淨電漿雷射核心
        /// </summary>
        public static void GeneratePlasmaCoreTexture(string path, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float distCenter = Mathf.Abs(v - 0.5f);

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;

                    float headFade = SmoothStep(0.0f, 0.02f, u);
                    float tailFade = Mathf.Pow(Mathf.Clamp01(1.0f - SmoothStep(0.50f, 0.99f, u)), 1.5f);

                    // 極細高斯核心光束
                    float coreRadius = Mathf.Lerp(0.045f, 0.008f, Mathf.Pow(u, 0.85f));
                    float laserBeam = Mathf.Exp(- (distCenter * distCenter) / (2.0f * coreRadius * coreRadius));
                    // 外圍極微弱高溫電暈
                    float plasmaGlow = Mathf.Exp(- (distCenter * distCenter) / (2.0f * coreRadius * coreRadius * 6.0f)) * 0.35f;

                    float alpha = Mathf.Clamp01((laserBeam + plasmaGlow) * headFade * tailFade);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// 4. Tex_Katana_Micro_Spark.png: 鋒銳十字菱形星芒 + 高速刀芒切片
        /// </summary>
        public static void GenerateMicroSparkTexture(string path, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float ny = ((y + 0.5f) / height) * 2.0f - 1.0f;
                for (int x = 0; x < width; x++)
                {
                    float nx = ((x + 0.5f) / width) * 2.0f - 1.0f;

                    float ax = Mathf.Abs(nx);
                    float ay = Mathf.Abs(ny);
                    float r = Mathf.Sqrt(nx * nx + ny * ny);

                    // 居合十字鋒芒針尖
                    float spikeX = Mathf.Exp(-ay * 22.0f) * Mathf.Pow(Mathf.Clamp01(1.0f - ax), 1.5f);
                    float spikeY = Mathf.Exp(-ax * 22.0f) * Mathf.Pow(Mathf.Clamp01(1.0f - ay), 1.5f);
                    // 菱形光核
                    float diamond = Mathf.Clamp01(1.0f - (ax + ay) * 1.8f);
                    // 柔和圓形暈光
                    float halo = Mathf.Exp(-r * r * 12.0f) * 0.60f;

                    float alpha = Mathf.Clamp01(spikeX * 1.2f + spikeY * 0.8f + diamond * 1.1f + halo);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.00001f, edge1 - edge0));
            return t * t * (3.0f - 2.0f * t);
        }

        private static void ConfigureTextureImport(string assetPath, TextureWrapMode wrapMode, int maxSize)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = wrapMode;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
        }
    }
}
