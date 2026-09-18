using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// 90일차: NPC 마을 배치 도구
// 1. NPC 데이터(7번 메뉴)를 먼저 갱신하고, 알파 NPC 7명의 저폴리 모델 · Prefab을 만든다
// 2. 게임 Scene(20_Gameplay)의 캠프 북쪽에 마을(카페 · 대장간 · 게시판 · 짐마차)과 NPC 집 4채를 짓는다
// 3. 일정 위치 17곳 · NPC 7명 · NPC 관리자를 배치한다
// 여러 번 실행해도 같은 Asset·오브젝트를 갱신한다. 실행 후 Ctrl+S로 Scene을 저장해야 반영된다.
public static class NpcPlacementBuilder
{
    public const string BuildMenuRoot = "Tools/Project U/Build Content/";
    private const string DialogTitle = "Project U NPC 마을 배치";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    public const string PrefabFolder = "Assets/_ProjectU/Prefabs/Npc";
    public const string VillageRootName = "=== NPC Village ===";
    private const string LocationsName = "Locations";
    private const string BuildingsName = "Buildings";
    private const string NpcsName = "NPCs";
    private const string EnvironmentRootName = "=== Stylized Environment ===";
    private const string InteractableLayer = "Interactable";
    private const string BuildingLayer = "Building";

    // 알파 NPC 머리 색 (캐릭터 시트 외형 설명 기준)
    private static readonly Dictionary<string, Color> HairColors = new Dictionary<string, Color>
    {
        { "char_lunette", Hex(0xC9CCD1) }, // 은발에 가까운 연회색
        { "char_verona", Hex(0x6B3A26) }, // 밤갈색
        { "char_mio", Hex(0x2B3346) }, // 흑청색
        { "char_mireille", Hex(0xF1E6CC) }, // 크림색 곱슬
        { "char_milky", Hex(0xE3D2BA) }, // 우유빛 백갈색
        { "char_dravia", Hex(0x8A3324) }, // 적갈색
        { "char_lichel", Hex(0xD0CCC6) } // 회백색
    };

    public static Color GetHairColor(string characterId) // 91일차: 초상에도 같은 머리 색 사용
    {
        return HairColors.TryGetValue(characterId ?? string.Empty, out Color hair) ? hair : Hex(0x5A3A26);
    }

    private sealed class BuildingSpec
    {
        public string Name;
        public string ModelId;
        public Vector3 Position;
        public float Yaw;
        public (Vector3 center, Vector3 size)[] Blocks; // 건물 로컬 좌표의 충돌·길막음 상자
        public string Label; // 건물 앞 이름판 (비우면 없음)
        public Vector3 LabelPosition; // 이름판 위치 (건물 로컬)
    }

    private static readonly BuildingSpec[] Buildings =
    {
        new BuildingSpec
        {
            Name = "SunsetCafe", ModelId = "build_npc_cafe", Position = new Vector3(-11f, 0f, 31f), Yaw = 90f, Label = "선셋 카페", LabelPosition = new Vector3(0f, 3.2f, 3.25f),
            Blocks = new[] { (new Vector3(0f, 1.5f, 0f), new Vector3(6.2f, 3f, 5.2f)), (new Vector3(1.9f, 0.5f, 3.6f), new Vector3(1.7f, 1f, 0.65f)) }
        },
        new BuildingSpec
        {
            Name = "Smithy", ModelId = "build_npc_smithy", Position = new Vector3(11f, 0f, 31f), Yaw = 270f, Label = "대장간", LabelPosition = new Vector3(0f, 2.85f, 2.45f),
            Blocks = new[] { (new Vector3(0f, 1.2f, -1.85f), new Vector3(5f, 2.4f, 0.4f)), (new Vector3(-1.4f, 0.6f, -1.1f), new Vector3(1.5f, 1.2f, 1.3f)), (new Vector3(0.75f, 0.35f, 0.2f), new Vector3(0.8f, 0.7f, 0.45f)) }
        },
        new BuildingSpec
        {
            Name = "VillageBoard", ModelId = "prop_village_board", Position = new Vector3(0f, 0f, 33f), Yaw = 180f,
            Blocks = new[] { (new Vector3(0f, 1f, 0f), new Vector3(1.8f, 2f, 0.3f)) }
        },
        new BuildingSpec
        {
            Name = "LichelWagon", ModelId = "prop_npc_wagon", Position = new Vector3(21f, 0f, 24.5f), Yaw = 0f,
            Blocks = new[] { (new Vector3(0f, 1.1f, 0f), new Vector3(2.4f, 2.2f, 1.4f)) }
        },
        HouseSpec("House_Lunette", new Vector3(-28f, 0f, -14f), 90f, "루네트의 오두막"),
        HouseSpec("House_Verona", new Vector3(28f, 0f, -9f), 270f, "베로나의 농가"),
        HouseSpec("House_Mio", new Vector3(34f, 0f, 14f), 180f, "미오의 오두막"),
        HouseSpec("House_Mireille", new Vector3(-28f, 0f, 10f), 90f, "미레유의 집")
    };

    private sealed class LocationSpec
    {
        public string Id;
        public string Building; // 비우면 월드 좌표
        public Vector3 Position; // 건물 로컬 또는 월드 좌표
        public float Yaw; // 건물 기준 또는 월드 방향
        public bool Home;
        public NpcLocationAnchor Anchor;
    }

    private static readonly LocationSpec[] LocationLayout =
    {
        new LocationSpec { Id = "loc_village_square", Position = new Vector3(0f, 0f, 29.5f), Yaw = 0f },
        new LocationSpec { Id = "loc_farm_fields", Position = new Vector3(12.5f, 0f, 6.5f), Yaw = 90f },
        new LocationSpec { Id = "loc_pond_dock", Position = new Vector3(21.8f, 0f, 7.6f), Yaw = 120f },
        new LocationSpec { Id = "loc_animal_pens", Position = new Vector3(-22.5f, 0f, 4f), Yaw = 90f, Anchor = NpcLocationAnchor.AnimalPen },
        new LocationSpec { Id = "loc_market_stall", Position = new Vector3(6f, 0f, 25.5f), Yaw = 180f, Anchor = NpcLocationAnchor.MarketStall },
        new LocationSpec { Id = "loc_forest_edge", Position = new Vector3(-24f, 0f, -23.5f), Yaw = 225f },
        new LocationSpec { Id = "loc_cafe", Building = "SunsetCafe", Position = new Vector3(1.2f, 0f, 5.2f), Yaw = 180f },
        new LocationSpec { Id = "loc_cafe_counter", Building = "SunsetCafe", Position = new Vector3(1.9f, 0f, 2.95f), Yaw = 0f },
        new LocationSpec { Id = "loc_smithy", Building = "Smithy", Position = new Vector3(0.7f, 0f, 2.7f), Yaw = 180f },
        new LocationSpec { Id = "loc_smithy_anvil", Building = "Smithy", Position = new Vector3(0.75f, 0f, -0.45f), Yaw = 0f },
        new LocationSpec { Id = "loc_home_lunette", Building = "House_Lunette", Position = new Vector3(0.6f, 0f, 3.1f), Yaw = 0f, Home = true },
        new LocationSpec { Id = "loc_home_verona", Building = "House_Verona", Position = new Vector3(0.6f, 0f, 3.1f), Yaw = 0f, Home = true },
        new LocationSpec { Id = "loc_home_mio", Building = "House_Mio", Position = new Vector3(0.6f, 0f, 3.1f), Yaw = 0f, Home = true },
        new LocationSpec { Id = "loc_home_mireille", Building = "House_Mireille", Position = new Vector3(0.6f, 0f, 3.1f), Yaw = 0f, Home = true },
        new LocationSpec { Id = "loc_home_milky", Building = "SunsetCafe", Position = new Vector3(0f, 0f, -3.4f), Yaw = 180f, Home = true },
        new LocationSpec { Id = "loc_home_dravia", Building = "Smithy", Position = new Vector3(0f, 0f, -2.9f), Yaw = 180f, Home = true },
        new LocationSpec { Id = "loc_lichel_camp", Building = "LichelWagon", Position = new Vector3(0f, 0f, -1.6f), Yaw = 180f, Home = true }
    };

    private static BuildingSpec HouseSpec(string name, Vector3 position, float yaw, string label)
    {
        return new BuildingSpec
        {
            Name = name, ModelId = "build_npc_house", Position = position, Yaw = yaw, Label = label, LabelPosition = new Vector3(0f, 2.7f, 2.55f),
            Blocks = new[] { (new Vector3(0f, 1.3f, 0f), new Vector3(4.2f, 2.6f, 4.2f)) }
        };
    }

    // ---------------------------------------------------------------- 메뉴

    [MenuItem(BuildMenuRoot + "8. NPC Placement (Village + Models + Manager)", false, 27)]
    private static void BuildAllMenu()
    {
        bool confirmed = EditorUtility.DisplayDialog(
            DialogTitle,
            "NPC 데이터를 갱신하고 알파 NPC 7명의 모델·Prefab을 만든 뒤,\n"
            + "현재 게임 Scene(20_Gameplay)의 캠프 북쪽에 마을 건물 · NPC 집 · 일정 위치 · NPC · NPC 관리자를 배치합니다.\n"
            + "마을 자리의 나무·바위·풀은 숨깁니다.\n\n"
            + "실행 전에 Scene을 저장해 두세요. 실행 후 Ctrl+S로 Scene을 저장해야 반영됩니다.",
            "실행",
            "취소");

        if (!confirmed)
        {
            return;
        }

        string report = BuildAll();
        Debug.Log(report);
        EditorUtility.DisplayDialog(DialogTitle, Shorten(report), "확인");
    }

    private static string Shorten(string report)
    {
        const int limit = 1800;
        return report.Length <= limit ? report : report.Substring(0, limit) + "\n... (전체 내용은 Console 참고)";
    }

    // ---------------------------------------------------------------- 전체 생성

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[NPC 마을 배치]\n");

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "NPC 데이터", 0.05f);
            string dataReport = NpcContentBuilder.BuildAll();
            string dataResult = dataReport.Split('\n').LastOrDefault(line => line.StartsWith("결과")) ?? "결과 없음";
            report.AppendLine($"NPC 데이터 갱신 ({dataResult.Trim()})");

            NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);

            if (database == null)
            {
                report.AppendLine("✗ NpcDatabase가 없습니다.");
                return report.ToString();
            }

            List<NpcCharacterData> cast = database.GetAlphaCast();

            EditorUtility.DisplayProgressBar(DialogTitle, "저폴리 모델", 0.2f);
            StylizedArtAssetFactory.EnsureFolder(PrefabFolder);
            int models = 0;

            foreach (string modelId in cast.Select(character => StylizedModelLibrary.GetNpcModelId(character.CharacterId))
                         .Concat(Buildings.Select(spec => spec.ModelId)).Distinct())
            {
                if (StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, true) != null)
                {
                    models++;
                }
            }

            report.AppendLine($"저폴리 모델 {models}개 생성·갱신 (NPC {cast.Count}명 · 건물 {Buildings.Select(spec => spec.ModelId).Distinct().Count()}종)");

            EditorUtility.DisplayProgressBar(DialogTitle, "NPC Prefab", 0.4f);
            int agentType = FindAgentTypeId();
            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

            foreach (NpcCharacterData character in cast)
            {
                GameObject prefab = CreateOrUpdateNpcPrefab(character, agentType, report);

                if (prefab != null)
                {
                    prefabs[character.CharacterId] = prefab;
                }
            }

            report.AppendLine($"NPC Prefab {prefabs.Count}개 ({PrefabFolder})");
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayProgressBar(DialogTitle, "게임 Scene", 0.7f);
            report.Append(BuildScene(database, cast, prefabs));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    // ---------------------------------------------------------------- NPC Prefab

    private static GameObject CreateOrUpdateNpcPrefab(NpcCharacterData character, int agentType, StringBuilder report)
    {
        string modelId = StylizedModelLibrary.GetNpcModelId(character.CharacterId);
        GameObject modelPrefab = StylizedArtAssetFactory.LoadModelPrefab(modelId);

        if (modelPrefab == null)
        {
            report.AppendLine($"✗ {character.CharacterId} 모델이 없습니다 ({modelId}).");
            return null;
        }

        string path = $"{PrefabFolder}/NPC_{character.EnglishName.Replace(" ", string.Empty)}.prefab";
        GameObject root = new GameObject($"NPC_{character.EnglishName.Replace(" ", string.Empty)}");

        try
        {
            root.layer = LayerMask.NameToLayer(InteractableLayer);

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = agentType;
            agent.radius = 0.3f;
            agent.height = 1.7f;
            agent.speed = 2.2f;
            agent.angularSpeed = 540f;
            agent.acceleration = 10f;
            agent.stoppingDistance = 0.2f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = 40 + Mathf.Abs(character.CharacterId.GetHashCode() % 20);

            CapsuleCollider body = root.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.85f, 0f);
            body.radius = 0.32f;
            body.height = 1.7f;

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            model.name = "Model";

            float height = model.GetComponentInChildren<Renderer>().bounds.max.y;
            TMP_Text nameTag = CreateWorldText(root.transform, "NameTag", height + 0.32f, 2.2f, 3.5f);
            string job = string.IsNullOrEmpty(character.Profile.jobName) ? character.Profile.raceName : character.Profile.jobName.Split('/')[0].Trim();
            nameTag.text = $"<mark=#1C1C20A0 padding=\"12,12,4,4\"><b>{character.DisplayName}</b>\n<size=62%><color=#E8E2D0>{job}</color></size></mark>";
            TMP_Text speech = CreateWorldText(root.transform, "Speech", height + 0.95f, 1.7f, 3.4f);
            speech.text = string.Empty;
            speech.gameObject.SetActive(false);

            NpcAppearance appearance = root.AddComponent<NpcAppearance>();
            appearance.EditorAssignRenderers(model.GetComponentsInChildren<Renderer>(true));
            appearance.SetColors(character.ThemeColor, character.AccentColor, HairColors.TryGetValue(character.CharacterId, out Color hair) ? hair : Hex(0x5A3A26));

            NpcAgent npc = root.AddComponent<NpcAgent>();
            npc.EditorAssign(character, model.transform, nameTag, speech, body);
            root.AddComponent<NpcInteractable>();

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static TMP_Text CreateWorldText(Transform parent, string name, float height, float fontSize, float width)
    {
        GameObject holder = new GameObject(name, typeof(RectTransform));
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = new Vector3(0f, height, 0f);
        TextMeshPro text = holder.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = new Vector2(width, 1f);
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Bottom;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.color = Color.white;
        text.richText = true;
        MeshRenderer renderer = holder.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return text;
    }

    private static int FindAgentTypeId()
    {
        Unity.AI.Navigation.NavMeshSurface surface = Object.FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>();
        return surface != null ? surface.agentTypeID : 0;
    }

    // ---------------------------------------------------------------- 게임 Scene

    private static string BuildScene(NpcDatabase database, List<NpcCharacterData> cast, Dictionary<string, GameObject> prefabs)
    {
        StringBuilder report = new StringBuilder();
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 모델·Prefab만 만들었습니다.");
            return report.ToString();
        }

        DayNightCycle dayNight = Object.FindFirstObjectByType<DayNightCycle>(FindObjectsInactive.Include);
        SeasonCycle season = Object.FindFirstObjectByType<SeasonCycle>(FindObjectsInactive.Include);
        WeatherCycle weather = Object.FindFirstObjectByType<WeatherCycle>(FindObjectsInactive.Include);

        GameObject village = FindRoot(scene, VillageRootName) ?? new GameObject(VillageRootName);
        village.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Transform buildingRoot = GetChild(village.transform, BuildingsName);
        Transform locationRoot = GetChild(village.transform, LocationsName);
        Transform npcRoot = GetChild(village.transform, NpcsName);

        // 건물
        int buildingLayer = LayerMask.NameToLayer(BuildingLayer);
        Dictionary<string, Transform> buildings = new Dictionary<string, Transform>();

        foreach (BuildingSpec spec in Buildings)
        {
            buildings[spec.Name] = CreateOrUpdateBuilding(buildingRoot, spec, buildingLayer);
        }

        int cleared = ClearEnvironment(scene, buildings.Values);
        report.AppendLine($"마을 건물 {Buildings.Length}개 (카페 · 대장간 · 게시판 · 짐마차 · 집 4채), 자리의 나무·바위·풀 {cleared}개 숨김");

        // 일정 위치
        List<NpcLocationPoint> points = new List<NpcLocationPoint>();

        foreach (NpcDatabase.Location location in database.Locations)
        {
            LocationSpec spec = LocationLayout.FirstOrDefault(entry => entry.Id == location.LocationId);

            if (spec == null)
            {
                report.AppendLine($"✗ 위치 배치 정보가 없습니다: {location.LocationId}");
                continue;
            }

            Transform holder = GetChild(locationRoot, location.LocationId);

            if (!string.IsNullOrEmpty(spec.Building) && buildings.TryGetValue(spec.Building, out Transform building))
            {
                holder.SetPositionAndRotation(building.TransformPoint(spec.Position), building.rotation * Quaternion.Euler(0f, spec.Yaw, 0f));
            }
            else
            {
                holder.SetPositionAndRotation(spec.Position, Quaternion.Euler(0f, spec.Yaw, 0f));
            }

            NpcLocationPoint point = GetOrAdd<NpcLocationPoint>(holder.gameObject);
            point.EditorAssign(location.LocationId, location.DisplayName, spec.Home, spec.Anchor);
            EditorUtility.SetDirty(point);
            points.Add(point);
        }

        report.AppendLine($"일정 위치 {points.Count}곳");

        // NPC
        List<NpcAgent> agents = new List<NpcAgent>();

        foreach (NpcCharacterData character in cast)
        {
            if (!prefabs.TryGetValue(character.CharacterId, out GameObject prefab))
            {
                continue;
            }

            Transform existing = npcRoot.Find(prefab.name);

            if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) != prefab)
            {
                Object.DestroyImmediate(existing.gameObject);
                existing = null;
            }

            GameObject instance = existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, npcRoot);
            NpcLocationPoint home = points.FirstOrDefault(point => point.LocationId == character.HomeLocationId);

            if (home != null)
            {
                instance.transform.SetPositionAndRotation(home.transform.position, home.transform.rotation);
            }

            agents.Add(instance.GetComponent<NpcAgent>());
        }

        // 배치에서 빠진 NPC 정리
        foreach (Transform child in npcRoot.Cast<Transform>().ToList())
        {
            if (!agents.Any(agent => agent != null && agent.transform == child))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        NpcManager manager = GetOrAdd<NpcManager>(village);
        manager.EditorAssign(database, dayNight, season, weather, agents, points);
        EditorUtility.SetDirty(manager);
        report.AppendLine($"NPC {agents.Count}명 배치 · NPC 관리자 연결 (낮밤 {(dayNight != null ? "O" : "X")} · 계절 {(season != null ? "O" : "X")} · 날씨 {(weather != null ? "O" : "X")})");

        EditorSceneManager.MarkSceneDirty(scene);
        report.AppendLine("Scene 변경 완료 : Ctrl+S로 저장하세요.");
        return report.ToString();
    }

    private static Transform CreateOrUpdateBuilding(Transform parent, BuildingSpec spec, int layer)
    {
        Transform holder = GetChild(parent, spec.Name);
        holder.SetPositionAndRotation(spec.Position, Quaternion.Euler(0f, spec.Yaw, 0f));
        holder.gameObject.layer = layer;

        // 외형 (모델 Prefab을 다시 넣어 최신 모델 사용)
        Transform oldVisual = holder.Find("Visual");

        if (oldVisual != null)
        {
            Object.DestroyImmediate(oldVisual.gameObject);
        }

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(StylizedArtAssetFactory.LoadModelPrefab(spec.ModelId), holder);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        StylizedVisualReplacer.SetLayerRecursively(visual.transform, layer);

        // 충돌체 · 길막음 (NPC·적이 건물을 뚫고 가지 않게)
        foreach (Transform old in holder.Cast<Transform>().Where(child => child.name.StartsWith("Block_")).ToList())
        {
            Object.DestroyImmediate(old.gameObject);
        }

        for (int index = 0; index < spec.Blocks.Length; index++)
        {
            GameObject block = new GameObject($"Block_{index}");
            block.layer = layer;
            block.transform.SetParent(holder, false);
            block.transform.localPosition = spec.Blocks[index].center;
            BoxCollider box = block.AddComponent<BoxCollider>();
            box.size = spec.Blocks[index].size;
            NavMeshObstacle obstacle = block.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = spec.Blocks[index].size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }

        // 이름판
        Transform oldLabel = holder.Find("Label");

        if (oldLabel != null)
        {
            Object.DestroyImmediate(oldLabel.gameObject);
        }

        if (!string.IsNullOrEmpty(spec.Label))
        {
            TMP_Text label = CreateWorldText(holder, "Label", spec.LabelPosition.y, 3.2f, 6f);
            label.text = $"<mark=#3A2A20C0 padding=\"16,16,6,6\">{spec.Label}</mark>";
            label.alignment = TextAlignmentOptions.Center;
            label.transform.localPosition = spec.LabelPosition;
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // 앞(+Z)에서 읽히게
        }

        return holder;
    }

    // 마을 건물 자리에 있는 나무·바위·풀 숨기기 (건물 둘레 1m 포함)
    private static int ClearEnvironment(UnityEngine.SceneManagement.Scene scene, IEnumerable<Transform> buildings)
    {
        GameObject environment = FindRoot(scene, EnvironmentRootName);

        if (environment == null)
        {
            return 0;
        }

        List<(Vector3 center, Quaternion rotation, Vector3 half)> areas = new List<(Vector3, Quaternion, Vector3)>();

        foreach (Transform building in buildings)
        {
            foreach (BoxCollider box in building.GetComponentsInChildren<BoxCollider>())
            {
                areas.Add((box.transform.TransformPoint(box.center), box.transform.rotation, box.size * 0.5f + new Vector3(1f, 0f, 1f)));
            }
        }

        int hidden = 0;

        foreach (Transform group in environment.transform)
        {
            if (group.name != "Trees" && group.name != "Rocks" && group.name != "Plants")
            {
                continue;
            }

            foreach (Transform item in group)
            {
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }

                foreach ((Vector3 center, Quaternion rotation, Vector3 half) in areas)
                {
                    Vector3 local = Quaternion.Inverse(rotation) * (item.position - center);

                    if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z)
                    {
                        item.gameObject.SetActive(false);
                        hidden++;
                        break;
                    }
                }
            }
        }

        return hidden;
    }

    private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static Transform GetChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);

        if (child == null)
        {
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
        }

        return child;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[NPC 배치 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        NpcDatabase database = AssetDatabase.LoadAssetAtPath<NpcDatabase>(NpcContentBuilder.DatabasePath);
        List<NpcCharacterData> cast = database != null ? database.GetAlphaCast() : new List<NpcCharacterData>();

        foreach (NpcCharacterData character in cast)
        {
            string modelId = StylizedModelLibrary.GetNpcModelId(character.CharacterId);

            if (!StylizedModelLibrary.Catalog.ContainsKey(modelId))
            {
                Error($"{character.CharacterId} : 저폴리 모델 도감에 없습니다 ({modelId}).");
            }
            else if (StylizedArtAssetFactory.LoadModelPrefab(modelId) == null)
            {
                Error($"{character.CharacterId} : 모델 Prefab이 없습니다. 8번 메뉴를 실행하세요.");
            }

            string prefabPath = $"{PrefabFolder}/NPC_{character.EnglishName.Replace(" ", string.Empty)}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null || prefab.GetComponent<NpcAgent>() == null || prefab.GetComponent<NpcAgent>().Character != character)
            {
                Error($"{character.CharacterId} : NPC Prefab이 없거나 캐릭터 연결이 다릅니다 ({prefabPath}).");
            }
        }

        NpcManager manager = Object.FindFirstObjectByType<NpcManager>(FindObjectsInactive.Include);

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
        }
        else if (manager == null)
        {
            Error("게임 Scene에 NPC 관리자가 없습니다. Build Content > 8. NPC Placement를 실행하세요.");
        }
        else
        {
            ValidateScene(manager, database, cast, Error, report);
        }

        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }

    private static void ValidateScene(NpcManager manager, NpcDatabase database, List<NpcCharacterData> cast, System.Action<string> error, StringBuilder report)
    {
        SerializedObject serialized = new SerializedObject(manager);

        foreach (string property in new[] { "database", "dayNightCycle", "seasonCycle", "weatherCycle" })
        {
            if (serialized.FindProperty(property).objectReferenceValue == null)
            {
                error($"NPC 관리자의 {property} 연결이 비어 있습니다.");
            }
        }

        int onNavMesh = 0;
        Physics.SyncTransforms();

        foreach (NpcDatabase.Location location in database.Locations)
        {
            NpcLocationPoint point = manager.Locations.FirstOrDefault(entry => entry != null && entry.LocationId == location.LocationId);

            if (point == null)
            {
                error($"일정 위치가 Scene에 없습니다: {location.LocationId}");
                continue;
            }

            if (!NavMesh.SamplePosition(point.transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                error($"일정 위치가 걸을 수 있는 곳(NavMesh) 밖입니다: {location.LocationId} {point.transform.position}");
                continue;
            }

            if (Physics.CheckSphere(point.transform.position + Vector3.up * 0.9f, 0.25f, LayerMask.GetMask(BuildingLayer), QueryTriggerInteraction.Ignore))
            {
                error($"일정 위치가 건물 안에 있습니다: {location.LocationId}");
                continue;
            }

            onNavMesh++;
        }

        int placed = 0;

        foreach (NpcCharacterData character in cast)
        {
            NpcAgent agent = manager.Agents.FirstOrDefault(entry => entry != null && entry.Character == character);

            if (agent == null)
            {
                error($"{character.CharacterId} : Scene에 배치되지 않았습니다.");
                continue;
            }

            if (agent.gameObject.layer != LayerMask.NameToLayer(InteractableLayer))
            {
                error($"{character.CharacterId} : 상호작용 레이어가 아닙니다.");
            }

            placed++;
        }

        report.AppendLine($"일정 위치 {onNavMesh}/{database.Locations.Count}곳 정상 · NPC {placed}/{cast.Count}명 배치");
    }
}
