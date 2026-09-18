using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 78일차: 현재 열린 Gameplay Scene을 저폴리 스타일 맵으로 꾸민다.
// Terrain 채색, 나무·바위·풀 배치, 거점 장식, 원경 산, 하늘, 플레이어·도구 외형을 적용한다.
public static class StylizedSceneDresser
{
    public const string EnvironmentRootName = "=== Stylized Environment ===";
    private const int Seed = 7823;

    private sealed class PlacementContext
    {
        public Terrain Terrain;
        public Bounds TerrainBounds;
        public Vector3 PlayCenter;
        public float PlayRadius;
        public readonly List<(Vector3 center, float radius)> Blocked = new List<(Vector3, float)>();
        public readonly List<Bounds> BlockedRects = new List<Bounds>();
        public readonly List<(Vector3 from, Vector3 to, float width)> Paths = new List<(Vector3, Vector3, float)>();
        public readonly List<(Vector3 position, float radius)> Placed = new List<(Vector3, float)>();
        public System.Random Random;
    }

    public static string DressActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        StringBuilder report = new StringBuilder();
        report.AppendLine($"Scene 꾸미기 : {scene.name}");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "정리되지 않은 임시 오브젝트 확인", 0.05f);
            report.AppendLine(CleanupOrphanVisuals());

            EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "플레이어·도구 외형", 0.15f);
            report.AppendLine(ApplyCharacterAndToolVisuals());

            EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "테스트 지형 재질", 0.25f);
            report.AppendLine(RestyleTestGeometry());

            PlacementContext context = BuildContext(report);

            if (context != null)
            {
                EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "Terrain 채색", 0.35f);
                StylizedTerrainPainter.PaintZones zones = BuildPaintZones(context);
                report.AppendLine(StylizedTerrainPainter.Paint(context.Terrain, zones, true));

                EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "나무와 바위 배치", 0.6f);
                report.AppendLine(BuildEnvironment(context));
            }

            EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "하늘과 조명", 0.85f);
            report.AppendLine(ApplySky());

            EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "저장용 ID 검사", 0.88f);
            report.AppendLine(AssignSaveIds());

            EditorUtility.DisplayProgressBar("Project U 맵 꾸미기", "NavMesh 다시 굽기", 0.92f);
            report.AppendLine(RebakeNavMesh());
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        report.AppendLine("Scene을 저장(Ctrl+S)하면 적용 내용이 유지됩니다.");
        return report.ToString();
    }

    public static string RemoveEnvironment()
    {
        GameObject root = GameObject.Find(EnvironmentRootName);

        if (root == null)
        {
            return "제거할 저폴리 환경 오브젝트가 없습니다.";
        }

        Undo.DestroyObjectImmediate(root);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return "저폴리 환경 오브젝트를 제거했습니다. (Terrain 채색은 Undo로 되돌리세요)";
    }

    // ----------------------------------------------------------------- 정리

    private static readonly HashSet<string> OrphanNames = new HashSet<string>
    {
        "Visual", "VisualInstance", "EffectOrigin", "UIAnchor", "InteractionPoint"
    };

    private static string CleanupOrphanVisuals()
    {
        Scene scene = SceneManager.GetActiveScene();
        List<GameObject> candidates = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            bool nameMatches = OrphanNames.Contains(root.name)
                || (root.name.StartsWith("TEMP_") && root.name.EndsWith("_Visual"));

            if (!nameMatches || root.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
            {
                continue;
            }

            candidates.Add(root);
        }

        if (candidates.Count == 0)
        {
            return "정리할 임시 오브젝트 없음";
        }

        HashSet<Object> referenced = CollectReferencedObjects(scene);
        int removed = 0;

        for (int index = 0; index < candidates.Count; index++)
        {
            GameObject candidate = candidates[index];
            bool isReferenced = false;

            foreach (Component component in candidate.GetComponentsInChildren<Component>(true))
            {
                if (component != null && (referenced.Contains(component) || referenced.Contains(component.gameObject)))
                {
                    isReferenced = true;
                    break;
                }
            }

            if (isReferenced)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(candidate);
            removed++;
        }

        return $"Scene 루트에 남은 임시 외형 오브젝트 {removed}개 정리";
    }

    private static HashSet<Object> CollectReferencedObjects(Scene scene)
    {
        HashSet<Object> result = new HashSet<Object>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(behaviour);
                SerializedProperty property = serialized.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                    {
                        result.Add(property.objectReferenceValue);
                    }
                }
            }
        }

        return result;
    }

    // ----------------------------------------------------------------- 캐릭터·도구

    private static string ApplyCharacterAndToolVisuals()
    {
        StringBuilder report = new StringBuilder();
        ThirdPersonCameraFollow cameraFollow = Object.FindFirstObjectByType<ThirdPersonCameraFollow>(FindObjectsInactive.Include);
        Transform playerVisual = null;

        if (cameraFollow != null)
        {
            SerializedObject serialized = new SerializedObject(cameraFollow);
            SerializedProperty property = serialized.FindProperty("playerVisualRoot");
            playerVisual = property != null ? property.objectReferenceValue as Transform : null;
        }

        if (playerVisual == null)
        {
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
            Transform found = player != null ? player.transform.Find("PlayerVisual") : null;
            playerVisual = found;
        }

        if (playerVisual != null)
        {
            StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
            {
                ParentOverride = playerVisual,
                UseUndo = true
            };
            StylizedVisualReplacer.Replace(playerVisual.gameObject, "char_player", options, out string message);
            report.AppendLine(message);
        }
        else
        {
            report.AppendLine("[건너뜀] 플레이어 외형 루트를 찾지 못함");
        }

        EquippedToolView toolView = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (toolView != null)
        {
            SerializedObject serialized = new SerializedObject(toolView);
            ReplaceTool(serialized.FindProperty("axeVisual"), "tool_stone_axe", report);
            ReplaceTool(serialized.FindProperty("pickaxeVisual"), "tool_pickaxe", report);
        }

        // Scene에 직접 배치된 월드 아이템
        foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(pickup.gameObject))
            {
                // 다른 아이템으로 바꿔 쓴 Prefab 인스턴스 (예: 사과 Prefab을 활 아이템으로 사용)
                string overrideReport = ApplyPickupInstanceOverride(pickup);

                if (overrideReport != null)
                {
                    report.AppendLine(overrideReport);
                }

                continue;
            }

            string modelId = GuessItemModel(pickup);

            if (modelId == null)
            {
                continue;
            }

            StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
            {
                UseUndo = true,
                AlignModelUpToLongestAxis = modelId.StartsWith("tool_")
            };

            if (StylizedVisualReplacer.Replace(pickup.gameObject, modelId, options, out string message))
            {
                report.AppendLine(message);
            }
        }

        TrainingDamageTarget dummy = Object.FindFirstObjectByType<TrainingDamageTarget>(FindObjectsInactive.Include);

        if (dummy != null)
        {
            StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options { UseUndo = true };
            StylizedVisualReplacer.Replace(dummy.gameObject, "training_dummy", options, out string message);
            report.AppendLine(message);
        }

        return report.ToString().TrimEnd();
    }

    private static void ReplaceTool(SerializedProperty property, string modelId, StringBuilder report)
    {
        GameObject tool = property != null ? property.objectReferenceValue as GameObject : null;

        if (tool == null)
        {
            return;
        }

        StylizedVisualReplacer.Options options = new StylizedVisualReplacer.Options
        {
            ParentOverride = tool.transform,
            AlignModelUpToLongestAxis = true,
            BladeHintObjectName = "Head",
            UseUndo = true
        };

        StylizedVisualReplacer.Replace(tool, modelId, options, out string message);
        report.AppendLine(message);
    }

    private const string InstanceOverrideName = "LP_Visual_Override";

    private static string ApplyPickupInstanceOverride(WorldItemPickup pickup)
    {
        StylizedVisualReplacement record = pickup.GetComponent<StylizedVisualReplacement>();
        string desired = GuessOverrideModel(pickup);

        if (record == null || record.GeneratedVisual == null || desired == null || desired == record.ModelId)
        {
            return null;
        }

        Transform inherited = record.GeneratedVisual.transform;
        Transform parent = inherited.parent;
        Transform previous = parent.Find(InstanceOverrideName);

        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous.gameObject);
        }

        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(desired, false);
        MeshFilter inheritedFilter = inherited.GetComponent<MeshFilter>();

        if (prefab == null || inheritedFilter == null || inheritedFilter.sharedMesh == null)
        {
            return null;
        }

        // 원래 모델이 차지하던 크기에 맞춰 새 모델을 배치
        Bounds oldBounds = inheritedFilter.sharedMesh.bounds;
        Vector3 oldSize = Vector3.Scale(oldBounds.size, inherited.localScale);
        Vector3 oldBottom = inherited.localPosition + inherited.localRotation
            * Vector3.Scale(new Vector3(oldBounds.center.x, oldBounds.min.y, oldBounds.center.z), inherited.localScale);
        Mesh newMesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        Quaternion rotation = desired.StartsWith("tool_") ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
        Bounds newBounds = newMesh.bounds;
        Vector3 rotatedSize = rotation * newBounds.size;
        rotatedSize = new Vector3(Mathf.Abs(rotatedSize.x), Mathf.Abs(rotatedSize.y), Mathf.Abs(rotatedSize.z));
        float scale = Mathf.Max(oldSize.x, oldSize.y, oldSize.z) / Mathf.Max(0.0001f, Mathf.Max(rotatedSize.x, rotatedSize.y, rotatedSize.z));
        Vector3 newBottom = rotation * (new Vector3(newBounds.center.x, newBounds.center.y, newBounds.center.z) * scale);
        float rotatedMinY = newBottom.y - rotatedSize.y * scale * 0.5f;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(visual, "Override Pickup Visual");
        visual.name = InstanceOverrideName;
        visual.transform.localRotation = rotation;
        visual.transform.localScale = Vector3.one * scale;
        visual.transform.localPosition = new Vector3(oldBottom.x - newBottom.x, oldBottom.y - rotatedMinY, oldBottom.z - newBottom.z);
        StylizedVisualReplacer.SetLayerRecursively(visual.transform, parent.gameObject.layer);

        Undo.RecordObject(inherited.gameObject, "Override Pickup Visual");
        inherited.gameObject.SetActive(false);
        return $"{pickup.name}: 인스턴스 외형 {record.ModelId} → {desired}";
    }

    // 전용 Prefab이 없는 아이템만 인스턴스 외형을 덮어쓴다
    private static string GuessOverrideModel(WorldItemPickup pickup)
    {
        string key = (pickup.name + " " + (pickup.ItemData != null ? pickup.ItemData.ItemId : string.Empty)).ToLowerInvariant();

        if (key.Contains("arrow")) return "projectile_arrow";
        if (key.Contains("bow")) return "tool_bow";
        return null;
    }

    private static string GuessItemModel(WorldItemPickup pickup)
    {
        string name = pickup.name.ToLowerInvariant();
        string itemId = pickup.ItemData != null ? pickup.ItemData.ItemId.ToLowerInvariant() : string.Empty;
        string key = name + " " + itemId;

        if (key.Contains("arrow")) return "projectile_arrow";
        if (key.Contains("bow")) return "tool_bow";
        if (key.Contains("pickaxe")) return "tool_pickaxe";
        if (key.Contains("axe")) return "tool_stone_axe";
        if (key.Contains("cloth")) return "item_cloth";
        if (key.Contains("wood")) return "item_wood_bundle";
        if (key.Contains("stone")) return "item_stone";
        return null;
    }

    private static string RestyleTestGeometry()
    {
        int count = 0;
        GameObject mapRoot = GameObject.Find("=== Map ===");

        if (mapRoot == null)
        {
            return "테스트 지형 없음";
        }

        foreach (Transform child in mapRoot.transform)
        {
            StylizedColor? color = null;

            if (child.name.StartsWith("Slope_"))
            {
                color = StylizedColor.Stone;
            }
            else if (child.name.StartsWith("CameraWall"))
            {
                color = StylizedColor.StoneDark;
            }
            else if (child.name.Contains("Roof"))
            {
                color = StylizedColor.WoodPlank;
            }

            MeshRenderer renderer = child.GetComponent<MeshRenderer>();

            if (color == null || renderer == null)
            {
                continue;
            }

            Undo.RecordObject(renderer, "Restyle Test Geometry");
            renderer.sharedMaterial = StylizedArtAssetFactory.GetMaterial(color.Value);
            count++;
        }

        return $"테스트 지형 재질 변경 {count}개";
    }

    // ----------------------------------------------------------------- 배치 정보

    private static PlacementContext BuildContext(StringBuilder report)
    {
        Terrain terrain = Terrain.activeTerrain != null
            ? Terrain.activeTerrain
            : Object.FindFirstObjectByType<Terrain>();

        if (terrain == null || terrain.terrainData == null)
        {
            report.AppendLine("[건너뜀] Terrain을 찾지 못해 맵 채색과 배치를 생략");
            return null;
        }

        PlacementContext context = new PlacementContext
        {
            Terrain = terrain,
            Random = new System.Random(Seed)
        };

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        context.TerrainBounds = new Bounds(origin + size * 0.5f, size);

        List<Vector3> keyPoints = new List<Vector3>();
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        if (player != null)
        {
            keyPoints.Add(player.transform.position);
            context.Blocked.Add((player.transform.position, 7f));
        }

        GameObject respawn = GameObject.Find("-- DefaultRespawnPoint --");

        if (respawn != null)
        {
            keyPoints.Add(respawn.transform.position);
            context.Blocked.Add((respawn.transform.position, 6f));
        }

        BuildGridArea grid = Object.FindFirstObjectByType<BuildGridArea>(FindObjectsInactive.Include);

        if (grid != null)
        {
            Bounds gridBounds = GetGridBounds(grid);
            gridBounds.Expand(new Vector3(4f, 0f, 4f));
            context.BlockedRects.Add(gridBounds);
            keyPoints.Add(gridBounds.center);
        }

        foreach (EnemySpawnPoint spawn in Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            keyPoints.Add(spawn.transform.position);
            context.Blocked.Add((spawn.transform.position, 6f));
        }

        foreach (GatherableResource resource in Object.FindObjectsByType<GatherableResource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            context.Blocked.Add((resource.transform.position, 2.5f));
        }

        foreach (WorldItemPickup pickup in Object.FindObjectsByType<WorldItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            context.Blocked.Add((pickup.transform.position, 1.5f));
        }

        GameObject mapRoot = GameObject.Find("=== Map ===");

        if (mapRoot != null)
        {
            foreach (Transform child in mapRoot.transform)
            {
                if (child.GetComponent<Terrain>() != null)
                {
                    continue;
                }

                Collider collider = child.GetComponent<Collider>();

                if (collider != null)
                {
                    Bounds bounds = collider.bounds;
                    bounds.Expand(new Vector3(3f, 0f, 3f));
                    context.BlockedRects.Add(bounds);
                }
            }
        }

        if (keyPoints.Count == 0)
        {
            keyPoints.Add(context.TerrainBounds.center);
        }

        Vector3 center = Vector3.zero;

        foreach (Vector3 point in keyPoints)
        {
            center += point;
        }

        center /= keyPoints.Count;
        float radius = 12f;

        foreach (Vector3 point in keyPoints)
        {
            radius = Mathf.Max(radius, FlatDistance(point, center) + 10f);
        }

        context.PlayCenter = center;
        context.PlayRadius = radius;

        // 거점 → 각 주요 지점으로 흙길 연결
        Vector3 hub = grid != null ? GetGridBounds(grid).center : center;

        foreach (Vector3 point in keyPoints)
        {
            if (FlatDistance(point, hub) > 3f)
            {
                context.Paths.Add((hub, point, 3.2f));
            }
        }

        return context;
    }

    private static Bounds GetGridBounds(BuildGridArea grid)
    {
        float width = grid.GridWidth * grid.CellSize;
        float depth = grid.GridDepth * grid.CellSize;
        Bounds bounds = new Bounds(grid.transform.TransformPoint(Vector3.zero), Vector3.zero);
        bounds.Encapsulate(grid.transform.TransformPoint(new Vector3(width, 0f, 0f)));
        bounds.Encapsulate(grid.transform.TransformPoint(new Vector3(0f, 0f, depth)));
        bounds.Encapsulate(grid.transform.TransformPoint(new Vector3(width, 0f, depth)));
        return bounds;
    }

    private static StylizedTerrainPainter.PaintZones BuildPaintZones(PlacementContext context)
    {
        StylizedTerrainPainter.PaintZones zones = new StylizedTerrainPainter.PaintZones();
        zones.Paths.AddRange(context.Paths);

        BuildGridArea grid = Object.FindFirstObjectByType<BuildGridArea>(FindObjectsInactive.Include);

        if (grid != null)
        {
            Bounds gridBounds = GetGridBounds(grid);
            zones.NoGrassRects.Add(gridBounds);
            zones.DirtCircles.Add((gridBounds.center, 3.5f));
        }

        foreach ((Vector3 center, float radius) in context.Blocked)
        {
            zones.NoGrassCircles.Add((center, Mathf.Min(radius, 2.5f)));
        }

        return zones;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    // ----------------------------------------------------------------- 배치

    private static string BuildEnvironment(PlacementContext context)
    {
        GameObject existing = GameObject.Find(EnvironmentRootName);

        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(EnvironmentRootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Stylized Environment");
        Transform trees = CreateGroup(root.transform, "Trees");
        Transform rocks = CreateGroup(root.transform, "Rocks");
        Transform plants = CreateGroup(root.transform, "Plants");
        Transform camp = CreateGroup(root.transform, "Camp");
        Transform backdrop = CreateGroup(root.transform, "Backdrop");

        Vector3 size = context.TerrainBounds.size;
        float area = size.x * size.z;
        int treeCount = Mathf.Clamp(Mathf.RoundToInt(area / 180f), 40, 260);
        int rockCount = Mathf.Clamp(Mathf.RoundToInt(area / 700f), 15, 90);
        int bushCount = Mathf.Clamp(Mathf.RoundToInt(area / 320f), 25, 160);
        int smallCount = Mathf.Clamp(Mathf.RoundToInt(area / 160f), 40, 260);

        string[] treeModels = { "tree_round", "tree_round_b", "tree_pine", "tree_pine", "tree_autumn" };
        int placedTrees = Scatter(context, trees, treeCount, treeModels, 3.2f, 5.5f, 1.2f, 0.85f, 1.45f, 24f, true, PropCollider.Trunk);
        int placedRocks = Scatter(context, rocks, rockCount, new[] { "rock_large", "rock_large", "stump", "fallen_log" }, 2.5f, 1.4f, 0.2f, 0.8f, 2.2f, 35f, true, PropCollider.Box);
        int placedBushes = Scatter(context, plants, bushCount, new[] { "bush", "bush", "bush_berry", "rock_small", "mushroom_cluster" }, 1.2f, 1f, 0.35f, 0.7f, 1.5f, 30f, false, PropCollider.None);
        int placedSmall = Scatter(context, plants, smallCount, new[] { "flower_patch", "grass_tuft", "grass_tuft", "pebbles", "reeds" }, 0.6f, 0.4f, 0.05f, 0.8f, 1.6f, 30f, false, PropCollider.None);
        string campReport = BuildCamp(context, camp);
        int mountains = BuildBackdrop(context, backdrop);

        return $"환경 배치 : 나무 {placedTrees} / 바위·통나무 {placedRocks} / 덤불 {placedBushes} / 꽃·풀 {placedSmall} / 원경 산 {mountains}\n{campReport}";
    }

    private enum PropCollider
    {
        None,
        Trunk,
        Box
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(group, "Build Stylized Environment");
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    // playBias : 0이면 전체 고르게, 클수록 플레이 구역 바깥에 몰린다
    private static int Scatter(
        PlacementContext context,
        Transform parent,
        int count,
        string[] models,
        float spacing,
        float blockRadius,
        float playBias,
        float minScale,
        float maxScale,
        float maxSlope,
        bool isStatic,
        PropCollider colliderType)
    {
        Bounds bounds = context.TerrainBounds;
        float margin = 2f;
        int placed = 0;
        int attempts = count * 25;

        for (int attempt = 0; attempt < attempts && placed < count; attempt++)
        {
            float x = Mathf.Lerp(bounds.min.x + margin, bounds.max.x - margin, (float)context.Random.NextDouble());
            float z = Mathf.Lerp(bounds.min.z + margin, bounds.max.z - margin, (float)context.Random.NextDouble());
            Vector3 position = new Vector3(x, 0f, z);

            // 플레이 구역 근처는 확률을 낮춰 시야와 이동을 방해하지 않게 한다
            float distance = FlatDistance(position, context.PlayCenter);
            float edge = Mathf.InverseLerp(context.PlayRadius * 0.4f, context.PlayRadius * 1.3f, distance);
            float chance = Mathf.Lerp(1f - playBias, 1f, edge);

            if (context.Random.NextDouble() > chance)
            {
                continue;
            }

            if (!IsFree(context, position, blockRadius, spacing))
            {
                continue;
            }

            Vector3 normalized = new Vector3((x - bounds.min.x) / bounds.size.x, 0f, (z - bounds.min.z) / bounds.size.z);

            if (context.Terrain.terrainData.GetSteepness(normalized.x, normalized.z) > maxSlope)
            {
                continue;
            }

            position.y = context.Terrain.SampleHeight(position) + context.Terrain.transform.position.y;
            string modelId = models[context.Random.Next(models.Length)];
            float scale = Mathf.Lerp(minScale, maxScale, (float)context.Random.NextDouble());
            float yaw = (float)context.Random.NextDouble() * 360f;

            GameObject instance = PlaceModel(parent, modelId, position, yaw, scale, isStatic);

            if (instance == null)
            {
                continue;
            }

            AddPropCollider(instance, colliderType, modelId);
            context.Placed.Add((position, spacing));
            placed++;
        }

        return placed;
    }

    private static bool IsFree(PlacementContext context, Vector3 position, float blockRadius, float spacing)
    {
        foreach ((Vector3 center, float radius) in context.Blocked)
        {
            if (FlatDistance(position, center) < radius + blockRadius)
            {
                return false;
            }
        }

        foreach (Bounds rect in context.BlockedRects)
        {
            if (position.x > rect.min.x - blockRadius && position.x < rect.max.x + blockRadius
                && position.z > rect.min.z - blockRadius && position.z < rect.max.z + blockRadius)
            {
                return false;
            }
        }

        foreach ((Vector3 from, Vector3 to, float width) in context.Paths)
        {
            if (StylizedTerrainPainter.DistanceToSegmentXZ(position, from, to) < width * 0.5f + blockRadius * 0.6f)
            {
                return false;
            }
        }

        foreach ((Vector3 placedPosition, float placedSpacing) in context.Placed)
        {
            float minimum = Mathf.Max(spacing, placedSpacing) * 0.75f;

            if ((placedPosition - position).sqrMagnitude < minimum * minimum && Mathf.Abs(placedPosition.x - position.x) < minimum)
            {
                if (FlatDistance(placedPosition, position) < minimum)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static GameObject PlaceModel(Transform parent, string modelId, Vector3 position, float yaw, float scale, bool isStatic)
    {
        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);

        if (prefab == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Place Stylized Model");
        instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        instance.transform.localScale = Vector3.one * scale;

        if (isStatic)
        {
            GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        return instance;
    }

    private static void AddPropCollider(GameObject instance, PropCollider colliderType, string modelId)
    {
        if (colliderType == PropCollider.None)
        {
            return;
        }

        Bounds meshBounds = instance.GetComponent<MeshFilter>().sharedMesh.bounds;

        if (colliderType == PropCollider.Trunk)
        {
            CapsuleCollider capsule = Undo.AddComponent<CapsuleCollider>(instance);
            float trunkHeight = modelId.StartsWith("tree_pine") ? 3f : 2.4f;
            capsule.radius = 0.32f;
            capsule.height = trunkHeight;
            capsule.center = new Vector3(0f, trunkHeight * 0.5f, 0f);
            return;
        }

        BoxCollider box = Undo.AddComponent<BoxCollider>(instance);
        box.center = meshBounds.center;
        box.size = Vector3.Scale(meshBounds.size, new Vector3(0.85f, 0.95f, 0.85f));
    }

    // 거점 주변 울타리·등불·천막 등
    private static string BuildCamp(PlacementContext context, Transform parent)
    {
        BuildGridArea grid = Object.FindFirstObjectByType<BuildGridArea>(FindObjectsInactive.Include);
        Bounds campBounds;

        if (grid != null)
        {
            campBounds = GetGridBounds(grid);
        }
        else
        {
            GameObject respawn = GameObject.Find("-- DefaultRespawnPoint --");
            Vector3 center = respawn != null ? respawn.transform.position : context.PlayCenter;
            campBounds = new Bounds(center, new Vector3(16f, 0f, 16f));
        }

        float margin = 2.2f;
        Vector3 min = campBounds.min - new Vector3(margin, 0f, margin);
        Vector3 max = campBounds.max + new Vector3(margin, 0f, margin);
        int fences = 0;
        int segmentIndex = 0;

        // 네 변을 따라 2m 울타리를 두르고, 길이 지나가는 곳과 일정 간격은 비워 둔다
        Vector3[] corners =
        {
            new Vector3(min.x, 0f, min.z), new Vector3(max.x, 0f, min.z),
            new Vector3(max.x, 0f, max.z), new Vector3(min.x, 0f, max.z)
        };

        for (int side = 0; side < 4; side++)
        {
            Vector3 from = corners[side];
            Vector3 to = corners[(side + 1) % 4];
            float length = FlatDistance(from, to);
            int segments = Mathf.Max(1, Mathf.FloorToInt(length / 2.1f));
            Vector3 direction = (to - from).normalized;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg - 90f;

            for (int index = 0; index < segments; index++)
            {
                segmentIndex++;
                Vector3 position = from + direction * (length * (index + 0.5f) / segments);

                if (segmentIndex % 5 == 0 || IsOnPath(context, position, 2.5f))
                {
                    continue;
                }

                position.y = SampleHeight(context, position);
                GameObject fence = PlaceModel(parent, "prop_fence", position, yaw, 1f, true);

                if (fence != null)
                {
                    BoxCollider box = Undo.AddComponent<BoxCollider>(fence);
                    box.center = new Vector3(0f, 0.55f, 0.05f);
                    box.size = new Vector3(2.1f, 1.1f, 0.2f);
                    fences++;
                }
            }
        }

        int props = 0;

        for (int side = 0; side < 4; side++)
        {
            Vector3 corner = corners[side];
            Vector3 inward = (campBounds.center - corner);
            inward.y = 0f;
            Vector3 lanternPosition = corner + inward.normalized * 0.8f;
            lanternPosition.y = SampleHeight(context, lanternPosition);

            if (PlaceLantern(parent, lanternPosition, side * 90f) != null)
            {
                props++;
            }
        }

        // 거점 바깥 한쪽에 천막·장작더미·상자·우물·깃발 묶음 배치
        Vector3 outsideDirection = FindQuietDirection(context, campBounds);
        Vector3 anchor = campBounds.center + outsideDirection * (Mathf.Max(campBounds.extents.x, campBounds.extents.z) + margin + 5f);
        float facing = Mathf.Atan2(-outsideDirection.x, -outsideDirection.z) * Mathf.Rad2Deg;
        Vector3 side90 = Quaternion.Euler(0f, 90f, 0f) * outsideDirection;

        (string model, Vector3 offset, float yawOffset, PropCollider collider)[] layout =
        {
            ("prop_tent", Vector3.zero, 90f, PropCollider.Box),
            ("prop_woodpile", side90 * 3.2f, 0f, PropCollider.Box),
            ("prop_crate", side90 * 3.4f + outsideDirection * 2f, 15f, PropCollider.Box),
            ("prop_barrel", side90 * 2.4f + outsideDirection * 2.4f, 0f, PropCollider.Box),
            ("prop_well", -side90 * 4.5f, 0f, PropCollider.Box),
            ("prop_banner", -side90 * 2.2f - outsideDirection * 2.6f, 0f, PropCollider.None),
            ("prop_bench", -side90 * 1.6f - outsideDirection * 1.2f, 90f, PropCollider.Box),
            ("prop_signpost", -outsideDirection * 3.4f + side90 * 1.4f, 0f, PropCollider.None)
        };

        foreach ((string model, Vector3 offset, float yawOffset, PropCollider collider) in layout)
        {
            Vector3 position = anchor + offset;

            if (!context.TerrainBounds.Contains(new Vector3(position.x, context.TerrainBounds.center.y, position.z)))
            {
                continue;
            }

            if (!IsFree(context, position, 1.2f, 1.5f))
            {
                continue;
            }

            position.y = SampleHeight(context, position);
            GameObject instance = PlaceModel(parent, model, position, facing + yawOffset, 1f, true);

            if (instance != null)
            {
                AddPropCollider(instance, collider, model);
                context.Placed.Add((position, 2f));
                props++;
            }
        }

        return $"거점 장식 : 울타리 {fences} / 소품 {props}";
    }

    private static GameObject PlaceLantern(Transform parent, Vector3 position, float yaw)
    {
        GameObject lantern = PlaceModel(parent, "prop_lantern_post", position, yaw, 1f, true);

        if (lantern == null)
        {
            return null;
        }

        GameObject lightObject = new GameObject("LanternLight");
        Undo.RegisterCreatedObjectUndo(lightObject, "Place Lantern Light");
        lightObject.transform.SetParent(lantern.transform, false);
        lightObject.transform.localPosition = new Vector3(0.5f, 1.8f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.78f, 0.45f);
        light.intensity = 1.6f;
        light.range = 7f;
        light.shadows = LightShadows.None;

        BoxCollider box = Undo.AddComponent<BoxCollider>(lantern);
        box.center = new Vector3(0f, 1.1f, 0f);
        box.size = new Vector3(0.3f, 2.2f, 0.3f);
        return lantern;
    }

    private static Vector3 FindQuietDirection(PlacementContext context, Bounds campBounds)
    {
        Vector3 best = Vector3.back;
        float bestScore = float.MinValue;

        for (int index = 0; index < 8; index++)
        {
            Vector3 direction = Quaternion.Euler(0f, index * 45f, 0f) * Vector3.forward;
            Vector3 probe = campBounds.center + direction * (Mathf.Max(campBounds.extents.x, campBounds.extents.z) + 7f);
            float score = 0f;

            // 주요 지점·길에서 멀수록, Terrain 안쪽일수록 좋은 위치
            foreach ((Vector3 center, float radius) in context.Blocked)
            {
                score += Mathf.Min(20f, FlatDistance(probe, center) - radius);
            }

            if (IsOnPath(context, probe, 4f))
            {
                score -= 200f;
            }

            if (!context.TerrainBounds.Contains(new Vector3(probe.x, context.TerrainBounds.center.y, probe.z)))
            {
                score -= 500f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = direction;
            }
        }

        return best;
    }

    private static bool IsOnPath(PlacementContext context, Vector3 position, float extra)
    {
        foreach ((Vector3 from, Vector3 to, float width) in context.Paths)
        {
            if (StylizedTerrainPainter.DistanceToSegmentXZ(position, from, to) < width * 0.5f + extra)
            {
                return true;
            }
        }

        return false;
    }

    private static float SampleHeight(PlacementContext context, Vector3 position)
    {
        return context.Terrain.SampleHeight(position) + context.Terrain.transform.position.y;
    }

    private static int BuildBackdrop(PlacementContext context, Transform parent)
    {
        Bounds bounds = context.TerrainBounds;
        float ring = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f + 45f;
        int count = 16;
        int placed = 0;

        for (int index = 0; index < count; index++)
        {
            float angle = (index + (float)context.Random.NextDouble() * 0.4f) / count * Mathf.PI * 2f;
            float distance = ring + (float)context.Random.NextDouble() * 60f;
            Vector3 position = bounds.center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            position.y = bounds.min.y - 2f;
            string model = index % 3 == 0 ? "mountain_low" : "mountain";
            float scale = Mathf.Lerp(1.4f, 2.8f, (float)context.Random.NextDouble());

            if (PlaceModel(parent, model, position, (float)context.Random.NextDouble() * 360f, scale, true) != null)
            {
                placed++;
            }
        }

        return placed;
    }

    // ----------------------------------------------------------------- 하늘

    private static string ApplySky()
    {
        StylizedArtAssetFactory.EnsureFolders();
        string path = $"{StylizedArtAssetFactory.MaterialFolder}/M_Skybox_Stylized.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (sky == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");

            if (shader == null)
            {
                return "[건너뜀] Skybox/Procedural 셰이더 없음";
            }

            sky = new Material(shader);
            AssetDatabase.CreateAsset(sky, path);
        }

        sky.SetFloat("_SunDisk", 2f);
        sky.SetFloat("_SunSize", 0.045f);
        sky.SetFloat("_SunSizeConvergence", 6f);
        sky.SetFloat("_AtmosphereThickness", 0.85f);
        sky.SetColor("_SkyTint", new Color(0.42f, 0.6f, 0.86f));
        sky.SetColor("_GroundColor", new Color(0.42f, 0.45f, 0.4f));
        sky.SetFloat("_Exposure", 1.2f);
        EditorUtility.SetDirty(sky);

        Undo.RecordObject(RenderSettings.skybox, "Apply Stylized Sky");
        RenderSettings.skybox = sky;
        RenderSettings.fogColor = new Color(0.66f, 0.76f, 0.86f);
        DynamicGI.UpdateEnvironment();
        return "하늘 Skybox 적용";
    }

    // ----------------------------------------------------------------- 저장 ID

    // 저장 시스템은 Scene의 월드 아이템·채집 자원·적 Spawn Point에 고유 ID가 있어야 초기화된다
    private static string AssignSaveIds()
    {
        WorldObjectIdValidator.AssignAndValidateWorldObjectIds();
        EnemySpawnPointIdTool.AssignEnemySpawnPointIds();
        return "저장용 ID 검사 실행 (월드 오브젝트, 적 Spawn Point)";
    }

    // ----------------------------------------------------------------- NavMesh

    private static string RebakeNavMesh()
    {
        NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (surfaces.Length == 0)
        {
            return "NavMeshSurface 없음 (굽기 생략)";
        }

        // 패키지 공식 굽기 도구를 사용해야 NavMesh 데이터가 에셋으로 저장된다
        Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.StartBakingSurfaces(surfaces);
        int baked = surfaces.Length;

        return $"NavMesh 다시 굽기 시작 {baked}개 (진행 표시가 끝난 뒤 Scene을 저장하세요)";
    }
}
