using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dungeon.EditorTools
{
    public enum WeaponCategoryTemplate
    {
        OneHandSword, // Type 0: 單手劍 (5段連擊, OneHandOverride, Sound: Sword)
        Katana,       // Type 1: 太刀 (4段連擊, OneHandOverride, Sound: Sword)
        TwoHandHeavy  // Type 2: 重型/斧頭/大劍 (3段連擊, TwoHandOverride, Sound: GSword)
    }

    public class WeaponPrefabBuilderWindow : EditorWindow
    {
        [MenuItem("Tools/Weapons/Batch Weapon Prefab Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<WeaponPrefabBuilderWindow>("Weapon Prefab Generator");
            window.minSize = new Vector2(480, 520);
        }

        private List<GameObject> modelAssets = new List<GameObject>();
        private WeaponCategoryTemplate defaultCategory = WeaponCategoryTemplate.TwoHandHeavy;
        private string outputFolder = "Assets/Prefab/Weapon";
        private Vector2 scrollPos;

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("武器 Prefab 批量生成工具 (Batch Weapon Generator)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("將武器模型 (FBX / Prefab) 拖入清單，選擇武器類別後即可自動按照標準架構生成包含 Rigidbody、BoxCollider、DropsInteract、ShopWeapon 與 Melee(Sword) 的武器預製件。", MessageType.Info);

            EditorGUILayout.Space(10);
            defaultCategory = (WeaponCategoryTemplate)EditorGUILayout.EnumPopup("預設武器類別 (Category)", defaultCategory);
            outputFolder = EditorGUILayout.TextField("輸出目錄 (Output Folder)", outputFolder);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"模型清單 ({modelAssets.Count})", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            for (int i = 0; i < modelAssets.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                modelAssets[i] = (GameObject)EditorGUILayout.ObjectField($"模型 #{i + 1}", modelAssets[i], typeof(GameObject), false);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    modelAssets.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 新增欄位"))
            {
                modelAssets.Add(null);
            }
            if (GUILayout.Button("清空清單"))
            {
                modelAssets.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);
            if (GUILayout.Button("一鍵批量生成 Prefab (Batch Generate)", GUILayout.Height(38)))
            {
                BatchGenerate();
            }
        }

        private void BatchGenerate()
        {
            int count = 0;
            foreach (var model in modelAssets)
            {
                if (model == null) continue;
                string cleanName = model.name
                    .Replace("FREE ", "")
                    .Replace(" COLOR 1", "")
                    .Replace(" COLOR 2", "")
                    .Replace(" COLOR 3", "")
                    .Replace(" ", "");

                WeaponPrefabBuilder.CreateWeaponPrefab(model, cleanName, defaultCategory, 35f, outputFolder);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("生成完畢", $"成功生成 {count} 把武器 Prefab 於 {outputFolder}！", "確定");
        }
    }

    public static class WeaponPrefabBuilder
    {
        public const int WEAPON_LAYER = 12; // Layer 12: Weapon
        public const string ONE_HAND_OVERRIDE_PATH = "Assets/Animator/Player/OneHandOverride.overrideController";
        public const string TWO_HAND_OVERRIDE_PATH = "Assets/Animator/Player/TwoHandOverride.overrideController";
        public const string TWO_HAND_ICON_PATH = "Assets/SpritePhoto/Weapon/EquipmentIcon_20261105_195626.png";
        public const string ONE_HAND_ICON_PATH = "Assets/SpritePhoto/Weapon/EquipmentIcon_20261105_195717.png";

        [MenuItem("Tools/Weapons/Build GreatSword Prefab (Test FREE GREAT SWORD 2)")]
        public static void BuildGreatSwordTest()
        {
            string sourcePath = "Assets/Res/Model/FREE Low Poly Weapons Pack – Fantasy Dream/Prefabs/GREAT SWORDS/FREE GREAT SWORD 2 COLOR 1.prefab";
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"[WeaponPrefabBuilder] 找不到模型 Prefab: {sourcePath}");
                return;
            }

            CreateWeaponPrefab(sourcePrefab, "GreatSword", WeaponCategoryTemplate.TwoHandHeavy, 35f, "Assets/Prefab/Weapon");
        }

        public static GameObject CreateWeaponPrefab(GameObject sourceModel, string prefabName, WeaponCategoryTemplate category, float damage, string targetFolder = "Assets/Prefab/Weapon")
        {
            if (sourceModel == null) return null;

            // 1. 建立根物件
            GameObject weaponRoot = new GameObject(prefabName);
            weaponRoot.layer = WEAPON_LAYER;
            weaponRoot.tag = "Untagged";
            weaponRoot.transform.position = Vector3.zero;
            weaponRoot.transform.rotation = Quaternion.identity;
            weaponRoot.transform.localScale = Vector3.one;

            // 2. 實例化模型子物件 (Prefab 連動)
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel, weaponRoot.transform);
            modelInstance.name = sourceModel.name;
            SetLayerRecursively(modelInstance, WEAPON_LAYER);

            // 旋轉微調：若是重型/大劍類別，套用基準握持朝向
            if (category == WeaponCategoryTemplate.TwoHandHeavy)
            {
                modelInstance.transform.localPosition = new Vector3(0.05f, 0.002f, 0.01f);
                modelInstance.transform.localRotation = Quaternion.Euler(8.8f, -85.3f, 90.7f);
            }
            else
            {
                // 單手劍基準旋轉
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
            }
            modelInstance.transform.localScale = Vector3.one;

            // 3. 計算模型在 Root 空間下的 Bounds
            Bounds totalBounds = CalculateCombinedBounds(weaponRoot);

            // 4. 建立 AttackPoint
            GameObject attackPointGO = new GameObject("AttackPoint");
            attackPointGO.layer = WEAPON_LAYER;
            attackPointGO.tag = "Untagged";
            attackPointGO.transform.SetParent(weaponRoot.transform);

            // AttackPoint 位於刀身 -X 軸中上段
            Vector3 attackPointPos = new Vector3(
                totalBounds.min.x + (totalBounds.size.x * 0.35f),
                totalBounds.center.y,
                totalBounds.center.z
            );
            attackPointGO.transform.localPosition = attackPointPos;
            attackPointGO.transform.localRotation = Quaternion.identity;
            attackPointGO.transform.localScale = Vector3.one;

            // 5. Rigidbody
            Rigidbody rb = weaponRoot.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.useGravity = true;
            rb.isKinematic = false;

            // 6. BoxCollider
            BoxCollider col = weaponRoot.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.center = totalBounds.center;
            col.size = totalBounds.size;

            // 7. DropsInteract
            DropsInteract drops = weaponRoot.AddComponent<DropsInteract>();
            drops.interactableName = GetDefaultChineseName(category, prefabName);

            // 8. ShopWeapon
            ShopWeapon shop = weaponRoot.AddComponent<ShopWeapon>();
            shop.itemName = "";
            shop.price = 10;
            shop.weight = 1;

            // 9. Sword (Melee 通用近戰組件)
            Sword sword = weaponRoot.AddComponent<Sword>();
            sword.weaponName = drops.interactableName;
            sword.damage = damage;
            sword.combo = 0;
            sword.attackPoint = attackPointGO.transform;
            sword.weaponTrails = new TrailRenderer[0];
            sword.weaponEffect = null;

            string overridePath;
            string iconPath;
            int totalCombos;
            int weaponType;
            float radius;
            float runSpd;
            float walkSpd;
            SoundAssetsType soundType;

            switch (category)
            {
                case WeaponCategoryTemplate.OneHandSword:
                    weaponType = 0;
                    overridePath = ONE_HAND_OVERRIDE_PATH;
                    iconPath = ONE_HAND_ICON_PATH;
                    totalCombos = 5;
                    radius = 0.55f;
                    runSpd = 7f;
                    walkSpd = 3f;
                    soundType = SoundAssetsType.Sword;
                    break;
                case WeaponCategoryTemplate.Katana:
                    weaponType = 1;
                    overridePath = ONE_HAND_OVERRIDE_PATH;
                    iconPath = ONE_HAND_ICON_PATH;
                    totalCombos = 4;
                    radius = 0.6f;
                    runSpd = 10f;
                    walkSpd = 4f;
                    soundType = SoundAssetsType.Sword;
                    break;
                case WeaponCategoryTemplate.TwoHandHeavy:
                default:
                    weaponType = 2;
                    overridePath = TWO_HAND_OVERRIDE_PATH;
                    iconPath = TWO_HAND_ICON_PATH;
                    totalCombos = 3;
                    radius = 0.7f;
                    runSpd = 5f;
                    walkSpd = 3f;
                    soundType = SoundAssetsType.GSword;
                    break;
            }

            sword.weaponType = weaponType;
            sword.attackRadius = radius;
            sword.runSpeed = runSpd;
            sword.walkSpeed = walkSpd;
            sword.weaponSoundType = soundType;

            AnimatorOverrideController aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
            if (aoc != null) sword.overrideController = aoc;

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon != null) sword.itemIcon = icon;

            // SerializedObject 設定 protected / private [SerializeField]
            SerializedObject swordSO = new SerializedObject(sword);
            SerializedProperty totalComboProp = swordSO.FindProperty("totalComboSteps");
            if (totalComboProp != null) totalComboProp.intValue = totalCombos;

            SerializedProperty comboResetProp = swordSO.FindProperty("comboResetTime");
            if (comboResetProp != null) comboResetProp.floatValue = 2.0f;

            SerializedProperty hitStopProp = swordSO.FindProperty("hitStopDuration");
            if (hitStopProp != null) hitStopProp.floatValue = 0.045f;

            swordSO.ApplyModifiedPropertiesWithoutUndo();

            // 10. 輸出 Prefab
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            string finalPath = Path.Combine(targetFolder, $"{prefabName}.prefab").Replace("\\", "/");
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(weaponRoot, finalPath);
            GameObject.DestroyImmediate(weaponRoot);

            Debug.Log($"<color=green>[WeaponPrefabBuilder] 成功建立武器 Prefab: {finalPath}！</color>");
            return savedPrefab;
        }

        private static string GetDefaultChineseName(WeaponCategoryTemplate category, string englishName)
        {
            if (englishName.Contains("GreatSword")) return "巨劍";
            if (englishName.Contains("Axe")) return "斧頭";
            if (englishName.Contains("Katana")) return "太刀";
            if (englishName.Contains("Sword")) return "單手劍";

            switch (category)
            {
                case WeaponCategoryTemplate.OneHandSword: return "單手劍";
                case WeaponCategoryTemplate.Katana: return "太刀";
                case WeaponCategoryTemplate.TwoHandHeavy: return "巨劍";
                default: return "武器";
            }
        }

        public static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        public static Bounds CalculateCombinedBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, new Vector3(1f, 0.3f, 0.1f));
            }

            Matrix4x4 rootWorldToLocal = root.transform.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds localBounds = new Bounds();

            foreach (Renderer r in renderers)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Mesh mesh = mf.sharedMesh;
                    Matrix4x4 meshToRoot = rootWorldToLocal * r.transform.localToWorldMatrix;
                    Vector3[] vertices = mesh.vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        Vector3 pt = meshToRoot.MultiplyPoint3x4(vertices[i]);
                        if (!hasBounds)
                        {
                            localBounds = new Bounds(pt, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(pt);
                        }
                    }
                }
                else
                {
                    Bounds b = r.bounds;
                    Vector3 center = rootWorldToLocal.MultiplyPoint3x4(b.center);
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(center, b.size);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(new Bounds(center, b.size));
                    }
                }
            }

            return localBounds;
        }


    }
}
