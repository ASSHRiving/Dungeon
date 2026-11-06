#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Game.VFX;

namespace Game.VFX.Editor
{
    public static class StoneGiantSlamVfxBuilder
    {
        private const string BaseDir = "Assets/VFX/StoneGiantSlam";
        private const string TexDir = BaseDir + "/Textures";
        private const string MatDir = BaseDir + "/Materials";
        private const string MeshDir = BaseDir + "/Meshes";
        private const string PrefabDir = BaseDir + "/Prefabs";

        private struct CrackSegment
        {
            public Vector2 a;
            public Vector2 b;
            public float widthA;
            public float widthB;
        }

        [MenuItem("Tools/VFX/Build Stone Giant Ground Slam Prefab")]
        public static void BuildStoneGiantSlamVfx()
        {
            EnsureDirectories();

            // 1. 生成 100% 實心不透明貼圖 (.png)
            Texture2D crackTex = GenerateBranchingCrackTexture(512, TexDir + "/StoneSlam_VoronoiCrack.png");
            Texture2D ringTex = GenerateShockwaveRingTexture(256, TexDir + "/StoneSlam_ShockwaveRing.png");
            Texture2D dustTex = GenerateToonDustTexture(256, TexDir + "/StoneSlam_ToonDust.png");
            Texture2D sparkTex = GenerateSparkStreakTexture(256, TexDir + "/StoneSlam_SparkStreak.png");

            // 2. 生成低面數硬邊碎石網格 (.asset)
            Mesh rockMesh = GenerateFlatShadedRockMesh(MeshDir + "/StoneSlam_RockChunk.asset");

            // 3. 建立針對「暗黑色地面」的高對比冷灰/銀白材質 (.mat)，無任何土黃色
            Material crackMat = CreateCrackMaterial(MatDir + "/Mat_GroundCrackDecal.mat", crackTex);
            Material ringMat = CreateParticleMaterial(
                MatDir + "/Mat_ShockwaveAdditive.mat",
                ringTex,
                isAdditive: true,
                tintColor: new Color(1.6f, 1.75f, 2.0f, 1.0f) // 冷亮白銀色氣浪
            );
            Material dustMat = CreateParticleMaterial(
                MatDir + "/Mat_ToonDustAlpha.mat",
                dustTex,
                isAdditive: false,
                tintColor: new Color(0.88f, 0.90f, 0.94f, 1.0f) // 高不透明淺冷灰石塵
            );
            Material sparkMat = CreateParticleMaterial(
                MatDir + "/Mat_SparksAdditive.mat",
                sparkTex,
                isAdditive: true,
                tintColor: new Color(2.0f, 2.2f, 2.5f, 1.0f) // 熾白碎石迸裂亮光
            );
            Material rockMat = CreateRockMeshMaterial(MatDir + "/Mat_RockDebrisMesh.mat");

            // 4. 組裝特效 Prefab 階層
            GameObject root = new GameObject("VFX_StoneGiantGroundSlam");

            root.AddComponent<StoneGiantSlamVfxController>();
            CreateImpactLight(root.transform);
            TryAddCinemachineImpulse(root);

            CreateGroundCrackLayer(root.transform, crackMat);
            CreateShockwaveRingLayer(root.transform, ringMat);
            CreateRockDebrisLayer(root.transform, rockMesh, rockMat, sparkMat);
            CreateToonDustLayer(root.transform, dustMat);
            CreateImpactSparksLayer(root.transform, sparkMat);
            CreateFistSlamTrailLayer(root.transform, sparkMat);

            // 5. 儲存 Prefab
            string prefabPath = PrefabDir + "/VFX_StoneGiantGroundSlam.prefab";
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(prefabAsset);
            Debug.Log($"[StoneGiantSlamVfxBuilder] 已重新生成 100% 實心貼圖與放射狀地裂紋 Prefab：{prefabPath}");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { BaseDir, TexDir, MatDir, MeshDir, PrefabDir };
            foreach (string dir in dirs)
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 正確的 GLSL/HLSL smoothstep(edge0, edge1, x) 實作，回傳 0.0 ~ 1.0。
        /// 修正 Unity Mathf.SmoothStep 把前兩個參數當作輸出上下限導致 Alpha 只有 10% 的問題。
        /// </summary>
        private static float Smoothstep01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(0.00001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b, out float segT)
        {
            Vector2 ab = b - a;
            float lenSq = Vector2.Dot(ab, ab);
            if (lenSq < 0.000001f)
            {
                segT = 0f;
                return Vector2.Distance(p, a);
            }
            segT = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 projection = a + segT * ab;
            return Vector2.Distance(p, projection);
        }

        // =====================================================================
        // STEP 1: 100% 實心不透明貼圖合成 (無圓盤背景、純放射狀分支裂紋)
        // =====================================================================

        /// <summary>
        /// 生成「放射狀閃電分支地裂紋」：
        /// 無圓形背景，僅有由中心向外撕裂的主幹折線與分叉細紋。
        /// 裂紋中心為深黑裂隙，兩側邊緣為高對比亮銀灰崩裂石唇 (Alpha = 255)，在暗黑地面極度清晰。
        /// </summary>
        private static Texture2D GenerateBranchingCrackTexture(int size, string assetPath)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];

            List<CrackSegment> segments = new List<CrackSegment>();
            System.Random rng = new System.Random(2026);

            int primaryRayCount = 10;
            for (int i = 0; i < primaryRayCount; i++)
            {
                float baseAngle = (i / (float)primaryRayCount) * Mathf.PI * 2f + ((float)rng.NextDouble() - 0.5f) * 0.28f;
                float totalLen = 0.62f + (float)rng.NextDouble() * 0.28f;
                int steps = 7;

                Vector2 curr = Vector2.zero;
                float currAngle = baseAngle;

                for (int s = 0; s < steps; s++)
                {
                    float t0 = s / (float)steps;
                    float t1 = (s + 1) / (float)steps;

                    // 每一節產生銳利的岩石折角
                    currAngle += ((float)rng.NextDouble() - 0.5f) * 0.65f;
                    float segLen = (totalLen / steps) * (0.8f + (float)rng.NextDouble() * 0.4f);
                    Vector2 next = curr + new Vector2(Mathf.Cos(currAngle), Mathf.Sin(currAngle)) * segLen;

                    float w0 = Mathf.Lerp(0.026f, 0.003f, t0);
                    float w1 = Mathf.Lerp(0.026f, 0.003f, t1);
                    segments.Add(new CrackSegment { a = curr, b = next, widthA = w0, widthB = w1 });

                    // 在主幹中段分叉出次級裂紋與細微支線
                    if (s >= 1 && s <= 5 && rng.NextDouble() < 0.75)
                    {
                        float branchSign = rng.NextDouble() < 0.5 ? 1f : -1f;
                        float branchAngle = currAngle + branchSign * (0.45f + (float)rng.NextDouble() * 0.45f);
                        Vector2 bCurr = next;
                        int bSteps = 3 + rng.Next(0, 2);

                        for (int bs = 0; bs < bSteps; bs++)
                        {
                            float bt0 = bs / (float)bSteps;
                            float bt1 = (bs + 1) / (float)bSteps;
                            branchAngle += ((float)rng.NextDouble() - 0.5f) * 0.55f;
                            float bLen = (0.24f / bSteps) * (0.75f + (float)rng.NextDouble() * 0.4f);
                            Vector2 bNext = bCurr + new Vector2(Mathf.Cos(branchAngle), Mathf.Sin(branchAngle)) * bLen;

                            segments.Add(new CrackSegment
                            {
                                a = bCurr,
                                b = bNext,
                                widthA = Mathf.Lerp(w1 * 0.8f, 0.002f, bt0),
                                widthB = Mathf.Lerp(w1 * 0.8f, 0.002f, bt1)
                            });
                            bCurr = bNext;
                        }
                    }

                    curr = next;
                }
            }

            // 加入少量橫向連接的碎裂石紋，但絕不形成圓圈
            for (int c = 0; c < 8; c++)
            {
                float a1 = (float)rng.NextDouble() * Mathf.PI * 2f;
                float a2 = a1 + 0.35f + (float)rng.NextDouble() * 0.35f;
                float rad1 = 0.14f + (float)rng.NextDouble() * 0.28f;
                float rad2 = rad1 + ((float)rng.NextDouble() - 0.5f) * 0.12f;

                Vector2 p1 = new Vector2(Mathf.Cos(a1) * rad1, Mathf.Sin(a1) * rad1);
                Vector2 p2 = new Vector2(Mathf.Cos(a2) * rad2, Mathf.Sin(a2) * rad2);
                Vector2 mid = (p1 + p2) * 0.5f + new Vector2((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * 0.06f;

                segments.Add(new CrackSegment { a = p1, b = mid, widthA = 0.010f, widthB = 0.008f });
                segments.Add(new CrackSegment { a = mid, b = p2, widthA = 0.008f, widthB = 0.005f });
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)(size - 1)) * 2f - 1f, (y / (float)(size - 1)) * 2f - 1f);
                    float r = p.magnitude;

                    float borderMask = 1f - Smoothstep01(0.88f, 0.95f, r);

                    // 高頻岩石鋸齒微擾動，讓裂紋邊緣像真實石材崩裂
                    float jitter = Mathf.Sin(p.x * 55f + Mathf.Cos(p.y * 45f) * 2.5f) * 0.0045f
                                 + Mathf.Cos(p.y * 60f - Mathf.Sin(p.x * 50f) * 2.5f) * 0.0045f;
                    Vector2 warpedP = p + new Vector2(jitter, -jitter);

                    float minNormDist = 999f;

                    for (int i = 0; i < segments.Count; i++)
                    {
                        CrackSegment seg = segments[i];
                        float segT;
                        float d = DistanceToSegment(warpedP, seg.a, seg.b, out segT);
                        float w = Mathf.Max(0.002f, Mathf.Lerp(seg.widthA, seg.widthB, segT));
                        float normD = d / w;
                        if (normD < minNormDist)
                            minNormDist = normD;
                    }

                    // 裂紋剖面：
                    // normD <= 0.9 : 深黑石頭裂隙中心 (RGB = 18, Alpha = 255)
                    // 0.9 < normD <= 2.3 : 亮銀白/淺冷灰石頭崩裂高光唇邊 (RGB = 235, Alpha = 255 -> 0)
                    float coreMask = 1f - Smoothstep01(0.65f, 1.05f, minNormDist);
                    float rimMask = (1f - Smoothstep01(0.85f, 2.35f, minNormDist)) * (1f - coreMask * 0.88f);
                    float totalAlpha = Mathf.Clamp01((coreMask + rimMask) * 1.4f) * borderMask;

                    // 深黑裂縫 (0.07) 與亮冷灰石邊 (0.92) 的銳利對比
                    float brightness = Mathf.Lerp(0.07f, 0.94f, Mathf.Clamp01(rimMask * 1.6f));

                    byte rByte = (byte)(Mathf.Clamp01(brightness * 0.95f) * 255f);
                    byte gByte = (byte)(Mathf.Clamp01(brightness * 0.98f) * 255f);
                    byte bByte = (byte)(Mathf.Clamp01(brightness * 1.00f) * 255f);
                    byte aByte = (byte)(Mathf.Clamp01(totalAlpha) * 255f);

                    pixels[y * size + x] = new Color32(rByte, gByte, bByte, aByte);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return SaveAndConfigureTexture(tex, assetPath);
        }

        /// <summary>
        /// 100% 實心不透明衝擊波環貼圖 (修正後 Alpha 高達 255)
        /// </summary>
        private static Texture2D GenerateShockwaveRingTexture(int size, string assetPath)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)(size - 1)) * 2f - 1f, (y / (float)(size - 1)) * 2f - 1f);
                    float r = p.magnitude;
                    float angle = Mathf.Atan2(p.y, p.x);

                    float borderMask = 1f - Smoothstep01(0.88f, 0.95f, r);

                    // 外圈銳利震波環
                    float spikeOffset = Mathf.Sin(angle * 12f) * 0.035f + Mathf.Cos(angle * 20f) * 0.02f;
                    float ringRadius = 0.68f + spikeOffset;
                    float distOuter = Mathf.Abs(r - ringRadius);
                    float outerRing = 1f - Smoothstep01(0.0f, 0.16f, distOuter);

                    // 內圈次級氣浪環
                    float distInner = Mathf.Abs(r - (0.42f - spikeOffset * 0.5f));
                    float innerRing = (1f - Smoothstep01(0.0f, 0.09f, distInner)) * 0.85f;

                    float combined = Mathf.Clamp01(Mathf.Max(outerRing, innerRing));
                    // 3 階卡通色帶並強化實心不透明度
                    float stepped = Mathf.Ceil(combined * 3f) / 3f;
                    float finalAlpha = Mathf.Clamp01(Mathf.Lerp(combined, stepped, 0.7f) * 1.3f) * borderMask;

                    byte a = (byte)(finalAlpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return SaveAndConfigureTexture(tex, assetPath);
        }

        /// <summary>
        /// 100% 實心不透明卡通岩石塵煙貼圖 (修正後雲團核心 Alpha = 255)
        /// </summary>
        private static Texture2D GenerateToonDustTexture(int size, string assetPath)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];

            Vector2[] lobes = new Vector2[]
            {
                new Vector2( 0.00f,  0.00f),
                new Vector2(-0.28f,  0.18f),
                new Vector2( 0.26f,  0.20f),
                new Vector2(-0.24f, -0.22f),
                new Vector2( 0.25f, -0.20f),
                new Vector2( 0.00f,  0.28f),
                new Vector2( 0.02f, -0.26f)
            };
            float[] radii = { 0.58f, 0.45f, 0.44f, 0.42f, 0.43f, 0.42f, 0.40f };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)(size - 1)) * 2f - 1f, (y / (float)(size - 1)) * 2f - 1f);
                    float r = p.magnitude;

                    float borderMask = 1f - Smoothstep01(0.84f, 0.95f, r);

                    float density = 0f;
                    for (int i = 0; i < lobes.Length; i++)
                    {
                        float d = Vector2.Distance(p, lobes[i]) / radii[i];
                        float lobeVal = Mathf.Clamp01(1f - d * d);
                        density = Mathf.Max(density, lobeVal);
                    }

                    // 4 階高對比冷白/淺銀灰卡通明暗 (0.72 ~ 1.0)，完全不帶黃色
                    float lightDir = Mathf.Clamp01(0.6f + (-p.x * 0.25f + p.y * 0.38f));
                    float toonBand = Mathf.Floor(lightDir * 4f) / 3f;
                    float grayVal = Mathf.Lerp(0.72f, 1.0f, Mathf.Clamp01(toonBand));

                    // 當 density >= 0.22 時即為 100% 完全不透明 (Alpha = 255)
                    float alpha = Smoothstep01(0.08f, 0.22f, density) * borderMask;

                    byte c = (byte)(Mathf.Clamp01(grayVal) * 255f);
                    byte a = (byte)(Mathf.Clamp01(alpha) * 255f);
                    pixels[y * size + x] = new Color32(c, c, c, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return SaveAndConfigureTexture(tex, assetPath);
        }

        /// <summary>
        /// 100% 實心不透明銳利菱形星芒/軌跡貼圖 (修正後中心與刀鋒 Alpha = 255)
        /// </summary>
        private static Texture2D GenerateSparkStreakTexture(int size, string assetPath)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x / (float)(size - 1)) * 2f - 1f, (y / (float)(size - 1)) * 2f - 1f);
                    float r = p.magnitude;

                    float borderMask = 1f - Smoothstep01(0.85f, 0.95f, r);

                    // 銳利長菱形刀鋒 + 中心亮核
                    float diamondDist = Mathf.Abs(p.x) * 2.1f + Mathf.Abs(p.y) * 0.95f;
                    float diamond = 1f - Smoothstep01(0.0f, 0.85f, diamondDist);
                    float core = 1f - Smoothstep01(0.0f, 0.38f, r);

                    float finalAlpha = Mathf.Clamp01(Mathf.Max(diamond, core) * 1.4f) * borderMask;

                    byte a = (byte)(finalAlpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return SaveAndConfigureTexture(tex, assetPath);
        }

        private static Texture2D SaveAndConfigureTexture(Texture2D tex, string assetPath)
        {
            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(assetPath, pngBytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        // =====================================================================
        // STEP 2: PROCEDURAL FLAT-SHADED LOW-POLY ROCK MESH (.asset)
        // =====================================================================

        private static Mesh GenerateFlatShadedRockMesh(string assetPath)
        {
            Vector3[] v = new Vector3[]
            {
                new Vector3( 0.04f,  0.52f, -0.03f),
                new Vector3(-0.05f, -0.46f,  0.04f),
                new Vector3( 0.54f,  0.05f,  0.06f),
                new Vector3(-0.50f, -0.04f, -0.05f),
                new Vector3( 0.06f, -0.02f,  0.52f),
                new Vector3(-0.08f,  0.07f, -0.48f)
            };

            int[] octaTris = new int[]
            {
                0, 4, 2,
                0, 2, 5,
                0, 5, 3,
                0, 3, 4,
                1, 2, 4,
                1, 5, 2,
                1, 3, 5,
                1, 4, 3
            };

            List<Vector3> flatVerts = new List<Vector3>();
            List<int> flatTris = new List<int>();
            List<Color> vertColors = new List<Color>();

            for (int i = 0; i < octaTris.Length; i += 3)
            {
                Vector3 a = v[octaTris[i]];
                Vector3 b = v[octaTris[i + 1]];
                Vector3 c = v[octaTris[i + 2]];

                Vector3 center = ((a + b + c) / 3f).normalized * 0.52f;

                AppendFlatTriangle(flatVerts, flatTris, vertColors, a, b, center);
                AppendFlatTriangle(flatVerts, flatTris, vertColors, b, c, center);
                AppendFlatTriangle(flatVerts, flatTris, vertColors, c, a, center);
            }

            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            Mesh mesh = existingMesh != null ? existingMesh : new Mesh();
            mesh.Clear();
            mesh.name = "StoneSlam_RockChunk";
            mesh.SetVertices(flatVerts);
            mesh.SetTriangles(flatTris, 0);
            mesh.SetColors(vertColors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (existingMesh == null)
                AssetDatabase.CreateAsset(mesh, assetPath);
            else
                EditorUtility.SetDirty(mesh);

            return mesh;
        }

        private static void AppendFlatTriangle(List<Vector3> verts, List<int> tris, List<Color> colors, Vector3 a, Vector3 b, Vector3 c)
        {
            int startIdx = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            tris.Add(startIdx);
            tris.Add(startIdx + 1);
            tris.Add(startIdx + 2);

            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            float shade = Mathf.Lerp(0.75f, 1.0f, normal.y * 0.5f + 0.5f);
            Color facetColor = new Color(shade * 0.94f, shade * 0.96f, shade, 1f);
            colors.Add(facetColor);
            colors.Add(facetColor);
            colors.Add(facetColor);
        }

        // =====================================================================
        // STEP 3: URP MATERIAL CREATION & CONFIGURATION (.mat)
        // =====================================================================

        private static Material CreateCrackMaterial(string path, Texture2D tex)
        {
            Shader shader = Shader.Find("Custom/URP_VoronoiCrackDecal");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");

            Material mat = LoadOrCreateMaterial(path, shader);
            mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_RimBoost")) mat.SetFloat("_RimBoost", 2.0f);

            ConfigureTransparentMaterial(mat, isAdditive: false);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateParticleMaterial(string path, Texture2D tex, bool isAdditive, Color tintColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            Material mat = LoadOrCreateMaterial(path, shader);

            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tintColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tintColor);

            ConfigureTransparentMaterial(mat, isAdditive);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// 3D 碎石材質：改為高明度淺冷灰花崗岩色，在暗黑色地板上對比強烈且不帶土黃色
        /// </summary>
        private static Material CreateRockMeshMaterial(string path)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard");

            Material mat = LoadOrCreateMaterial(path, shader);
            Color lightGraniteColor = new Color(0.68f, 0.72f, 0.76f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", lightGraniteColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", lightGraniteColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.18f, 0.20f, 0.24f, 1f));
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            return mat;
        }

        private static void ConfigureTransparentMaterial(Material mat, bool isAdditive)
        {
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", 0f);
            if (mat.HasProperty("_ColorMode")) mat.SetFloat("_ColorMode", 0f);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHAMODULATE_ON");

            if (isAdditive)
            {
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.One);
                if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.One);

                mat.EnableKeyword("_BLENDMODE_ADD");
                mat.DisableKeyword("_BLENDMODE_ALPHA");
            }
            else
            {
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
                if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_SrcBlendAlpha")) mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                if (mat.HasProperty("_DstBlendAlpha")) mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);

                mat.EnableKeyword("_BLENDMODE_ALPHA");
                mat.DisableKeyword("_BLENDMODE_ADD");
            }

            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        // =====================================================================
        // STEP 4: PARTICLE SYSTEM & TRAIL RENDERER HIERARCHY BUILDERS
        // =====================================================================

        private static void CreateImpactLight(Transform parent)
        {
            GameObject lightGo = new GameObject("00_ImpactFlash_Light");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            Light ptLight = lightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = new Color(0.85f, 0.92f, 1.0f, 1.0f); // 冷白衝擊強光
            ptLight.range = 11.0f;
            ptLight.intensity = 15.0f;
            ptLight.shadows = LightShadows.None;
        }

        private static void TryAddCinemachineImpulse(GameObject root)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type impulseType = assembly.GetType("Cinemachine.CinemachineImpulseSource")
                                       ?? assembly.GetType("Unity.Cinemachine.CinemachineImpulseSource");
                if (impulseType != null)
                {
                    if (root.GetComponent(impulseType) == null)
                        root.AddComponent(impulseType);
                    break;
                }
            }
        }

        private static void CreateGroundCrackLayer(Transform parent, Material mat)
        {
            GameObject go = new GameObject("01_GroundCrack_Decal");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.03f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.duration = 2.2f;
            main.loop = false;
            main.startLifetime = 2.0f;
            main.startSpeed = 0f;
            main.startSize = 5.8f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var shape = ps.shape;
            shape.enabled = false;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0.0f, 0.45f, 8f, 8f),
                new Keyframe(0.06f, 1.0f, 0f, 0f),
                new Keyframe(1.0f, 1.0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // 冷銀白 -> 淺石灰，完全無土黃色，且前 80% 時間保持 100% 不透明
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0f),
                    new GradientColorKey(new Color(0.82f, 0.85f, 0.90f), 0.7f),
                    new GradientColorKey(new Color(0.55f, 0.58f, 0.62f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.80f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            var customData = ps.customData;
            customData.enabled = true;
            customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
            customData.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1);
            AnimationCurve erosionCurve = new AnimationCurve(
                new Keyframe(0.0f, 0.0f),
                new Keyframe(0.65f, 0.0f),
                new Keyframe(1.0f, 1.0f)
            );
            customData.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(1f, erosionCurve));

            psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            psr.sharedMaterial = mat;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
            psr.sortingFudge = -2f;

            psr.SetActiveVertexStreams(new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV,
                ParticleSystemVertexStream.Custom1X
            });
        }

        private static void CreateShockwaveRingLayer(Transform parent, Material mat)
        {
            GameObject go = new GameObject("02_Shockwave_Ring");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.04f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.duration = 1.0f;
            main.loop = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 0f;
            main.startSize = 7.8f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.0f, 2),
                new ParticleSystem.Burst(0.07f, 1)
            });

            var shape = ps.shape;
            shape.enabled = false;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve ringExpand = new AnimationCurve(
                new Keyframe(0.0f, 0.15f, 4.2f, 4.2f),
                new Keyframe(0.6f, 0.88f, 0.6f, 0.6f),
                new Keyframe(1.0f, 1.0f, 0.1f, 0.1f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, ringExpand);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0f),
                    new GradientColorKey(new Color(0.75f, 0.82f, 0.92f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = grad;

            psr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            psr.sharedMaterial = mat;
            psr.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void CreateRockDebrisLayer(Transform parent, Mesh rockMesh, Material rockMat, Material trailMat)
        {
            GameObject go = new GameObject("03_RockDebris_Mesh");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.duration = 2.0f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 13.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.65f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 2.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, 20, 26, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 48f;
            shape.radius = 0.65f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var rotOverLife = ps.rotationOverLifetime;
            rotOverLife.enabled = true;
            rotOverLife.separateAxes = true;
            rotOverLife.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rotOverLife.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rotOverLife.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.0f, 1.0f),
                new Keyframe(0.8f, 1.0f),
                new Keyframe(1.0f, 0.0f)
            ));

            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 0.42f;
            collision.bounce = 0.35f;
            collision.lifetimeLoss = 0.05f;

            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 0.5f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.18f);
            trails.minVertexDistance = 0.08f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.35f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            Gradient trailGrad = new Gradient();
            trailGrad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.9f, 0.94f, 1.0f), 0f),
                    new GradientColorKey(new Color(0.6f, 0.65f, 0.72f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.0f, 1f)
                }
            );
            trails.colorOverTrail = trailGrad;

            psr.renderMode = ParticleSystemRenderMode.Mesh;
            psr.mesh = rockMesh;
            psr.sharedMaterial = rockMat;
            psr.trailMaterial = trailMat;
            psr.shadowCastingMode = ShadowCastingMode.On;
            psr.receiveShadows = true;
        }

        private static void CreateToonDustLayer(Transform parent, Material dustMat)
        {
            GameObject go = new GameObject("04_DustPlume_Toon");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.22f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.duration = 2.0f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(7.5f, 12.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.3f, 2.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, 26, 32, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.85f;
            shape.radiusThickness = 0.35f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var limitVel = ps.limitVelocityOverLifetime;
            limitVel.enabled = true;
            limitVel.limit = 1.25f;
            limitVel.dampen = 0.22f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0.0f, 0.5f, 2.0f, 2.0f),
                new Keyframe(0.35f, 1.0f, 0.4f, 0.4f),
                new Keyframe(1.0f, 1.35f)
            ));

            var rotOverLife = ps.rotationOverLifetime;
            rotOverLife.enabled = true;
            rotOverLife.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

            // 純冷灰白石頭煙塵 (無任何土黃色)，且 Alpha 保持 1.0 實心飽滿
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.92f, 0.94f, 0.98f), 0.0f),
                    new GradientColorKey(new Color(0.75f, 0.78f, 0.82f), 0.5f),
                    new GradientColorKey(new Color(0.55f, 0.58f, 0.62f), 1.0f)
                },
                new[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.65f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = grad;

            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = dustMat;
            psr.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void CreateImpactSparksLayer(Transform parent, Material sparkMat)
        {
            GameObject go = new GameObject("05_ImpactSparks_Stretch");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

            var main = ps.main;
            main.duration = 1.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(9.0f, 18.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
            main.gravityModifier = 1.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, 40, 50, 1, 0.01f) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 58f;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0.0f),
                    new GradientColorKey(new Color(0.80f, 0.88f, 1.0f), 0.6f),
                    new GradientColorKey(new Color(0.55f, 0.62f, 0.72f), 1.0f)
                },
                new[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.7f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = grad;

            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.dampen = 0.3f;
            collision.bounce = 0.5f;

            psr.renderMode = ParticleSystemRenderMode.Stretch;
            psr.velocityScale = 0.06f;
            psr.lengthScale = 2.6f;
            psr.sharedMaterial = sparkMat;
            psr.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void CreateFistSlamTrailLayer(Transform parent, Material trailMat)
        {
            GameObject go = new GameObject("06_FistSlam_Trail");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.28f;
            trail.minVertexDistance = 0.06f;
            trail.widthMultiplier = 0.95f;
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0.0f, 1.0f),
                new Keyframe(0.6f, 0.6f),
                new Keyframe(1.0f, 0.0f)
            );

            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0.0f),
                    new GradientColorKey(new Color(0.75f, 0.82f, 0.92f), 1.0f)
                },
                new[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.4f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            trail.colorGradient = grad;

            trail.textureMode = LineTextureMode.Stretch;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = trailMat;
        }
    }
}
#endif