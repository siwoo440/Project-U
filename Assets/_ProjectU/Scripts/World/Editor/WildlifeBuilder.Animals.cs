using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// 118일차: 야생동물 데이터 · Prefab · 섬 배치 · 덫 · 무두질대
public static partial class WildlifeBuilder
{
    private const string GruntPrefabPath = "Assets/_ProjectU/Prefabs/Enemies/Day74/Enemy_MeleeGrunt.prefab";
    private const string StoneResourcePath = "Assets/_ProjectU/Prefabs/Gathering/StoneResource_01.prefab";
    private const string CampfirePlacedPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneCampfirePlaced.prefab";
    private const string CampfirePreviewPath = "Assets/_ProjectU/Prefabs/Building/Day73/StoneCampfirePreview.prefab";
    private const string TrapPlacedPath = BuildPrefabFolder + "/AnimalTrapPlaced.prefab";
    private const string TrapPreviewPath = BuildPrefabFolder + "/AnimalTrapPreview.prefab";
    private const string TrapRecipePath = BuildingFolder + "/BuildRecipe_AnimalTrap.asset";
    private const string RackPlacedPath = BuildPrefabFolder + "/TanningRackPlaced.prefab";
    private const string RackPreviewPath = BuildPrefabFolder + "/TanningRackPreview.prefab";
    private const string RackRecipePath = BuildingFolder + "/BuildRecipe_TanningRack.asset";
    private const string WoodPath = "Assets/_ProjectU/Data/Items/ItemData_Wood.asset";
    private const string FiberPath = "Assets/_ProjectU/Data/Items/Day71/ItemData_PlantFiber.asset";
    private const string StonePath = "Assets/_ProjectU/Data/Items/ItemData_Stone.asset";

    // ---------------------------------------------------------------- 동물 표

    public sealed class LootSpec
    {
        public string ItemId; // 얻는 아이템
        public float Chance = 1f; // 나올 확률
        public int Min = 1; // 최소 수량
        public int Max = 1; // 최대 수량
    }

    public sealed class AnimalSpec
    {
        public string Id; // 동물 ID
        public string Korean; // 한글 이름
        public string English; // 표시 이름
        public string Model; // 모델 ID
        public WildAnimalKind Kind = WildAnimalKind.Calm; // 성격
        public WildAnimalActivity Activity = WildAnimalActivity.Always; // 활동 시간
        public SeasonType[] Hidden = new SeasonType[0]; // 숨는 계절
        public float Health = 30f; // 체력 (돌도끼 3 ~ 12번)
        public float Speed = 2.6f; // 이동 속도
        public float Damage; // 공격력 (0이면 사람을 공격하지 않는다)
        public float Detection = 12f; // 알아채는 거리
        public float AttackRange = 1.8f; // 공격 거리
        public float Radius = 32f; // 영역 반경
        public int Count = 2; // 영역 수
        public int PerPoint = 1; // 한 영역에 나오는 수 (늑대는 2)
        public float Scale = 1f; // 모델 크기
        public float Collider = 0.5f; // 충돌체 반지름
        public float Respawn = 200f; // 다시 나오는 시간 (초, 게임 4시간 = 100초 이상)
        public string[] Biomes = new string[0]; // 사는 곳
        public LootSpec[] Loot = new LootSpec[0]; // 전리품
    }

    // 야생동물 13종 중 사냥할 수 있는 10종 (작은 것 3종은 아래 Creatures)
    public static readonly AnimalSpec[] Animals =
    {
        new AnimalSpec
        {
            Id = "rabbit", Korean = "토끼", English = "RABBIT", Model = "animal_rabbit", Activity = WildAnimalActivity.DayOnly,
            Health = 26f, Speed = 3.4f, Radius = 28f, Count = 5, Scale = 1f, Collider = 0.22f, Respawn = 150f,
            Biomes = new[] { "forest", "field" },
            Loot = new[] { new LootSpec { ItemId = "food_small_meat" }, new LootSpec { ItemId = "resource_small_hide", Chance = 0.7f } }
        },
        new AnimalSpec
        {
            Id = "deer", Korean = "사슴", English = "DEER", Model = "animal_deer",
            Health = 48f, Speed = 4.4f, Radius = 46f, Count = 3, Collider = 0.5f, Respawn = 300f,
            Biomes = new[] { "field", "forest" },
            Loot = new[] { new LootSpec { ItemId = "food_red_meat", Min = 2, Max = 3 }, new LootSpec { ItemId = "resource_thick_hide", Min = 1, Max = 2 }, new LootSpec { ItemId = "resource_antler", Chance = 0.3f } }
        },
        new AnimalSpec
        {
            Id = "goat", Korean = "산양", English = "MOUNTAIN GOAT", Model = "animal_goat",
            Health = 40f, Speed = 3.4f, Radius = 34f, Count = 2, Collider = 0.4f, Respawn = 280f,
            Biomes = new[] { "snow" },
            Loot = new[] { new LootSpec { ItemId = "food_red_meat", Min = 1, Max = 2 }, new LootSpec { ItemId = "resource_wool", Min = 1, Max = 2 }, new LootSpec { ItemId = "resource_antler", Chance = 0.25f } }
        },
        new AnimalSpec
        {
            Id = "fox", Korean = "여우", English = "FOX", Model = "animal_fox", Activity = WildAnimalActivity.NightOnly,
            Health = 32f, Speed = 4f, Radius = 38f, Count = 2, Collider = 0.3f, Respawn = 260f,
            Biomes = new[] { "forest", "ruins" },
            Loot = new[] { new LootSpec { ItemId = "food_small_meat" }, new LootSpec { ItemId = "resource_fine_pelt" }, new LootSpec { ItemId = "resource_beast_fang", Chance = 0.25f } }
        },
        new AnimalSpec
        {
            Id = "lizard", Korean = "사막 도마뱀", English = "DESERT LIZARD", Model = "animal_lizard", Activity = WildAnimalActivity.DayOnly, Hidden = new[] { SeasonType.Winter },
            Health = 26f, Speed = 3.2f, Radius = 24f, Count = 2, Scale = 1.2f, Collider = 0.2f, Respawn = 160f,
            Biomes = new[] { "desert" },
            Loot = new[] { new LootSpec { ItemId = "food_small_meat" }, new LootSpec { ItemId = "resource_scale", Min = 1, Max = 2 } }
        },
        new AnimalSpec
        {
            Id = "heron", Korean = "물새", English = "MARSH HERON", Model = "animal_heron", Activity = WildAnimalActivity.DayOnly,
            Health = 28f, Speed = 3f, Radius = 26f, Count = 2, Collider = 0.28f, Respawn = 180f,
            Biomes = new[] { "swamp", "shore" },
            Loot = new[] { new LootSpec { ItemId = "food_small_meat" }, new LootSpec { ItemId = "resource_feather", Min = 2, Max = 3 } }
        },
        new AnimalSpec
        {
            Id = "boar", Korean = "멧돼지", English = "WILD BOAR", Model = "animal_boar", Kind = WildAnimalKind.Fierce,
            Health = 70f, Speed = 3.6f, Damage = 8f, Detection = 12f, AttackRange = 2f, Radius = 40f, Count = 3, Collider = 0.45f, Respawn = 320f,
            Biomes = new[] { "forest", "swamp" },
            Loot = new[] { new LootSpec { ItemId = "food_red_meat", Min = 2, Max = 2 }, new LootSpec { ItemId = "resource_thick_hide" }, new LootSpec { ItemId = "resource_beast_fang", Chance = 0.35f } }
        },
        new AnimalSpec
        {
            Id = "wolf", Korean = "늑대", English = "WOLF", Model = "animal_wolf", Kind = WildAnimalKind.Fierce, Activity = WildAnimalActivity.NightOnly,
            Health = 55f, Speed = 4.2f, Damage = 7f, Detection = 14f, AttackRange = 1.9f, Radius = 42f, Count = 2, PerPoint = 2, Collider = 0.4f, Respawn = 300f,
            Biomes = new[] { "snow", "forest" },
            Loot = new[] { new LootSpec { ItemId = "food_small_meat", Min = 1, Max = 2 }, new LootSpec { ItemId = "resource_fine_pelt" }, new LootSpec { ItemId = "resource_beast_fang", Chance = 0.5f } }
        },
        new AnimalSpec
        {
            Id = "bear", Korean = "큰 곰", English = "GREAT BEAR", Model = "animal_bear", Kind = WildAnimalKind.Fierce, Hidden = new[] { SeasonType.Winter },
            Health = 140f, Speed = 3.2f, Damage = 16f, Detection = 13f, AttackRange = 2.4f, Radius = 50f, Count = 1, Collider = 0.7f, Respawn = 600f,
            Biomes = new[] { "forest" },
            Loot = new[] { new LootSpec { ItemId = "food_bear_meat", Min = 2, Max = 3 }, new LootSpec { ItemId = "resource_fine_pelt", Min = 2, Max = 2 }, new LootSpec { ItemId = "resource_beast_fang" } }
        },
        new AnimalSpec
        {
            Id = "scorpion", Korean = "사막 전갈", English = "DESERT SCORPION", Model = "animal_scorpion", Kind = WildAnimalKind.Fierce, Hidden = new[] { SeasonType.Winter },
            Health = 30f, Speed = 2.8f, Damage = 6f, Detection = 9f, AttackRange = 1.6f, Radius = 24f, Count = 2, Scale = 1.1f, Collider = 0.3f, Respawn = 200f,
            Biomes = new[] { "desert" },
            Loot = new[] { new LootSpec { ItemId = "resource_stinger" }, new LootSpec { ItemId = "resource_scale" } }
        }
    };

    public sealed class CreatureSpec
    {
        public string Id; // 작은 것 ID
        public string Korean; // 한글 이름
        public string Model; // 모델 ID
        public string ItemId; // 잡으면 얻는 아이템
        public int Count = 3; // 놓는 수
        public int Quantity = 1; // 한 곳에서 잡는 수
        public float Respawn = 90f; // 다시 나오는 시간
        public float Scale = 1f; // 모델 크기
        public float Bob = 0.25f; // 흔들리는 높이
        public float Height = 1.1f; // 바닥에서 띄우는 높이
        public WildAnimalActivity Activity = WildAnimalActivity.DayOnly; // 활동 시간
        public SeasonType[] Hidden = new SeasonType[0]; // 숨는 계절
        public string[] Biomes = new string[0]; // 사는 곳
    }

    // 채집망으로 잡는 작은 것 3종
    public static readonly CreatureSpec[] Creatures =
    {
        new CreatureSpec
        {
            Id = "butterfly", Korean = "나비", Model = "creature_butterfly", ItemId = "resource_butterfly", Count = 4, Respawn = 90f, Scale = 1.6f, Bob = 0.4f, Height = 1.2f,
            Activity = WildAnimalActivity.DayOnly, Hidden = new[] { SeasonType.Winter }, Biomes = new[] { "field", "forest" }
        },
        new CreatureSpec
        {
            Id = "firefly", Korean = "반딧불이", Model = "creature_firefly", ItemId = "resource_firefly", Count = 3, Respawn = 90f, Scale = 1.8f, Bob = 0.5f, Height = 1.3f,
            Activity = WildAnimalActivity.NightOnly, Hidden = new[] { SeasonType.Winter }, Biomes = new[] { "forest", "swamp" }
        },
        new CreatureSpec
        {
            Id = "crab", Korean = "게", Model = "creature_crab", ItemId = "food_crab_meat", Count = 3, Quantity = 2, Respawn = 70f, Scale = 1.2f, Bob = 0f, Height = 0.1f,
            Activity = WildAnimalActivity.Always, Biomes = new[] { "shore" }
        }
    };

    public static string AnimalPrefabPath(AnimalSpec spec) => $"{AnimalPrefabFolder}/Animal_{spec.Id}.prefab";

    public static string LootTablePath(AnimalSpec spec) => $"{LootFolder}/WildLootTable_Animal_{spec.Id}.asset";

    public static string CombatDataPath(AnimalSpec spec) => $"{EnemyDataFolder}/EnemyCombatData_Animal_{spec.Id}.asset";

    public static string CreaturePrefabPath(CreatureSpec spec) => $"{CreatureFolder}/Creature_{spec.Id}.prefab";

    // ---------------------------------------------------------------- 동물 데이터 · Prefab

    private static string BuildAnimalAssets()
    {
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        int made = 0;
        int creatures = 0;

        foreach (AnimalSpec spec in Animals)
        {
            CreateCombatData(spec);
            CreateLootTable(spec, registry);

            if (BuildAnimalPrefab(spec))
            {
                made++;
            }
        }

        foreach (CreatureSpec spec in Creatures)
        {
            if (BuildCreaturePrefab(spec))
            {
                creatures++;
            }
        }

        AssetDatabase.SaveAssets();
        return $"동물 Prefab {made}종 (전투 데이터 · 전리품 표 포함) · 작은 것 Prefab {creatures}종";
    }

    private static void CreateCombatData(AnimalSpec spec)
    {
        EnemyCombatData data = LoadOrCreate<EnemyCombatData>(CombatDataPath(spec));
        SerializedObject serialized = new SerializedObject(data);
        serialized.FindProperty("enemyId").stringValue = "animal_" + spec.Id;
        serialized.FindProperty("displayName").stringValue = spec.English;
        serialized.FindProperty("maximumHealth").floatValue = spec.Health;
        serialized.FindProperty("defensePercent").floatValue = 0f;
        serialized.FindProperty("moveSpeed").floatValue = spec.Speed;
        serialized.FindProperty("rotationSpeed").floatValue = 300f;
        serialized.FindProperty("detectionRange").floatValue = spec.Kind == WildAnimalKind.Fierce ? spec.Detection : 0.1f;
        serialized.FindProperty("loseTargetRange").floatValue = spec.Kind == WildAnimalKind.Fierce ? spec.Detection + 6f : 0.2f;
        serialized.FindProperty("attackRange").floatValue = spec.AttackRange;
        serialized.FindProperty("attackDamage").floatValue = spec.Damage;
        serialized.FindProperty("attackCooldown").floatValue = 1.1f;
        serialized.FindProperty("attackWindupDuration").floatValue = 0.5f;
        serialized.FindProperty("attackRecoveryDuration").floatValue = 0.45f;
        serialized.FindProperty("hitReactionDuration").floatValue = 0.2f;
        serialized.FindProperty("deathCleanupDelay").floatValue = 4f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
    }

    private static void CreateLootTable(AnimalSpec spec, WorldItemPickupRegistry registry)
    {
        EnemyLootTable table = LoadOrCreate<EnemyLootTable>(LootTablePath(spec));
        SerializedObject serialized = new SerializedObject(table);
        SerializedProperty entries = serialized.FindProperty("entries");
        entries.arraySize = spec.Loot.Length;

        for (int index = 0; index < spec.Loot.Length; index++)
        {
            LootSpec loot = spec.Loot[index];
            ItemData item = FindItem(loot.ItemId);
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("itemData").objectReferenceValue = item;
            entry.FindPropertyRelative("dropChance").floatValue = loot.Chance;
            entry.FindPropertyRelative("minimumQuantity").intValue = loot.Min;
            entry.FindPropertyRelative("maximumQuantity").intValue = loot.Max;

            if (registry != null && item != null && registry.TryGetPickup(item, out WorldItemPickup pickup))
            {
                entry.FindPropertyRelative("pickupPrefab").objectReferenceValue = pickup;
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
    }

    private static bool BuildAnimalPrefab(AnimalSpec spec)
    {
        string path = AnimalPrefabPath(spec);
        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.Model, true);

        if (model == null || !CopyPrefab(GruntPrefabPath, path))
        {
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            root.name = "Animal_" + spec.Id;

            foreach (ContentVisualRoot visual in root.GetComponentsInChildren<ContentVisualRoot>(true)) // 자동 외형 교체를 끈다
            {
                UnityEngine.Object.DestroyImmediate(visual, true);
            }

            foreach (ContentVisualProfileBinder binder in root.GetComponentsInChildren<ContentVisualProfileBinder>(true))
            {
                UnityEngine.Object.DestroyImmediate(binder, true);
            }

            foreach (ContentVisualDataSourceBinder binder in root.GetComponentsInChildren<ContentVisualDataSourceBinder>(true))
            {
                UnityEngine.Object.DestroyImmediate(binder, true);
            }

            foreach (ContentVisualIdentity identity in root.GetComponentsInChildren<ContentVisualIdentity>(true))
            {
                UnityEngine.Object.DestroyImmediate(identity, true);
            }

            foreach (EnemyNavMeshMovement movement in root.GetComponentsInChildren<EnemyNavMeshMovement>(true)) // 이동은 WildAnimalAgent가 맡는다
            {
                UnityEngine.Object.DestroyImmediate(movement, true);
            }

            if (spec.Kind == WildAnimalKind.Calm) // 순한 동물은 전투 부품을 뺀다
            {
                foreach (Component component in root.GetComponentsInChildren<EnemyCombatFeedback>(true).Cast<Component>()
                    .Concat(root.GetComponentsInChildren<EnemyCombatImpactMotor>(true))
                    .Concat(root.GetComponentsInChildren<EnemyCombatController>(true)))
                {
                    UnityEngine.Object.DestroyImmediate(component, true);
                }
            }

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
                capsule.height = Mathf.Max(spec.Collider * 2.2f, spec.Collider * 4f);
                capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);
            }

            NavMeshAgent agent = root.GetComponentInChildren<NavMeshAgent>(true);

            if (agent != null)
            {
                agent.radius = Mathf.Max(0.2f, spec.Collider);
                agent.height = Mathf.Max(0.5f, spec.Collider * 4f);
                agent.speed = spec.Speed;
                agent.stoppingDistance = 0.4f;
            }

            GameObject mover = agent != null ? agent.gameObject : root; // NavMeshAgent가 있는 곳에 붙인다
            WildAnimalAgent wild = mover.GetComponent<WildAnimalAgent>();

            if (wild == null)
            {
                wild = mover.AddComponent<WildAnimalAgent>();
            }

            wild.EditorAssign(spec.Id, spec.Kind, spec.Activity, spec.Radius, spec.Speed * 0.45f, spec.Speed, spec.Kind == WildAnimalKind.Calm ? 12f : 8f, spec.Hidden);
            ReplaceVisual(root, model, spec.Scale);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return true;
    }

    private static bool BuildCreaturePrefab(CreatureSpec spec)
    {
        string path = CreaturePrefabPath(spec);
        ItemData item = FindItem(spec.ItemId);
        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab(spec.Model, true);

        if (item == null || model == null || !CopyPrefab(StoneResourcePath, path))
        {
            return false;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            root.name = "Creature_" + spec.Id;
            GatherableResource resource = root.GetComponent<GatherableResource>();
            SerializedObject serialized = new SerializedObject(resource);
            serialized.FindProperty("resourceItem").objectReferenceValue = item;
            serialized.FindProperty("promptMessage").stringValue = $"LMB - {spec.Korean} 잡기";
            serialized.FindProperty("totalQuantity").intValue = spec.Quantity;
            serialized.FindProperty("quantityPerInteraction").intValue = spec.Quantity;
            serialized.FindProperty("requiredToolType").intValue = (int)ToolType.Net;
            serialized.FindProperty("requiredToolTier").intValue = 0;
            serialized.FindProperty("respawnEnabled").boolValue = true;
            serialized.FindProperty("respawnDelay").floatValue = spec.Respawn;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SmallCreatureLife life = root.GetComponent<SmallCreatureLife>();

            if (life == null)
            {
                life = root.AddComponent<SmallCreatureLife>();
            }

            life.EditorAssign(spec.Activity, spec.Hidden, spec.Bob, spec.Bob > 0f ? 40f : 8f);
            ReplaceVisual(root, model, spec.Scale);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return true;
    }

    private static void ReplaceVisual(GameObject root, GameObject model, float scale) // 외형을 동물 모델로 바꾼다
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

        foreach (MeshRenderer renderer in visual.GetComponents<MeshRenderer>())
        {
            UnityEngine.Object.DestroyImmediate(renderer, true);
        }

        foreach (MeshFilter filter in visual.GetComponents<MeshFilter>())
        {
            UnityEngine.Object.DestroyImmediate(filter, true);
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * scale;
    }

    private static bool HasVisualProfileParts(GameObject target) // 외형 카드 부품이 붙어 있는지
    {
        return target.GetComponentsInChildren<ContentVisualRoot>(true).Length > 0
            || target.GetComponentsInChildren<ContentVisualProfileBinder>(true).Length > 0
            || target.GetComponentsInChildren<ContentVisualDataSourceBinder>(true).Length > 0
            || target.GetComponentsInChildren<ContentVisualIdentity>(true).Length > 0;
    }

    // ---------------------------------------------------------------- 섬에 놓기

    private static string PlaceWildlife()
    {
        Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            return "게임 Scene이 열려 있지 않아 동물은 놓지 않았습니다.";
        }

        Terrain terrain = Terrain.activeTerrain != null ? Terrain.activeTerrain : UnityEngine.Object.FindFirstObjectByType<Terrain>();

        if (terrain == null)
        {
            return "✗ 섬 지형이 없습니다.";
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
        Transform spawnGroup = new GameObject("AnimalSpawns").transform;
        spawnGroup.SetParent(root.transform, false);
        Transform creatureGroup = new GameObject("SmallCreatures").transform;
        creatureGroup.SetParent(root.transform, false);

        List<Vector3> used = new List<Vector3>();
        List<Vector3> candidates = Candidates(terrain).ToList();
        int animals = 0;
        List<string> counts = new List<string>();

        foreach (AnimalSpec spec in Animals)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalPrefabPath(spec));
            int placed = 0;

            foreach (Vector3 point in candidates)
            {
                if (placed >= spec.Count)
                {
                    break;
                }

                if (prefab == null || !MatchesBiome(spec.Biomes, terrain, point) || TooClose(used, point, 70f) || !OnNavMesh(point))
                {
                    continue;
                }

                for (int index = 0; index < Mathf.Max(1, spec.PerPoint); index++)
                {
                    Vector3 spot = point + new Vector3((index - 0.5f) * 5f, 0f, 0f);
                    spot.y = terrain.SampleHeight(spot) + terrain.transform.position.y;
                    CreateSpawnPoint(spawnGroup, runtimeParent, spec, prefab, spot, placed, index);
                    animals++;
                }

                used.Add(point);
                placed++;
            }

            counts.Add($"{spec.Korean} {placed * Mathf.Max(1, spec.PerPoint)}");
        }

        int creatures = 0;

        foreach (CreatureSpec spec in Creatures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreaturePrefabPath(spec));
            int placed = 0;

            foreach (Vector3 point in candidates)
            {
                if (placed >= spec.Count)
                {
                    break;
                }

                if (prefab == null || !MatchesBiome(spec.Biomes, terrain, point) || TooClose(used, point, 42f))
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, creatureGroup);
                instance.transform.SetPositionAndRotation(point + Vector3.up * spec.Height, Quaternion.Euler(0f, (point.x + point.z) % 360f, 0f));
                instance.name = $"Creature_{spec.Id}_{placed:00}";
                WorldObjectIdentity identity = instance.GetComponent<WorldObjectIdentity>();

                if (identity == null)
                {
                    identity = instance.AddComponent<WorldObjectIdentity>();
                }

                identity.AssignWorldObjectId($"wild_creature_{spec.Id}_{placed:00}");
                EditorUtility.SetDirty(identity);
                GameObjectUtility.SetStaticEditorFlags(instance, 0);
                used.Add(point);
                placed++;
                creatures++;
            }

            counts.Add($"{spec.Korean} {placed}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        return $"섬에 야생동물 {animals}마리 · 작은 것 {creatures}마리 ({string.Join(" · ", counts)})";
    }

    private static void CreateSpawnPoint(Transform parent, Transform runtimeParent, AnimalSpec spec, GameObject prefab, Vector3 spot, int territory, int index)
    {
        GameObject holder = new GameObject($"Spawn_{spec.Id}_{territory:00}_{index}");
        holder.transform.SetParent(parent, false);
        holder.transform.position = spot;
        EnemySpawnPoint point = holder.AddComponent<EnemySpawnPoint>();
        SerializedObject serialized = new SerializedObject(point);
        serialized.FindProperty("spawnPointId").stringValue = $"wild_{spec.Id}_{territory:00}_{index}";
        serialized.FindProperty("enemyPrefab").objectReferenceValue = prefab;
        serialized.FindProperty("runtimeEnemyParent").objectReferenceValue = runtimeParent;
        serialized.FindProperty("spawnOnStart").boolValue = true;
        serialized.FindProperty("initialSpawnDelay").floatValue = 0.5f + index * 0.4f;
        serialized.FindProperty("respawnEnabled").boolValue = true;
        serialized.FindProperty("respawnDelay").floatValue = spec.Respawn;
        serialized.FindProperty("corpseVisibleDuration").floatValue = 3f;
        serialized.FindProperty("spawnRadius").floatValue = Mathf.Min(8f, spec.Radius * 0.25f);
        serialized.FindProperty("navMeshSampleRadius").floatValue = 8f;
        serialized.FindProperty("logSpawnResults").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // 섬 위 후보 자리 (항상 같은 순서로 나온다)
    private static IEnumerable<Vector3> Candidates(Terrain terrain)
    {
        for (int ring = 0; ring < 19; ring++)
        {
            float radius = 110f + ring * 33f;
            int steps = 14 + ring * 3;

            for (int step = 0; step < steps; step++)
            {
                float angle = (step + ring * 0.41f) / steps * Mathf.PI * 2f;
                Vector3 point = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                point.y = terrain.SampleHeight(point) + terrain.transform.position.y;
                yield return point;
            }
        }
    }

    private static bool MatchesBiome(string[] biomes, Terrain terrain, Vector3 point)
    {
        if (biomes == null || biomes.Length == 0)
        {
            return false;
        }

        float height = point.y;
        Vector3 size = terrain.terrainData.size;
        Vector3 local = point - terrain.transform.position;
        float steep = terrain.terrainData.GetSteepness(Mathf.Clamp01(local.x / size.x), Mathf.Clamp01(local.z / size.z));

        if (height < 2.6f || steep > 26f || !IslandNatureBuilder.IsClear(point.x, point.z, 14f)) // 물 · 가파른 곳 · 마을 · 흙길 제외
        {
            return false;
        }

        float forest = IslandNatureBuilder.ForestNoise(point.x, point.z);

        foreach (string biome in biomes)
        {
            switch (biome)
            {
                case "forest":
                    if (forest > 0.5f && height < 30f) return true;
                    break;
                case "field":
                    if (forest < 0.36f && height < 22f) return true;
                    break;
                case "shore":
                    if (height < 6.5f) return true;
                    break;
                case "snow":
                    if (ZoneDistance("snow", point) < 230f) return true;
                    break;
                case "desert":
                    if (ZoneDistance("desert", point) < 210f) return true;
                    break;
                case "swamp":
                    if (ZoneDistance("swamp", point) < 190f) return true;
                    break;
                case "ruins":
                    if (ZoneDistance("ruins", point) < 170f) return true;
                    break;
            }
        }

        return false;
    }

    private static float ZoneDistance(string zoneId, Vector3 point)
    {
        IslandZoneLayout.Site site = IslandZoneLayout.Get(zoneId);

        if (site == null)
        {
            return float.PositiveInfinity;
        }

        Vector3 center = new Vector3(site.Offset.x + site.PadCenter.x, 0f, site.Offset.y + site.PadCenter.y);
        return Vector2.Distance(new Vector2(point.x, point.z), new Vector2(center.x, center.z));
    }

    private static bool TooClose(List<Vector3> used, Vector3 point, float gap)
    {
        return used.Any(item => Vector2.Distance(new Vector2(item.x, item.z), new Vector2(point.x, point.z)) < gap);
    }

    private static bool OnNavMesh(Vector3 point)
    {
        return NavMesh.SamplePosition(point, out NavMeshHit hit, 6f, NavMesh.AllAreas) && Mathf.Abs(hit.position.y - point.y) < 6f;
    }

    // ---------------------------------------------------------------- 덫 · 무두질대

    private static string BuildTrap(Dictionary<string, ItemData> items)
    {
        if (!CopyPrefab(CampfirePlacedPath, TrapPlacedPath) || !CopyPrefab(CampfirePreviewPath, TrapPreviewPath))
        {
            return "✗ 덫 Prefab을 만들지 못했습니다.";
        }

        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab("build_animal_trap", true);
        GameObject placed = PrefabUtility.LoadPrefabContents(TrapPlacedPath);

        try
        {
            placed.name = "AnimalTrapPlaced";

            foreach (Component component in placed.GetComponentsInChildren<CampfireCookingStation>(true).Cast<Component>()
                .Concat(placed.GetComponentsInChildren<Light>(true))
                .Concat(placed.GetComponentsInChildren<ParticleSystem>(true)))
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }

            AnimalTrap trap = placed.GetComponent<AnimalTrap>();

            if (trap == null)
            {
                trap = placed.AddComponent<AnimalTrap>();
            }

            trap.EditorAssign(4f, 1, new List<TrapCatchEntry>
            {
                new TrapCatchEntry { animalName = "토끼", item = items["food_small_meat"], amount = 1, extraItem = items["resource_small_hide"], extraAmount = 1 },
                new TrapCatchEntry { animalName = "여우", item = items["resource_fine_pelt"], amount = 1, extraItem = items["food_small_meat"], extraAmount = 1 },
                new TrapCatchEntry { animalName = "게", item = items["food_crab_meat"], amount = 2, extraItem = items["resource_crab_shell"], extraAmount = 1 },
                new TrapCatchEntry { animalName = "물새", item = items["resource_feather"], amount = 2, extraItem = items["food_small_meat"], extraAmount = 1 }
            });

            SerializedObject serialized = new SerializedObject(trap);
            serialized.FindProperty("promptMessage").stringValue = "덫 : 기다리는 중";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ReplaceVisual(placed, model, 1f);
            PrefabUtility.SaveAsPrefabAsset(placed, TrapPlacedPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(placed);
        }

        GameObject preview = PrefabUtility.LoadPrefabContents(TrapPreviewPath);

        try
        {
            preview.name = "AnimalTrapPreview";
            ReplaceVisual(preview, model, 1f);
            PrefabUtility.SaveAsPrefabAsset(preview, TrapPreviewPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(preview);
        }

        BuildRecipeData recipe = CreateBuildRecipe(TrapRecipePath, "structure_animal_trap", "ANIMAL TRAP", TrapPlacedPath, TrapPreviewPath,
            new Vector3(0.6f, 0.3f, 0.6f), new[] { (WoodPath, 4), (FiberPath, 2) });
        return $"덫 건축물 (나무 4 + 식물 섬유 2) · 하루 1마리 · {ConnectBuildMenu(recipe)}";
    }

    private static string BuildTanningRack(List<CookingRecipeData> cooks)
    {
        if (!CopyPrefab(CampfirePlacedPath, RackPlacedPath) || !CopyPrefab(CampfirePreviewPath, RackPreviewPath))
        {
            return "✗ 무두질대 Prefab을 만들지 못했습니다.";
        }

        GameObject model = StylizedArtAssetFactory.GetOrCreateModelPrefab("build_tanning_rack", true);
        List<CookingRecipeData> tanning = cooks.Where(recipe => recipe != null && recipe.RequiredStation == CookingStationTier.TanningRack).ToList();
        GameObject placed = PrefabUtility.LoadPrefabContents(RackPlacedPath);

        try
        {
            placed.name = "TanningRackPlaced";

            foreach (Component component in placed.GetComponentsInChildren<Light>(true).Cast<Component>().Concat(placed.GetComponentsInChildren<ParticleSystem>(true)))
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }

            CampfireCookingStation station = placed.GetComponentInChildren<CampfireCookingStation>(true);

            if (station == null)
            {
                return "✗ 무두질대에 작업대 부품이 없습니다.";
            }

            SerializedObject serialized = new SerializedObject(station);
            serialized.FindProperty("stationTier").intValue = (int)CookingStationTier.TanningRack;
            serialized.FindProperty("slotCount").intValue = 2;
            serialized.FindProperty("maxBatchQuantity").intValue = 3;
            serialized.FindProperty("fuelItem").objectReferenceValue = FindItem("drink_water_bottle"); // 무두질에 쓰는 물
            serialized.FindProperty("fuelAmount").intValue = 1;
            serialized.FindProperty("promptMessage").stringValue = "F - TAN LEATHER";
            serialized.FindProperty("slots").arraySize = 0;
            SerializedProperty list = serialized.FindProperty("recipes");
            list.arraySize = tanning.Count;

            for (int index = 0; index < tanning.Count; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = tanning[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            ReplaceVisual(placed, model, 1f);
            PrefabUtility.SaveAsPrefabAsset(placed, RackPlacedPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(placed);
        }

        GameObject preview = PrefabUtility.LoadPrefabContents(RackPreviewPath);

        try
        {
            preview.name = "TanningRackPreview";
            ReplaceVisual(preview, model, 1f);
            PrefabUtility.SaveAsPrefabAsset(preview, RackPreviewPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(preview);
        }

        BuildRecipeData recipe = CreateBuildRecipe(RackRecipePath, "structure_tanning_rack", "TANNING RACK", RackPlacedPath, RackPreviewPath,
            new Vector3(0.9f, 0.6f, 0.5f), new[] { (WoodPath, 12), (StonePath, 4) });
        return $"무두질대 건축물 (나무 12 + 돌 4) · 무두질법 {tanning.Count}개 · {ConnectBuildMenu(recipe)}";
    }

    private static BuildRecipeData CreateBuildRecipe(string path, string recipeId, string english, string placedPath, string previewPath, Vector3 halfExtents, (string path, int amount)[] ingredients)
    {
        BuildRecipeData recipe = LoadOrCreate<BuildRecipeData>(path);
        SerializedObject serialized = new SerializedObject(recipe);
        serialized.FindProperty("recipeId").stringValue = recipeId;
        serialized.FindProperty("displayName").stringValue = english;
        serialized.FindProperty("structureType").intValue = 4; // 기능성 가구
        serialized.FindProperty("allowGroundPlacement").boolValue = true;
        serialized.FindProperty("placementType").intValue = 2;
        serialized.FindProperty("rotationStep").floatValue = 45f;
        serialized.FindProperty("placedPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(placedPath);
        serialized.FindProperty("previewPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(previewPath);
        serialized.FindProperty("placementCheckCenter").vector3Value = new Vector3(0f, halfExtents.y, 0f);
        serialized.FindProperty("placementCheckHalfExtents").vector3Value = halfExtents;
        serialized.FindProperty("maximumSlopeAngle").floatValue = 22f;
        serialized.FindProperty("maximumHeightDifference").floatValue = 0.2f;
        serialized.FindProperty("demolitionRefundRatio").floatValue = 0.5f;
        SerializedProperty list = serialized.FindProperty("ingredients");
        list.arraySize = ingredients.Length;

        for (int index = 0; index < ingredients.Length; index++)
        {
            list.GetArrayElementAtIndex(index).FindPropertyRelative("itemData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemData>(ingredients[index].path);
            list.GetArrayElementAtIndex(index).FindPropertyRelative("amount").intValue = ingredients[index].amount;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(recipe);
        AssetDatabase.SaveAssets();
        return recipe;
    }

    private static string ConnectBuildMenu(BuildRecipeData recipe) // 건축 목록 · 건축물 저장 목록에 넣기
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            return "건축 목록 연결은 게임 Scene에서";
        }

        BuildPlacementController controller = UnityEngine.Object.FindFirstObjectByType<BuildPlacementController>(FindObjectsInactive.Include);
        PlacedStructureSaveBridge bridge = UnityEngine.Object.FindFirstObjectByType<PlacedStructureSaveBridge>(FindObjectsInactive.Include);
        int added = 0;

        foreach (UnityEngine.Object target in new UnityEngine.Object[] { controller, bridge })
        {
            if (target == null)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty("buildRecipes");
            bool found = false;

            for (int index = 0; index < list.arraySize; index++)
            {
                found |= list.GetArrayElementAtIndex(index).objectReferenceValue == recipe;
            }

            if (found)
            {
                continue;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return added > 0 ? $"건축 · 저장 목록 {added}곳에 추가" : "건축 · 저장 목록에 이미 있음";
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[야생동물 검증]\n");
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

        foreach (AnimalSpec spec in Animals)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalPrefabPath(spec));
            EnemyHealth health = prefab != null ? prefab.GetComponentInChildren<EnemyHealth>(true) : null;
            WildAnimalAgent wild = prefab != null ? prefab.GetComponentInChildren<WildAnimalAgent>(true) : null;
            EnemyLootTable table = AssetDatabase.LoadAssetAtPath<EnemyLootTable>(LootTablePath(spec));

            if (prefab == null || health == null || wild == null)
            {
                Error($"{spec.Id} 동물 Prefab이 없거나 부품이 빠졌습니다.");
                continue;
            }

            if (health.MaximumHealth != spec.Health || wild.Kind != spec.Kind)
            {
                Error($"{spec.Id} 체력 · 성격이 표와 다릅니다.");
            }

            if (table == null || table.Entries.Count != spec.Loot.Length || table.Entries.Any(entry => entry?.ItemData == null))
            {
                Error($"{spec.Id} 전리품 표가 없거나 아이템이 비어 있습니다.");
            }

            if (spec.Kind == WildAnimalKind.Calm && prefab.GetComponentInChildren<EnemyCombatController>(true) != null)
            {
                Error($"{spec.Id} 순한 동물에 전투 부품이 남아 있습니다.");
            }
        }

        foreach (CreatureSpec spec in Creatures)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreaturePrefabPath(spec));
            GatherableResource resource = prefab != null ? prefab.GetComponent<GatherableResource>() : null;

            if (resource == null || resource.RequiredToolType != ToolType.Net)
            {
                Error($"{spec.Id} 작은 것 Prefab이 없거나 채집망 조건이 아닙니다.");
            }
        }

        foreach (CookSpec spec in Cooks)
        {
            CookingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CookingRecipeData>($"{CookingFolder}/{spec.Asset}.asset");

            if (recipe == null || recipe.ResultItem == null || recipe.RequiredStation != spec.Station)
            {
                Error($"{spec.Id} 요리 · 무두질법이 없거나 시설이 다릅니다.");
            }
        }

        foreach (CraftSpec spec in Crafts)
        {
            CraftingRecipeData recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipeData>($"{CraftingFolder}/{spec.Asset}.asset");

            if (recipe == null || recipe.ResultItem == null)
            {
                Error($"{spec.Id} 제작법이 없습니다.");
            }
        }

        GameObject trap = AssetDatabase.LoadAssetAtPath<GameObject>(TrapPlacedPath);

        if (trap == null || trap.GetComponent<AnimalTrap>() == null)
        {
            Error("덫 Prefab이 없습니다.");
        }

        GameObject rack = AssetDatabase.LoadAssetAtPath<GameObject>(RackPlacedPath);
        CampfireCookingStation rackStation = rack != null ? rack.GetComponentInChildren<CampfireCookingStation>(true) : null;

        if (rackStation == null || rackStation.Tier != CookingStationTier.TanningRack)
        {
            Error("무두질대 Prefab이 없거나 무두질 시설이 아닙니다.");
        }

        if (AssetDatabase.LoadAssetAtPath<BuildRecipeData>(TrapRecipePath) == null || AssetDatabase.LoadAssetAtPath<BuildRecipeData>(RackRecipePath) == null)
        {
            Error("덫 · 무두질대 건축 데이터가 없습니다.");
        }

        if (EditorSceneManager.GetActiveScene().path == ScenePath)
        {
            GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == RootName);
            EnemySpawnPoint[] spawns = root != null ? root.GetComponentsInChildren<EnemySpawnPoint>(true) : new EnemySpawnPoint[0];
            GatherableResource[] creatures = root != null ? root.GetComponentsInChildren<GatherableResource>(true) : new GatherableResource[0];
            int expected = Animals.Sum(spec => spec.Count * Mathf.Max(1, spec.PerPoint));

            if (root == null)
            {
                Error("Scene에 야생동물 묶음이 없습니다.");
            }
            else if (spawns.Length < expected * 0.7f)
            {
                Error($"야생동물 생성 지점이 {spawns.Length}곳입니다 ({expected}곳 목표).");
            }

            if (creatures.Length < Creatures.Sum(spec => spec.Count) * 0.7f)
            {
                Error($"작은 것이 {creatures.Length}마리입니다 ({Creatures.Sum(spec => spec.Count)}마리 목표).");
            }

            foreach (AnimalSpec spec in Animals.Where(spec => !spawns.Any(point => point.SpawnPointId.StartsWith($"wild_{spec.Id}_", StringComparison.Ordinal))))
            {
                Error($"{spec.Korean}이(가) 섬에 한 마리도 없습니다.");
            }
        }

        report.AppendLine($"동물 {Animals.Length}종 · 작은 것 {Creatures.Length}종 · 사냥 물건 {Items.Length}개 · 요리 {Cooks.Length}개 · 제작법 {Crafts.Length}개");
        report.AppendLine($"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }
}
