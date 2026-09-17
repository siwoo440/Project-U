using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// 78일차: 게임 Prefab들의 기본 도형 외형을 저폴리 모델로 교체한다.
public static class StylizedArtPrefabApplier
{
    private const string PrefabRoot = "Assets/_ProjectU/Prefabs/";
    private const string ProfileFolder = "Assets/_ProjectU/Data/VisualProfiles";
    private const string RegistryPath = "Assets/_ProjectU/Data/Registry/GameDataRegistry.asset";

    private sealed class Entry
    {
        public readonly string Path;
        public readonly string ModelId;
        public readonly StylizedVisualReplacer.Options Options;
        public readonly bool AddFlame;

        public Entry(string path, string modelId, StylizedVisualReplacer.Options options = null, bool addFlame = false)
        {
            Path = PrefabRoot + path;
            ModelId = modelId;
            Options = options ?? new StylizedVisualReplacer.Options();
            AddFlame = addFlame;
        }
    }

    private static StylizedVisualReplacer.Options Wall()
    {
        return new StylizedVisualReplacer.Options { OrientThinAxisToZ = true };
    }

    private static StylizedVisualReplacer.Options LongZ()
    {
        return new StylizedVisualReplacer.Options { OrientLongAxisToZ = true };
    }

    private static StylizedVisualReplacer.Options LyingTool()
    {
        return new StylizedVisualReplacer.Options { ExtraEuler = new Vector3(0f, 0f, 90f) };
    }

    private static readonly Entry[] Entries =
    {
        // 채집 자원
        new Entry("Gathering/TreeResource_01.prefab", "tree_round"),
        new Entry("Gathering/StoneResource_01.prefab", "rock_resource"),

        // 아이템
        new Entry("Items/ApplePickup.prefab", "item_apple"),
        new Entry("Items/BandagePickup.prefab", "item_bandage"),
        new Entry("Items/Berry.prefab", "item_berry"),
        new Entry("Items/ClothCapPickup.prefab", "item_cap"),
        new Entry("Items/ClothShirtPickup.prefab", "item_shirt"),
        new Entry("Items/SmallBackpackPickup.prefab", "item_backpack"),
        new Entry("Items/StonePickup.prefab", "item_stone"),
        new Entry("Items/WaterBottlePickup.prefab", "item_water_bottle"),
        new Entry("Items/WorkHatPickup.prefab", "item_work_hat"),
        new Entry("Items/WorldItemDrop.prefab", "item_sack"),
        new Entry("Items/Day71/HerbalTeaPickup.prefab", "item_herbal_tea"),
        new Entry("Items/Day71/IronAxePickup.prefab", "tool_iron_axe", LyingTool()),
        new Entry("Items/Day71/IronOrePickup.prefab", "item_iron_ore"),
        new Entry("Items/Day71/PlantFiberPickup.prefab", "item_plant_fiber"),
        new Entry("Items/Day71/WildMushroomPickup.prefab", "item_mushroom"),

        // 투사체
        new Entry("Combat/Projectile_Arrow.prefab", "projectile_arrow"),
        new Entry("Enemies/Day74/EnemyProjectile_Day74.prefab", "projectile_spit"),

        // 건축물 (설치본과 미리보기)
        new Entry("Building/CampfirePlaced.prefab", "build_campfire", null, true),
        new Entry("Building/CampfirePreview.prefab", "build_campfire"),
        new Entry("Building/Day73/StoneCampfirePlaced.prefab", "build_campfire_stone", null, true),
        new Entry("Building/Day73/StoneCampfirePreview.prefab", "build_campfire_stone"),
        new Entry("Building/Day73/StoneFloorPlaced.prefab", "build_stone_floor"),
        new Entry("Building/Day73/StoneFloorPreview.prefab", "build_stone_floor"),
        new Entry("Building/Day73/StoneFoundationPlaced.prefab", "build_stone_foundation"),
        new Entry("Building/Day73/StoneFoundationPreview.prefab", "build_stone_foundation"),
        new Entry("Building/Day73/StoneWallPlaced.prefab", "build_stone_wall", Wall()),
        new Entry("Building/Day73/StoneWallPreview.prefab", "build_stone_wall", Wall()),
        new Entry("Building/Day73/WoodChairPlaced.prefab", "build_chair"),
        new Entry("Building/Day73/WoodChairPreview.prefab", "build_chair"),
        new Entry("Building/Day73/WoodTablePlaced.prefab", "build_table"),
        new Entry("Building/Day73/WoodTablePreview.prefab", "build_table"),
        new Entry("Building/LargeChestPlaced.prefab", "build_chest_large"),
        new Entry("Building/LargeChestPreview.prefab", "build_chest_large"),
        new Entry("Building/SmallChestPlaced.prefab", "build_chest_small"),
        new Entry("Building/SmallChestPreview.prefab", "build_chest_small"),
        new Entry("Building/SleepingBagPlaced.prefab", "build_sleeping_bag", LongZ()),
        new Entry("Building/SleepingBagPreview.prefab", "build_sleeping_bag", LongZ()),
        new Entry("Building/StandingLampPlaced.prefab", "build_lamp"),
        new Entry("Building/StandingLampPreview.prefab", "build_lamp"),
        new Entry("Building/WoodFloorPreview.prefab", "build_wood_floor"),
        new Entry("Building/WoodFoundationPlaced.prefab", "build_wood_foundation"),
        new Entry("Building/WoodFoundationPreview.prefab", "build_wood_foundation"),
        new Entry("Building/WoodWallPlaced.prefab", "build_wood_wall", Wall()),
        new Entry("Building/WoodWallPreview.prefab", "build_wood_wall", Wall()),
        new Entry("Building/WorkbenchPlaced.prefab", "build_workbench"),
        new Entry("Building/WorkbenchPreview.prefab", "build_workbench"),
    };

    // ContentVisualRoot를 사용하는 Prefab은 Visual Profile의 외형 Prefab을 바꾼다
    private sealed class ProfileEntry
    {
        public readonly string PrefabPath;
        public readonly string ProfileAssetPath;
        public readonly string ProfileId;
        public readonly string DisplayName;
        public readonly ContentVisualCategory Category;
        public readonly string ModelId;
        public readonly bool FitToCapsule;
        public readonly string LegacyMeshChild;

        public ProfileEntry(string prefabPath, string profileAssetPath, string profileId, string displayName, ContentVisualCategory category, string modelId, bool fitToCapsule, string legacyMeshChild = null)
        {
            PrefabPath = PrefabRoot + prefabPath;
            ProfileAssetPath = ProfileFolder + "/" + profileAssetPath;
            ProfileId = profileId;
            DisplayName = displayName;
            Category = category;
            ModelId = modelId;
            FitToCapsule = fitToCapsule;
            LegacyMeshChild = legacyMeshChild;
        }
    }

    private static readonly ProfileEntry[] ProfileEntries =
    {
        new ProfileEntry("Development/Placeholder/PF_Temp_Enemy_Basic.prefab", "Enemies/VP_Enemy_Basic.asset", "visual_enemy_basic", "ENEMY BASIC", ContentVisualCategory.Enemy, "enemy_slime", true),
        new ProfileEntry("Enemies/Day74/Enemy_MeleeGrunt.prefab", "Enemies/VP_Enemy_MeleeGrunt.asset", "visual_enemy_melee_grunt", "MELEE GRUNT", ContentVisualCategory.Enemy, "enemy_grunt", true),
        new ProfileEntry("Enemies/Day74/Enemy_RangedSpitter.prefab", "Enemies/VP_Enemy_RangedSpitter.asset", "visual_enemy_ranged_spitter", "RANGED SPITTER", ContentVisualCategory.Enemy, "enemy_spitter", true),
        new ProfileEntry("Items/WoodPickup.prefab", "Items/VP_Item_Wood.asset", "visual_item_wood", "WOOD", ContentVisualCategory.Item, "item_wood_bundle", false),
        new ProfileEntry("Building/WoodFloorPlaced.prefab", "Buildables/VP_Buildable_WoodFloor.asset", "visual_buildable_wood_floor", "WOOD FLOOR", ContentVisualCategory.Buildable, "build_wood_floor", false, "FloorMesh"),
    };

    public static string ApplyAll()
    {
        StringBuilder report = new StringBuilder();
        int success = 0;
        int failed = 0;

        try
        {
            for (int index = 0; index < Entries.Length; index++)
            {
                Entry entry = Entries[index];
                EditorUtility.DisplayProgressBar("Project U 저폴리 외형 적용", entry.Path, index / (float)(Entries.Length + ProfileEntries.Length));

                if (ApplyEntry(entry, out string message))
                {
                    success++;
                }
                else
                {
                    failed++;
                }

                report.AppendLine(message);
            }

            List<ContentVisualProfile> profiles = new List<ContentVisualProfile>();

            for (int index = 0; index < ProfileEntries.Length; index++)
            {
                ProfileEntry entry = ProfileEntries[index];
                EditorUtility.DisplayProgressBar("Project U 저폴리 외형 적용", entry.PrefabPath, (Entries.Length + index) / (float)(Entries.Length + ProfileEntries.Length));

                if (ApplyProfileEntry(entry, out ContentVisualProfile profile, out string message))
                {
                    success++;
                    profiles.Add(profile);
                }
                else
                {
                    failed++;
                }

                report.AppendLine(message);
            }

            RegisterProfiles(profiles, report);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }

        report.Insert(0, $"Prefab 외형 적용 완료 / 성공 {success} / 실패 {failed}\n");
        return report.ToString();
    }

    public static string RestoreAll()
    {
        int restored = 0;

        for (int index = 0; index < Entries.Length; index++)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(Entries[index].Path);

            try
            {
                if (StylizedVisualReplacer.Restore(root, false))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, Entries[index].Path);
                    restored++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return $"기본 도형 외형으로 복구한 Prefab {restored}개 (Visual Profile 연결은 유지)";
    }

    private static bool ApplyEntry(Entry entry, out string message)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(entry.Path) == null)
        {
            message = $"[건너뜀] Prefab 없음: {entry.Path}";
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(entry.Path);

        try
        {
            StylizedVisualReplacer.Options options = entry.Options;
            options.FlameRoot = null;
            options.FlameLight = null;

            if (entry.AddFlame)
            {
                ResolveFlameTargets(root, options);
            }

            bool applied = StylizedVisualReplacer.Replace(root, entry.ModelId, options, out message);

            if (applied)
            {
                PrefabUtility.SaveAsPrefabAsset(root, entry.Path);
            }

            return applied;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ResolveFlameTargets(GameObject root, StylizedVisualReplacer.Options options)
    {
        CampfireCookingStation station = root.GetComponent<CampfireCookingStation>();

        if (station == null)
        {
            return;
        }

        SerializedObject serializedStation = new SerializedObject(station);
        SerializedProperty fireRootProperty = serializedStation.FindProperty("fireVisualRoot");
        GameObject fireRoot = fireRootProperty != null ? fireRootProperty.objectReferenceValue as GameObject : null;

        if (fireRoot == null)
        {
            return;
        }

        options.FlameRoot = fireRoot.transform;
        options.FlameLight = fireRoot.GetComponentInChildren<Light>(true);
    }

    private static bool ApplyProfileEntry(ProfileEntry entry, out ContentVisualProfile profile, out string message)
    {
        profile = null;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath) == null)
        {
            message = $"[건너뜀] Prefab 없음: {entry.PrefabPath}";
            return false;
        }

        GameObject modelPrefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(entry.ModelId, false);

        if (modelPrefab == null)
        {
            message = $"[실패] 모델 생성 실패: {entry.ModelId}";
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(entry.PrefabPath);

        try
        {
            ContentVisualRoot visualRoot = root.GetComponent<ContentVisualRoot>();

            if (visualRoot == null)
            {
                message = $"[실패] ContentVisualRoot 없음: {entry.PrefabPath}";
                return false;
            }

            profile = LoadOrCreateProfile(entry, visualRoot);
            Bounds modelBounds = modelPrefab.GetComponent<MeshFilter>().sharedMesh.bounds;
            Bounds target = ResolveProfileTargetBounds(root, entry);
            Vector3 scale;

            if (entry.FitToCapsule)
            {
                // 캐릭터는 콜라이더 높이에 맞추되 너무 넓어지지 않도록 폭도 제한
                float byHeight = target.size.y / Mathf.Max(0.001f, modelBounds.size.y);
                float modelWidth = Mathf.Max(modelBounds.size.x, modelBounds.size.z);
                float byWidth = Mathf.Max(target.size.x, target.size.z) * 1.7f / Mathf.Max(0.001f, modelWidth);
                scale = Vector3.one * Mathf.Min(byHeight, byWidth);
            }
            else if (entry.Category == ContentVisualCategory.Buildable)
            {
                scale = new Vector3(
                    target.size.x / Mathf.Max(0.001f, modelBounds.size.x),
                    target.size.y / Mathf.Max(0.001f, modelBounds.size.y),
                    target.size.z / Mathf.Max(0.001f, modelBounds.size.z));
            }
            else
            {
                float targetLargest = Mathf.Max(target.size.x, target.size.y, target.size.z);
                float modelLargest = Mathf.Max(modelBounds.size.x, modelBounds.size.y, modelBounds.size.z);
                scale = Vector3.one * (targetLargest / Mathf.Max(0.001f, modelLargest));
            }

            Vector3 modelBottom = new Vector3(modelBounds.center.x, modelBounds.min.y, modelBounds.center.z);
            Vector3 targetBottom = new Vector3(target.center.x, target.min.y, target.center.z);
            Vector3 position = targetBottom - Vector3.Scale(modelBottom, scale);

            SerializedObject serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("visualPrefab").objectReferenceValue = modelPrefab;
            serializedProfile.FindProperty("visualLocalPosition").vector3Value = position;
            serializedProfile.FindProperty("visualLocalEulerAngles").vector3Value = Vector3.zero;
            serializedProfile.FindProperty("visualLocalScale").vector3Value = scale;
            serializedProfile.FindProperty("materialOverride").objectReferenceValue = null;
            serializedProfile.FindProperty("removeVisualColliders").boolValue = true;
            serializedProfile.FindProperty("inheritRootLayer").boolValue = true;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);

            // Prefab 안에 저장된 외형도 즉시 새 모델로 교체해 Scene에서도 보이게 한다
            visualRoot.ApplyProfile(profile, true);
            ContentVisualProfileBinder binder = root.GetComponent<ContentVisualProfileBinder>();

            if (binder != null)
            {
                SerializedObject serializedBinder = new SerializedObject(binder);
                SerializedProperty profileProperty = serializedBinder.FindProperty("visualProfile");

                if (profileProperty != null)
                {
                    profileProperty.objectReferenceValue = profile;
                    serializedBinder.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (!string.IsNullOrEmpty(entry.LegacyMeshChild))
            {
                HideLegacyChildMesh(root, entry.LegacyMeshChild);
            }

            PrefabUtility.SaveAsPrefabAsset(root, entry.PrefabPath);
            message = $"{root.name}: Visual Profile {entry.ProfileId} → {entry.ModelId}";
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static ContentVisualProfile LoadOrCreateProfile(ProfileEntry entry, ContentVisualRoot visualRoot)
    {
        ContentVisualProfile profile = AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(entry.ProfileAssetPath);

        if (profile != null)
        {
            return profile;
        }

        StylizedArtAssetFactory.EnsureFolder(System.IO.Path.GetDirectoryName(entry.ProfileAssetPath).Replace('\\', '/'));
        ContentVisualProfile template = visualRoot.AppliedProfile;
        profile = template != null
            ? Object.Instantiate(template)
            : ScriptableObject.CreateInstance<ContentVisualProfile>();

        SerializedObject serializedProfile = new SerializedObject(profile);
        serializedProfile.FindProperty("profileId").stringValue = entry.ProfileId;
        serializedProfile.FindProperty("displayName").stringValue = entry.DisplayName;
        serializedProfile.FindProperty("category").enumValueIndex = (int)entry.Category;
        serializedProfile.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(profile, entry.ProfileAssetPath);
        return profile;
    }

    private static Bounds ResolveProfileTargetBounds(GameObject root, ProfileEntry entry)
    {
        if (entry.FitToCapsule)
        {
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();

            if (capsule != null)
            {
                float height = Mathf.Max(capsule.height, capsule.radius * 2f);
                float diameter = capsule.radius * 2f;
                return new Bounds(capsule.center, new Vector3(diameter, height, diameter));
            }

            return new Bounds(Vector3.zero, new Vector3(1f, 2f, 1f));
        }

        if (!string.IsNullOrEmpty(entry.LegacyMeshChild))
        {
            Transform legacy = root.transform.Find(entry.LegacyMeshChild);
            MeshFilter filter = legacy != null ? legacy.GetComponent<MeshFilter>() : null;
            StylizedVisualReplacement record = root.GetComponent<StylizedVisualReplacement>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;

            // 이미 숨긴 경우 기록된 원래 메시 크기를 사용
            if (mesh == null && record != null && record.OriginalMeshes.Length > 0)
            {
                mesh = record.OriginalMeshes[0];
            }

            if (legacy != null && mesh != null)
            {
                return TransformBounds(mesh.bounds, root.transform.worldToLocalMatrix * legacy.localToWorldMatrix);
            }
        }

        BoxCollider box = root.GetComponent<BoxCollider>();

        if (box != null)
        {
            return new Bounds(box.center, box.size);
        }

        MeshFilter rootFilter = root.GetComponent<MeshFilter>();

        if (rootFilter != null && rootFilter.sharedMesh != null)
        {
            return rootFilter.sharedMesh.bounds;
        }

        return new Bounds(new Vector3(0f, 0.25f, 0f), Vector3.one * 0.5f);
    }

    private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
    {
        Bounds result = new Bounds(matrix.MultiplyPoint3x4(bounds.center), Vector3.zero);
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = new Vector3(
                (corner & 1) == 0 ? min.x : max.x,
                (corner & 2) == 0 ? min.y : max.y,
                (corner & 4) == 0 ? min.z : max.z);
            result.Encapsulate(matrix.MultiplyPoint3x4(point));
        }

        return result;
    }

    private static void HideLegacyChildMesh(GameObject root, string childName)
    {
        Transform legacy = root.transform.Find(childName);
        MeshFilter filter = legacy != null ? legacy.GetComponent<MeshFilter>() : null;

        if (filter == null || filter.sharedMesh == null)
        {
            return;
        }

        StylizedVisualReplacement record = root.GetComponent<StylizedVisualReplacement>();

        if (record == null)
        {
            record = root.AddComponent<StylizedVisualReplacement>();
        }

        record.Record("profile_legacy_hidden", null, new[] { filter }, new[] { filter.sharedMesh });
        filter.sharedMesh = null;
        EditorUtility.SetDirty(filter);
    }

    private static void RegisterProfiles(List<ContentVisualProfile> profiles, StringBuilder report)
    {
        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);

        if (registry == null)
        {
            report.AppendLine($"[경고] GameDataRegistry를 찾지 못해 Profile 등록을 건너뜀: {RegistryPath}");
            return;
        }

        SerializedObject serializedRegistry = new SerializedObject(registry);
        SerializedProperty list = serializedRegistry.FindProperty("visualProfiles");
        HashSet<Object> existing = new HashSet<Object>();

        for (int index = 0; index < list.arraySize; index++)
        {
            existing.Add(list.GetArrayElementAtIndex(index).objectReferenceValue);
        }

        int added = 0;

        for (int index = 0; index < profiles.Count; index++)
        {
            if (profiles[index] == null || existing.Contains(profiles[index]))
            {
                continue;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = profiles[index];
            existing.Add(profiles[index]);
            added++;
        }

        serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
        report.AppendLine($"GameDataRegistry Visual Profile 신규 등록 {added}개");
    }
}
