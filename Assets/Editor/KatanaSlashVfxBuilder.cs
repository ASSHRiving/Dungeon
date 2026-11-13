using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dungeon.Editor
{
    /// <summary>
    /// 嚴格遵循 VFX_Spec_Katana.md 規格建置太刀特效：
    /// - 3 層 TrailRenderer (Gale: 0.45m/0.18s, Edge: 0.30m/0.14s, Core: 0.10m/0.09s)
    /// - PS_Katana_Sparks: 強化為「高頻光劍熱能電漿粒子」——延長滯留時間 (0.55~0.85s)、低擴散飄逸、高能量留存，揮砍時形成光劍般的光芒鞘膜。
    ///
    /// 選單：Tools / VFX / Katana / Build Slash VFX
    /// </summary>
    public static class KatanaSlashVfxBuilder
    {
        private const string VfxFolder  = "Assets/VFX/Katana";
        private const string PrefabPath = "Assets/Prefab/Weapon/Katana.prefab";

        [MenuItem("Tools/VFX/Katana/Build Slash VFX")]
        public static void BuildKatanaSlashVfx()
        {
            KatanaTextureGenerator.GenerateAllTextures();

            var texGale     = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(VfxFolder, "Tex_Katana_Gale_Shadow.png"));
            var texCrescent = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(VfxFolder, "Tex_Katana_Crescent_Edge.png"));
            var texPlasma   = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(VfxFolder, "Tex_Katana_Plasma_Core.png"));
            var texSpark    = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(VfxFolder, "Tex_Katana_Micro_Spark.png"));

            if (texGale == null || texCrescent == null || texPlasma == null || texSpark == null)
            {
                Debug.LogError("[KatanaSlashVfxBuilder] 找不到生成的貼圖！");
                return;
            }

            // 材質球配置 (依據文檔 5.1 規格)
            var matGale = CreateOrUpdateParticleMaterial(
                Path.Combine(VfxFolder, "Mat_VFX_Katana_GaleShadow.mat"),
                texGale,
                new Color(1.0f, 1.0f, 1.0f, 1.0f),
                isAdditive: false,
                renderQueue: 2999);

            var matCrescent = CreateOrUpdateParticleMaterial(
                Path.Combine(VfxFolder, "Mat_VFX_Katana_CrescentEdge.mat"),
                texCrescent,
                new Color(2.2f, 0.8f, 0.15f, 1.0f),
                isAdditive: true,
                renderQueue: 3000);

            var matPlasma = CreateOrUpdateParticleMaterial(
                Path.Combine(VfxFolder, "Mat_VFX_Katana_PlasmaCore.mat"),
                texPlasma,
                new Color(2.5f, 2.5f, 2.5f, 1.0f),
                isAdditive: true,
                renderQueue: 3001);

            var matSpark = CreateOrUpdateParticleMaterial(
                Path.Combine(VfxFolder, "Mat_VFX_Katana_SparkParticle.mat"),
                texSpark,
                new Color(2.2f, 1.6f, 0.8f, 1.0f),
                isAdditive: true,
                renderQueue: 3002);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError("[KatanaSlashVfxBuilder] 無法讀取 " + PrefabPath);
                return;
            }

            try
            {
                RemoveChildIfExists(prefabRoot.transform, "Effects");
                RemoveChildIfExists(prefabRoot.transform, "VFX_Slash");
                RemoveChildIfExists(prefabRoot.transform, "VFX_Katana_Slash");

                var vfxRoot = new GameObject("VFX_Katana_Slash");
                vfxRoot.layer = prefabRoot.layer;
                vfxRoot.transform.SetParent(prefabRoot.transform, false);
                vfxRoot.transform.localPosition = Vector3.zero;
                vfxRoot.transform.localRotation = Quaternion.identity;
                vfxRoot.transform.localScale = Vector3.one;

                // 1. 底層破空真空暗影 (寬 0.45m, 滯留 0.18s, 掛載 -0.950, 0.015, 0)
                ConfigureGaleTrail(CreateChild(vfxRoot.transform, "Trail_Katana_Gale", new Vector3(-0.950f, 0.015f, 0f)), matGale);

                // 2. 居合弦月外刃 (寬 0.30m, 滯留 0.14s, 掛載 -1.260, 0.030, 0)
                ConfigureCrescentEdgeTrail(CreateChild(vfxRoot.transform, "Trail_Katana_Edge", new Vector3(-1.260f, 0.030f, 0f)), matCrescent);

                // 3. 高頻電漿核心 (寬 0.10m, 滯留 0.09s, 掛載 -1.180, 0.022, 0)
                ConfigurePlasmaCoreTrail(CreateChild(vfxRoot.transform, "Trail_Katana_Core", new Vector3(-1.180f, 0.022f, 0f)), matPlasma);

                // 4. 刀身熱能電漿粒子 (掛載 -0.750, 0.015, 0，依用戶需求強化光劍殘留感)
                ConfigureLightsaberSparks(CreateChild(vfxRoot.transform, "PS_Katana_Sparks", new Vector3(-0.750f, 0.015f, 0f)), matSpark);

                var weapon = prefabRoot.GetComponent<Weapon>();
                if (weapon != null)
                {
                    weapon.weaponTrails = vfxRoot.GetComponentsInChildren<TrailRenderer>(true);
                    weapon.weaponEffect = vfxRoot.GetComponentInChildren<ParticleSystem>(true);
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log("[KatanaSlashVfxBuilder] 成功將嚴格符合文檔與光劍效果之 VFX_Katana_Slash 套用至 " + PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material CreateOrUpdateParticleMaterial(
            string matPath,
            Texture2D mainTex,
            Color hdrTint,
            bool isAdditive,
            int renderQueue)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", hdrTint);
            mat.SetColor("_Color", hdrTint);

            mat.SetFloat("_Surface", 1.0f);
            mat.SetFloat("_ZWrite", 0.0f);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_AlphaClip", 0.0f);
            mat.SetFloat("_ColorMode", 0.0f);

            if (isAdditive)
            {
                mat.SetFloat("_Blend", 2.0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.One);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
            }
            else
            {
                mat.SetFloat("_Blend", 0.0f);
                mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            }

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = renderQueue;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ConfigureGaleTrail(GameObject go, Material mat)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.time = 0.18f;
            tr.minVertexDistance = 0.012f;
            tr.widthMultiplier = 0.45f;
            tr.numCornerVertices = 4;
            tr.numCapVertices = 2;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.generateLightingData = false;
            tr.emitting = false;
            tr.autodestruct = false;

            tr.widthCurve = new AnimationCurve(
                new Keyframe(0.00f, 0.85f),
                new Keyframe(0.15f, 1.00f),
                new Keyframe(1.00f, 0.05f));

            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.12f, 0.02f, 0.01f), 0.00f),
                    new GradientColorKey(new Color(0.08f, 0.02f, 0.01f), 0.50f),
                    new GradientColorKey(new Color(0.02f, 0.01f, 0.01f), 1.00f)
                },
                new[]
                {
                    new GradientAlphaKey(0.90f, 0.00f),
                    new GradientAlphaKey(0.60f, 0.50f),
                    new GradientAlphaKey(0.00f, 1.00f)
                });
            tr.colorGradient = grad;
        }

        private static void ConfigureCrescentEdgeTrail(GameObject go, Material mat)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.time = 0.14f;
            tr.minVertexDistance = 0.012f;
            tr.widthMultiplier = 0.30f;
            tr.numCornerVertices = 4;
            tr.numCapVertices = 2;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.generateLightingData = false;
            tr.emitting = false;
            tr.autodestruct = false;

            tr.widthCurve = new AnimationCurve(
                new Keyframe(0.00f, 0.95f),
                new Keyframe(0.10f, 1.00f),
                new Keyframe(1.00f, 0.02f));

            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.00f, 0.82f, 0.22f), 0.00f),
                    new GradientColorKey(new Color(1.00f, 0.42f, 0.08f), 0.45f),
                    new GradientColorKey(new Color(0.85f, 0.12f, 0.02f), 1.00f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0.00f),
                    new GradientAlphaKey(0.80f, 0.40f),
                    new GradientAlphaKey(0.00f, 1.00f)
                });
            tr.colorGradient = grad;
        }

        private static void ConfigurePlasmaCoreTrail(GameObject go, Material mat)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = mat;
            tr.time = 0.09f;
            tr.minVertexDistance = 0.012f;
            tr.widthMultiplier = 0.10f;
            tr.numCornerVertices = 4;
            tr.numCapVertices = 2;
            tr.alignment = LineAlignment.View;
            tr.textureMode = LineTextureMode.Stretch;
            tr.shadowCastingMode = ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.generateLightingData = false;
            tr.emitting = false;
            tr.autodestruct = false;

            tr.widthCurve = new AnimationCurve(
                new Keyframe(0.00f, 1.00f),
                new Keyframe(1.00f, 0.04f));

            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.00f),
                    new GradientColorKey(new Color(1.00f, 0.90f, 0.70f), 0.40f),
                    new GradientColorKey(new Color(0.95f, 0.55f, 0.15f), 1.00f)
                },
                new[]
                {
                    new GradientAlphaKey(1.00f, 0.00f),
                    new GradientAlphaKey(0.80f, 0.40f),
                    new GradientAlphaKey(0.00f, 1.00f)
                });
            tr.colorGradient = grad;
        }

        /// <summary>
        /// 強化為光劍風格（Lightsaber Effect）的刀身熱能電漿粒子：
        /// - 滯留時間延長至 0.55 ~ 0.85s
        /// - 緊貼刀身飄逸，形成一層凝滯的高熱電漿鞘膜
        /// - 隨揮砍在空中拉出延綿不絕的光劍殘光
        /// </summary>
        private static void ConfigureLightsaberSparks(GameObject go, Material mat)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.45f;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            // 延長存活時間，呈現光劍能量殘留滯空效果
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
            // 降低擴散速度，讓粒子緊貼刀刃軌跡形成熱能劍光鞘
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1.00f, 0.98f, 0.85f, 1.0f),
                new Color(1.00f, 0.65f, 0.15f, 1.0f));
            main.gravityModifier = 0.02f;
            main.maxParticles = 300;

            var emission = ps.emission;
            emission.enabled = false;
            emission.rateOverTime = 25f;
            emission.rateOverDistance = 60f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0.01f, 16, 22, 1, 0.01f)
            });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.15f, 0.04f, 0.04f);

            var limitVel = ps.limitVelocityOverLifetime;
            limitVel.enabled = true;
            limitVel.limit = 0.40f;
            limitVel.dampen = 0.35f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var colGrad = new Gradient();
            colGrad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1.00f, 1.00f, 0.90f), 0.00f), // 純白光劍核心
                    new GradientColorKey(new Color(1.00f, 0.65f, 0.15f), 0.50f), // 熾熱橙金
                    new GradientColorKey(new Color(0.85f, 0.18f, 0.05f), 1.00f)  // 尾端餘燼
                },
                new[]
                {
                    new GradientAlphaKey(0.00f, 0.00f),
                    new GradientAlphaKey(1.00f, 0.08f),
                    new GradientAlphaKey(0.85f, 0.65f), // 長時間維持高亮不散
                    new GradientAlphaKey(0.00f, 1.00f)
                });
            col.color = new ParticleSystem.MinMaxGradient(colGrad);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            // 前 60% 時間保持飽滿光劍光暈，末段才優雅衰減
            sol.size = new ParticleSystem.MinMaxCurve(1.0f, new AnimationCurve(
                new Keyframe(0.00f, 0.60f),
                new Keyframe(0.15f, 1.00f),
                new Keyframe(0.65f, 0.90f),
                new Keyframe(1.00f, 0.00f)));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = mat;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
        }

        private static GameObject CreateChild(Transform parent, string name, Vector3 localPos)
        {
            var child = new GameObject(name);
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPos;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child;
        }

        private static void RemoveChildIfExists(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }
    }
}
