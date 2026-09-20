using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 120일차: 동굴 몬스터 데이터 · Prefab · 동굴 배치 · 보스 방
public static partial class CaveMonsterBuilder
{
    // ---------------------------------------------------------------- 몬스터 표

    public sealed class LootSpec
    {
        public string ItemId; // 얻는 아이템
        public float Chance = 1f; // 나올 확률
        public int Min = 1; // 최소 수량
        public int Max = 1; // 최대 수량
    }

    public sealed class MonsterSpec
    {
        public string Id; // 몬스터 ID
        public string Korean; // 한글 이름
        public string English; // 표시 이름
        public string Model; // 모델 ID
        public float Health = 40f; // 체력 (돌도끼 3 ~ 12번)
        public float Speed = 3.2f; // 이동 속도
        public float Damage = 8f; // 공격력
        public float AttackRange = 1.8f; // 공격 거리
        public float DarkRange = 7f; // 어두울 때 알아채는 거리
        public float LitRange = 16f; // 횃불을 들었을 때 거리
        public float Radius = 24f; // 영역 반경 (방 하나)
        public float Scale = 1f; // 모델 크기
        public float Collider = 0.4f; // 충돌체 반지름
        public float Respawn = 240f; // 다시 나오는 시간 (초)
        public int Layer = 1; // 1 입구층 · 2 깊은층
        public int PerRoom = 2; // 방 하나에 나오는 수
        public bool Flying; // 공중에 떠 있는지 (박쥐)
        public bool Boss; // 보스인지
        public LootSpec[] Loot = new LootSpec[0]; // 전리품
    }

    // 동굴 몬스터 3종과 보스
    public static readonly MonsterSpec[] Monsters =
    {
        new MonsterSpec
        {
            Id = "cave_bat", Korean = "박쥐", English = "CAVE BAT", Model = "monster_cave_bat",
            Health = 30f, Speed = 4.6f, Damage = 6f, AttackRange = 1.6f, DarkRange = 8f, LitRange = 18f,
            Radius = 26f, Scale = 1.1f, Collider = 0.3f, Respawn = 200f, Layer = 1, PerRoom = 1, Flying = true,
            Loot = new[] { new LootSpec { ItemId = "resource_bat_wing", Min = 1, Max = 2 }, new LootSpec { ItemId = "food_small_meat", Chance = 0.4f } }
        },
        new MonsterSpec
        {
            Id = "cave_spider", Korean = "동굴 거미", English = "CAVE SPIDER", Model = "monster_cave_spider",
            Health = 48f, Speed = 3.4f, Damage = 9f, AttackRange = 2f, DarkRange = 7f, LitRange = 15f,
            Radius = 24f, Scale = 1f, Collider = 0.45f, Respawn = 260f, Layer = 1, PerRoom = 1,
            Loot = new[] { new LootSpec { ItemId = "resource_venom_fang", Chance = 0.7f }, new LootSpec { ItemId = "item_spider_silk", Min = 1, Max = 2 } }
        },
        new MonsterSpec
        {
            Id = "stone_golem", Korean = "돌 골렘", English = "STONE GOLEM", Model = "monster_stone_golem",
            Health = 130f, Speed = 2.6f, Damage = 18f, AttackRange = 2.6f, DarkRange = 9f, LitRange = 16f,
            Radius = 26f, Scale = 1f, Collider = 0.7f, Respawn = 420f, Layer = 2, PerRoom = 1,
            Loot = new[] { new LootSpec { ItemId = "resource_stone", Min = 3, Max = 5 }, new LootSpec { ItemId = "item_iron_ore", Min = 1, Max = 2 }, new LootSpec { ItemId = "resource_crystal", Chance = 0.5f } }
        },
        new MonsterSpec
        {
            Id = "crystal_golem", Korean = "수정 골렘", English = "CRYSTAL GOLEM", Model = "monster_crystal_golem",
            Health = 600f, Speed = 2.8f, Damage = 22f, AttackRange = 3f, DarkRange = 16f, LitRange = 18f,
            Radius = 20f, Scale = 1f, Collider = 1.1f, Respawn = 0f, Layer = 2, PerRoom = 1, Boss = true,
            Loot = new[]
            {
                new LootSpec { ItemId = "resource_crystal_heart" },
                new LootSpec { ItemId = "resource_gem_diamond", Min = 1, Max = 2 },
                new LootSpec { ItemId = "resource_crystal", Min = 3, Max = 5 }
            }
        }
    };

    // 보스가 부르는 수정 조각
    public static readonly MonsterSpec Shardling = new MonsterSpec
    {
        Id = "crystal_shardling", Korean = "수정 조각", English = "CRYSTAL SHARDLING", Model = "monster_crystal_shard",
        Health = 30f, Speed = 3.6f, Damage = 7f, AttackRange = 1.8f, DarkRange = 16f, LitRange = 18f,
        Radius = 22f, Scale = 1f, Collider = 0.3f, Respawn = 0f, Layer = 2, PerRoom = 0,
        Loot = new[] { new LootSpec { ItemId = "resource_crystal", Chance = 0.6f } }
    };

    public static string MonsterPrefabPath(MonsterSpec spec) => $"{MonsterPrefabFolder}/Monster_{spec.Id}.prefab";

    public static string LootTablePath(MonsterSpec spec) => $"{LootFolder}/CaveLootTable_Monster_{spec.Id}.asset";

    public static string CombatDataPath(MonsterSpec spec) => $"{EnemyDataFolder}/EnemyCombatData_Monster_{spec.Id}.asset";

    // ---------------------------------------------------------------- 몬스터 데이터 · Prefab

    private static string BuildMonsterAssets()
    {
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        BuildBoltPrefab();
        int made = 0;

        foreach (MonsterSpec spec in Monsters.Concat(new[] { Shardling }))
        {
            CreateCombatData(spec);
            CreateLootTable(spec, registry);

            if (BuildMonsterPrefab(spec))
            {
                made++;
            }
        }

        AssetDatabase.SaveAssets();
        return $"동굴 몬스터 Prefab {made}종 (박쥐 · 동굴 거미 · 돌 골렘 · 수정 골렘 · 수정 조각)";
    }

    private static void CreateCombatData(MonsterSpec spec)
    {
        EnemyCombatData data = LoadOrCreate<EnemyCombatData>(CombatDataPath(spec));
        SerializedObject serialized = new SerializedObject(data);
        serialized.FindProperty("enemyId").stringValue = "enemy_" + spec.Id; // 외형 ID 규칙 : enemy_ 로 시작
        serialized.FindProperty("displayName").stringValue = spec.English;
        serialized.FindProperty("maximumHealth").floatValue = spec.Health;
        serialized.FindProperty("defensePercent").floatValue = spec.Boss ? 10f : 0f;
        serialized.FindProperty("moveSpeed").floatValue = spec.Speed;
        serialized.FindProperty("rotationSpeed").floatValue = spec.Boss ? 180f : 300f;
        serialized.FindProperty("detectionRange").floatValue = spec.LitRange;
        serialized.FindProperty("loseTargetRange").floatValue = spec.LitRange + 8f;
        serialized.FindProperty("attackRange").floatValue = spec.AttackRange;
        serialized.FindProperty("attackDamage").floatValue = spec.Damage;
        serialized.FindProperty("attackCooldown").floatValue = spec.Boss ? 1.6f : 1.1f;
        serialized.FindProperty("attackWindupDuration").floatValue = spec.Boss ? 0.8f : 0.5f;
        serialized.FindProperty("attackRecoveryDuration").floatValue = spec.Boss ? 0.7f : 0.4f;
        serialized.FindProperty("hitReactionDuration").floatValue = 0.2f;
        serialized.FindProperty("deathCleanupDelay").floatValue = spec.Boss ? 8f : 4f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    private static void CreateLootTable(MonsterSpec spec, WorldItemPickupRegistry registry)
    {
        EnemyLootTable table = LoadOrCreate<EnemyLootTable>(LootTablePath(spec));
        SerializedObject serialized = new SerializedObject(table);
        SerializedProperty entries = serialized.FindProperty("entries");
        LootSpec[] loot = spec.Loot.Where(entry => FindItem(entry.ItemId) != null).ToArray();
        entries.arraySize = loot.Length;

        for (int index = 0; index < loot.Length; index++)
        {
            ItemData item = FindItem(loot[index].ItemId);
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("itemData").objectReferenceValue = item;
            entry.FindPropertyRelative("dropChance").floatValue = loot[index].Chance;
            entry.FindPropertyRelative("minimumQuantity").intValue = loot[index].Min;
            entry.FindPropertyRelative("maximumQuantity").intValue = loot[index].Max;

            if (registry != null && item != null && registry.TryGetPickup(item, out WorldItemPickup pickup))
            {
                entry.FindPropertyRelative("pickupPrefab").objectReferenceValue = pickup;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
    }

    private static void BuildBoltPrefab() // 보스가 던지는 수정 조각 (날아가는 것)
    {
        if (!CopyPrefab(ProjectilePrefabPath, BoltPrefabPath))
        {
            return;
        }

        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab("fx_crystal_bolt", true);
        GameObject root = PrefabUtility.LoadPrefabContents(BoltPrefabPath);

        try
        {
            root.name = "CrystalBoltProjectile";
            ReplaceVisual(root, model, 1.4f);
            PrefabUtility.SaveAsPrefabAsset(root, BoltPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool BuildMonsterPrefab(MonsterSpec spec)
    {
        string path = MonsterPrefabPath(spec);
        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.Model, true);
        string template = spec.Boss ? SpitterPrefabPath : GruntPrefabPath;

        if (model == null || !CopyPrefab(template, path))
        {
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            root.name = "Monster_" + spec.Id;
            RemoveVisualProfileParts(root); // 몬스터는 외형 카드를 쓰지 않는다
            EnemyHealth health = root.GetComponentInChildren<EnemyHealth>(true);
            SerializedObject healthObject = new SerializedObject(health);
            healthObject.FindProperty("combatData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyCombatData>(CombatDataPath(spec));
            healthObject.FindProperty("logHealthResults").boolValue = false;
            healthObject.ApplyModifiedPropertiesWithoutUndo();

            EnemyLootDropper dropper = root.GetComponentInChildren<EnemyLootDropper>(true);
            SerializedObject dropperObject = new SerializedObject(dropper);
            dropperObject.FindProperty("lootTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(LootTablePath(spec));
            dropperObject.ApplyModifiedPropertiesWithoutUndo();

            CapsuleCollider capsule = root.GetComponentInChildren<CapsuleCollider>(true);

            if (capsule != null)
            {
                capsule.radius = spec.Collider;
                capsule.height = Mathf.Max(spec.Collider * 2.4f, spec.Boss ? 3.2f : spec.Collider * 4f);
                capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
            }

            UnityEngine.AI.NavMeshAgent agent = root.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>(true);

            if (agent != null)
            {
                agent.radius = Mathf.Max(0.25f, spec.Collider);
                agent.height = Mathf.Max(0.6f, spec.Boss ? 3.2f : spec.Collider * 4f);
                agent.speed = spec.Speed;
                agent.stoppingDistance = Mathf.Max(0.4f, spec.AttackRange * 0.45f);
            }

            CaveMonsterSense sense = root.GetComponent<CaveMonsterSense>();

            if (sense == null)
            {
                sense = root.AddComponent<CaveMonsterSense>();
            }

            sense.EditorAssign(spec.DarkRange, spec.LitRange);

            if (spec.Flying) // 박쥐는 떠서 흔들린다
            {
                if (root.GetComponent<CaveBatFlight>() == null)
                {
                    root.AddComponent<CaveBatFlight>();
                }
            }

            if (spec.Boss) // 보스 : 단계 관리자와 수정 던지기
            {
                EnemyRangedAttackController ranged = root.GetComponentInChildren<EnemyRangedAttackController>(true);

                if (ranged != null)
                {
                    SerializedObject rangedObject = new SerializedObject(ranged);
                    rangedObject.FindProperty("projectilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BoltPrefabPath)?.GetComponent<EnemyProjectile>();
                    rangedObject.FindProperty("projectileSpeed").floatValue = 16f;
                    rangedObject.FindProperty("logRangedAttackResults").boolValue = false;
                    rangedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                CaveBossController boss = root.GetComponent<CaveBossController>();

                if (boss == null)
                {
                    boss = root.AddComponent<CaveBossController>();
                }

                boss.EditorAssign(BossId, spec.Korean, null, AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath(Shardling)), 18f);
            }

            ReplaceVisual(root, model, spec.Scale);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return true;
    }

    private static void ReplaceVisual(GameObject root, GameObject model, float scale) // 외형을 몬스터 모델로 바꾼다
    {
        Transform visual = root.transform.Find("Visual") ?? root.transform.Find("VisualRoot");

        if (visual != null && HasVisualProfileParts(visual.gameObject)) // 외형 카드 부품이 남아 있으면 통째로 새로 만든다
        {
            string name = visual.name;
            UnityEngine.Object.DestroyImmediate(visual.gameObject);
            visual = new GameObject(name).transform;
            visual.SetParent(root.transform, false);
        }

        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
        }

        foreach (Transform child in visual.Cast<Transform>().ToList())
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        foreach (Component component in visual.GetComponents<MeshRenderer>().Cast<Component>().Concat(visual.GetComponents<MeshFilter>()))
        {
            UnityEngine.Object.DestroyImmediate(component, true);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * scale;
    }

    private static void RemoveVisualProfileParts(GameObject root) // 몬스터는 외형 카드를 쓰지 않는다
    {
        ContentVisualPartCleaner.RemoveParts(root);
    }

    private static bool HasVisualProfileParts(GameObject target)
    {
        return target.GetComponentsInChildren<ContentVisualRoot>(true).Length > 0
            || target.GetComponentsInChildren<ContentVisualProfileBinder>(true).Length > 0
            || target.GetComponentsInChildren<ContentVisualDataSourceBinder>(true).Length > 0
            || target.GetComponentsInChildren<ContentVisualIdentity>(true).Length > 0;
    }

    // ---------------------------------------------------------------- 동굴에 놓기

    private static string PlaceMonsters()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            return "게임 Scene이 열려 있지 않아 몬스터는 놓지 않았습니다.";
        }

        Terrain terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : UnityEngine.Object.FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            return "✗ 섬 지형이 없습니다.";
        }

        List<CaveBuilder.RoomInfo> rooms = CaveBuilder.RoomPlan(terrain);

        if (rooms.Count == 0)
        {
            return "✗ 동굴 방 자리를 찾지 못했습니다.";
        }

        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == RootName);

        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        foreach (Transform child in root.transform.Cast<Transform>().ToList()) // 다시 만들 때는 비우고 새로 놓는다
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        Transform runtimeParent = new GameObject(RuntimeParentName).transform;
        runtimeParent.SetParent(root.transform, false);
        Transform spawnGroup = new GameObject("MonsterSpawns").transform;
        spawnGroup.SetParent(root.transform, false);
        List<string> counts = new List<string>();
        int placed = 0;

        foreach (MonsterSpec spec in Monsters.Where(spec => !spec.Boss))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath(spec));
            int made = 0;

            foreach (CaveBuilder.RoomInfo room in rooms.Where(room => room.Layer == spec.Layer && !IsBossRoom(room)))
            {
                if (prefab == null)
                {
                    continue;
                }

                for (int index = 0; index < spec.PerRoom; index++)
                {
                    float angle = (index + 0.5f) / spec.PerRoom * Mathf.PI * 2f + room.Id.GetHashCode() % 7;
                    Vector3 spot = room.WorldCenter + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * room.Radius * 0.45f;
                    CreateSpawnPoint(spawnGroup, runtimeParent, spec, prefab, spot, room, index);
                    made++;
                    placed++;
                }
            }

            counts.Add($"{spec.Korean} {made}");
        }

        string bossReport = PlaceBoss(root.transform, rooms);
        EditorSceneManager.MarkSceneDirty(scene);
        return $"동굴에 몬스터 {placed}마리 ({string.Join(" · ", counts)}) · {bossReport}";
    }

    private static bool IsBossRoom(CaveBuilder.RoomInfo room) => room.Id == "deep_hall"; // 깊은층 가운데 방은 보스 방

    private static void CreateSpawnPoint(Transform parent, Transform runtimeParent, MonsterSpec spec, GameObject prefab, Vector3 spot, CaveBuilder.RoomInfo room, int index)
    {
        GameObject holder = new GameObject($"Spawn_{spec.Id}_{room.Id}_{index}");
        holder.transform.SetParent(parent, false);
        holder.transform.position = spot;
        EnemySpawnPoint point = holder.AddComponent<EnemySpawnPoint>();
        SerializedObject serialized = new SerializedObject(point);
        serialized.FindProperty("spawnPointId").stringValue = $"cave_{spec.Id}_{room.Id}_{index}";
        serialized.FindProperty("enemyPrefab").objectReferenceValue = prefab;
        serialized.FindProperty("runtimeEnemyParent").objectReferenceValue = runtimeParent;
        serialized.FindProperty("spawnOnStart").boolValue = true;
        serialized.FindProperty("initialSpawnDelay").floatValue = 0.5f + index * 0.35f;
        serialized.FindProperty("respawnEnabled").boolValue = true;
        serialized.FindProperty("respawnDelay").floatValue = spec.Respawn;
        serialized.FindProperty("corpseVisibleDuration").floatValue = 3f;
        serialized.FindProperty("spawnRadius").floatValue = 3f;
        serialized.FindProperty("navMeshSampleRadius").floatValue = 8f;
        serialized.FindProperty("logSpawnResults").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- 보스 방

    private static string PlaceBoss(Transform root, List<CaveBuilder.RoomInfo> rooms)
    {
        MonsterSpec spec = Monsters.First(monster => monster.Boss);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath(spec));
        CaveBuilder.RoomInfo? bossRoom = rooms.Where(IsBossRoom).Select(room => (CaveBuilder.RoomInfo?)room).FirstOrDefault();

        if (prefab == null || bossRoom == null)
        {
            return "✗ 보스를 놓지 못했습니다.";
        }

        CaveBuilder.RoomInfo room = bossRoom.Value;
        Transform group = new GameObject("BossRoom").transform;
        group.SetParent(root, false);

        GameObject gate = BuildGate(group, room);
        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
        boss.transform.SetPositionAndRotation(room.WorldCenter + Vector3.forward * room.Radius * 0.35f, Quaternion.Euler(0f, 180f, 0f));
        boss.name = "Monster_crystal_golem";
        CaveBossController controller = boss.GetComponent<CaveBossController>();

        if (controller != null)
        {
            controller.EditorAssign(BossId, spec.Korean, gate, AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath(Shardling)), room.Radius);
            EditorUtility.SetDirty(controller);
        }

        SetCaveLayer(boss);
        return $"보스 방 : {spec.Korean} 1마리 · 수정 문 1개 (깊은층 가운데 방)";
    }

    private static GameObject BuildGate(Transform parent, CaveBuilder.RoomInfo room) // 보스 방을 막는 수정 문
    {
        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab("cave_crystal_gate", true);

        if (model == null)
        {
            return null;
        }

        GameObject gate = new GameObject("CrystalGate");
        gate.transform.SetParent(parent, false);
        Vector3 toEntrance = new Vector3(0f, 0f, -1f); // 가운데 방으로 들어오는 굴 쪽
        gate.transform.SetPositionAndRotation(room.WorldCenter + toEntrance * (room.Radius * 0.92f), Quaternion.identity);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, gate.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localScale = new Vector3(6.5f, 5.2f, 1f);
        BoxCollider blocker = gate.AddComponent<BoxCollider>();
        blocker.size = new Vector3(6.5f, 5.2f, 0.6f);
        blocker.center = new Vector3(0f, 2.6f, 0f);
        SetCaveLayer(gate);
        gate.SetActive(false); // 싸움이 시작되면 켜진다
        return gate;
    }

    private static void SetCaveLayer(GameObject target) // 동굴 레이어로 옮긴다 (동굴 안 지도에 보이게)
    {
        int layer = LayerMask.NameToLayer("Cave");

        if (layer < 0)
        {
            return;
        }

        foreach (Transform item in target.GetComponentsInChildren<Transform>(true))
        {
            item.gameObject.layer = layer;
        }
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[동굴 몬스터 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        foreach (ItemSpec spec in Items)
        {
            ItemData item = FindItem(spec.Id);

            if (item == null || item.KoreanName != spec.Korean)
            {
                Error($"{spec.Id} 아이템이 없거나 한글 이름이 다릅니다.");
            }
        }

        foreach (MonsterSpec spec in Monsters.Concat(new[] { Shardling }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath(spec));
            EnemyHealth health = prefab != null ? prefab.GetComponentInChildren<EnemyHealth>(true) : null;
            CaveMonsterSense sense = prefab != null ? prefab.GetComponent<CaveMonsterSense>() : null;
            EnemyLootTable table = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(LootTablePath(spec));

            if (prefab == null || health == null || sense == null)
            {
                Error($"{spec.Id} 몬스터 Prefab이 없거나 부품이 빠졌습니다.");
                continue;
            }

            if (Mathf.Abs(health.MaximumHealth - spec.Health) > 0.1f)
            {
                Error($"{spec.Id} 체력이 표와 다릅니다 ({health.MaximumHealth}).");
            }

            if (table == null || table.Entries.Count == 0 || table.Entries.Any(entry => entry?.ItemData == null))
            {
                Error($"{spec.Id} 전리품 표가 없거나 아이템이 비어 있습니다.");
            }

            if (spec.Boss && prefab.GetComponent<CaveBossController>() == null)
            {
                Error("보스에 단계 관리자가 없습니다.");
            }

            if (spec.Flying && prefab.GetComponent<CaveBatFlight>() == null)
            {
                Error($"{spec.Id} 나는 모습 부품이 없습니다.");
            }
        }

        foreach (CraftSpec spec in Crafts)
        {
            CraftingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/{spec.Asset}.asset");

            if (recipe == null || recipe.ResultItem == null || recipe.Ingredients.Count != spec.Ingredients.Length)
            {
                Error($"{spec.Id} 제작법이 없거나 재료가 모자랍니다.");
            }
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(BoltPrefabPath) == null)
        {
            Error("보스가 던지는 수정 조각 Prefab이 없습니다.");
        }

        if (EditorSceneManager.GetActiveScene().path == ScenePath)
        {
            GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == RootName);
            EnemySpawnPoint[] spawns = root != null ? root.GetComponentsInChildren<EnemySpawnPoint>(true) : new EnemySpawnPoint[0];
            CaveBossController boss = root != null ? root.GetComponentInChildren<CaveBossController>(true) : null;

            if (root == null)
            {
                Error("Scene에 동굴 몬스터 묶음이 없습니다.");
            }
            else if (spawns.Length < 20)
            {
                Error($"동굴 몬스터 생성 지점이 {spawns.Length}곳입니다 (20곳 이상 목표).");
            }

            if (boss == null)
            {
                Error("Scene에 보스가 없습니다.");
            }
            else if (!boss.GateClosed && boss.transform.parent != null && boss.transform.parent.Find("CrystalGate") == null)
            {
                Error("보스 방에 수정 문이 없습니다.");
            }

            foreach (MonsterSpec spec in Monsters.Where(monster => !monster.Boss)
                .Where(spec => !spawns.Any(point => point.SpawnPointId.StartsWith($"cave_{spec.Id}_", StringComparison.Ordinal))))
            {
                Error($"{spec.Korean}이(가) 동굴에 한 마리도 없습니다.");
            }
        }

        report.AppendLine($"몬스터 {Monsters.Length}종 (보스 1) · 전리품 · 수정 장비 {Items.Length}개 · 제작법 {Crafts.Length}개");
        report.AppendLine($"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }
}
