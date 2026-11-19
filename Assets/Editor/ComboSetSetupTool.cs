using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ComboSetSetupTool
{
    private const string ComboFolder = "Assets/ScriptableObjects/Combat";
    private const string WeaponsFolder = "Assets/ScriptableObjects/Weapons";
    private const string WarriorPrefabPath = "Assets/Prefab/Character/Player/Warrior.prefab";

    [MenuItem("Tools/Dungeon/Setup ComboSets & PlayerStateController")]
    public static void SetupAll()
    {
        EnsureFolder(ComboFolder);

        ComboSetSO swordSet = CreateOrUpdateComboSet(
            $"{ComboFolder}/ComboSet_Sword.asset",
            new List<ComboStepData>
            {
                new ComboStepData { animatorStatePath = "Sword.Attack1", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.30f, comboCancelTime = 0.31f, moveCancelTime = 0.34f, endNormalizedTime = 0.88f, comboHoldDuration = 0.65f },
                new ComboStepData { animatorStatePath = "Sword.Attack2", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.42f, comboCancelTime = 0.43f, moveCancelTime = 0.46f, endNormalizedTime = 0.90f, comboHoldDuration = 0.65f },
                new ComboStepData { animatorStatePath = "Sword.Attack3", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.42f, comboCancelTime = 0.43f, moveCancelTime = 0.46f, endNormalizedTime = 0.87f, comboHoldDuration = 0.65f },
                new ComboStepData { animatorStatePath = "Sword.Attack4", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.44f, comboCancelTime = 0.45f, moveCancelTime = 0.48f, endNormalizedTime = 0.91f, comboHoldDuration = 0.65f },
                new ComboStepData { animatorStatePath = "Sword.Attack5", crossFadeDuration = 0.08f, moveCancelCrossFadeDuration = 0.25f, rollCancelTime = 0.51f, comboCancelTime = 0.52f, moveCancelTime = 0.55f, endNormalizedTime = 0.87f, comboHoldDuration = 0.65f },
            });

        ComboSetSO katanaSet = CreateOrUpdateComboSet(
            $"{ComboFolder}/ComboSet_Katana.asset",
            new List<ComboStepData>
            {
                new ComboStepData { animatorStatePath = "Katana.Attack1", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.21f, comboCancelTime = 0.22f, moveCancelTime = 0.26f, endNormalizedTime = 0.88f, comboHoldDuration = 0.60f },
                new ComboStepData { animatorStatePath = "Katana.Attack2", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.24f, comboCancelTime = 0.25f, moveCancelTime = 0.29f, endNormalizedTime = 0.87f, comboHoldDuration = 0.60f },
                new ComboStepData { animatorStatePath = "Katana.Attack3", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.25f, comboCancelTime = 0.26f, moveCancelTime = 0.30f, endNormalizedTime = 0.90f, comboHoldDuration = 0.60f },
                new ComboStepData { animatorStatePath = "Katana.Attack4", crossFadeDuration = 0.06f, moveCancelCrossFadeDuration = 0.20f, rollCancelTime = 0.25f, comboCancelTime = 0.26f, moveCancelTime = 0.30f, endNormalizedTime = 0.90f, comboHoldDuration = 0.60f },
            });

        ComboSetSO greatSwordSet = CreateOrUpdateComboSet(
            $"{ComboFolder}/ComboSet_GreatSword.asset",
            new List<ComboStepData>
            {
                new ComboStepData { animatorStatePath = "Hammer.GreatSword_Attack01", crossFadeDuration = 0.10f, moveCancelCrossFadeDuration = 0.28f, rollCancelTime = 0.42f, comboCancelTime = 0.43f, moveCancelTime = 0.48f, endNormalizedTime = 0.89f, comboHoldDuration = 0.75f },
                new ComboStepData { animatorStatePath = "Hammer.GreatSword_Attack02", crossFadeDuration = 0.10f, moveCancelCrossFadeDuration = 0.28f, rollCancelTime = 0.37f, comboCancelTime = 0.38f, moveCancelTime = 0.43f, endNormalizedTime = 0.89f, comboHoldDuration = 0.75f },
                new ComboStepData { animatorStatePath = "Hammer.GreatSword_Attack03", crossFadeDuration = 0.10f, moveCancelCrossFadeDuration = 0.28f, rollCancelTime = 0.56f, comboCancelTime = 0.57f, moveCancelTime = 0.62f, endNormalizedTime = 0.90f, comboHoldDuration = 0.75f },
            });

        // 幫所有 WeaponDataSO 資產配好對應的 ComboSetSO
        string[] weaponGuids = AssetDatabase.FindAssets("t:WeaponDataSO", new[] { WeaponsFolder });
        int boundCount = 0;
        foreach (string guid in weaponGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            WeaponDataSO weapon = AssetDatabase.LoadAssetAtPath<WeaponDataSO>(path);
            if (weapon == null) continue;

            ComboSetSO targetSet = weapon.weaponType switch
            {
                1 => katanaSet,
                2 => greatSwordSet,
                _ => swordSet
            };

            if (weapon.comboSet != targetSet)
            {
                weapon.comboSet = targetSet;
                EditorUtility.SetDirty(weapon);
                boundCount++;
            }
        }

        // 將 PlayerStateController 掛到 Warrior.prefab 根物件，並同步更新 Input Buffer 預設秒數
        bool prefabUpdated = false;
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(WarriorPrefabPath);
        if (prefabRoot != null)
        {
            try
            {
                if (prefabRoot.GetComponent<PlayerStateController>() == null)
                {
                    prefabRoot.AddComponent<PlayerStateController>();
                    prefabUpdated = true;
                }

                CharacterInputSystem inputSys = prefabRoot.GetComponent<CharacterInputSystem>();
                if (inputSys != null)
                {
                    SerializedObject so = new SerializedObject(inputSys);
                    so.FindProperty("attackBufferTime").floatValue = 0.30f;
                    so.FindProperty("rollBufferTime").floatValue = 0.28f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    prefabUpdated = true;
                }

                if (prefabUpdated)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, WarriorPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ComboSetSetupTool] 完成！已建置 3 份 ComboSetSO，綁定 {boundCount} 份武器資產，Warrior.prefab 更新={prefabUpdated}");
    }

    private static ComboSetSO CreateOrUpdateComboSet(string assetPath, List<ComboStepData> steps)
    {
        ComboSetSO asset = AssetDatabase.LoadAssetAtPath<ComboSetSO>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ComboSetSO>();
            asset.steps = steps;
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        else
        {
            asset.steps = steps;
            EditorUtility.SetDirty(asset);
        }
        return asset;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
