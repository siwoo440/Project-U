using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 106일차: 수영 · 잠수
// 1. 플레이어에 수영(PlayerSwimming)을 붙이고 잠수 입력(C) · 외형 · 손 도구 · 물보라 · 물방울을 연결한다
// 2. 핫바 왼쪽에 스태미나, 오른쪽에 숨 막대를 만든다 (줄었을 때 · 헤엄칠 때만 보임)
// 3. 게임 카메라에 물속 화면 효과(푸른 안개 · 먹먹한 소리)를 붙인다
// 4. 바다 경계(ShoreGuard)에 방향별 해안선을 넣어 먼 바다 파도가 섬 쪽으로 밀게 한다
// 5. 난파선 앞바다 · 석호에 해초 · 산호 · 가라앉은 짐과 잠수해서 주울 물건을 놓는다
// 여러 번 실행해도 같은 결과가 나온다 (바닷속 묶음 · 막대를 지우고 다시 만듦).
public static class SwimmingBuilder
{
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string DialogTitle = "Project U 수영 · 잠수";
    private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";
    private const string ParticleMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";
    private const string CanvasName = "=== GameplayCanvas ===";
    private const string SourceBarName = "--- WetnessBarRoot ---";
    private const string AnchorBarName = "--- TemperatureBarRoot ---";
    public const string StaminaBarName = "--- StaminaBarRoot ---";
    public const string BreathBarName = "--- BreathBarRoot ---";
    public static readonly Vector2 StaminaBarPosition = new Vector2(-348f, 60f); // 핫바 왼쪽 (막대 오른쪽 끝 기준)
    public static readonly Vector2 BreathBarPosition = new Vector2(376f, 60f); // 핫바 오른쪽 (막대 왼쪽 끝 기준, 왼쪽에 아이콘)
    public const string UnderwaterGroupName = "Underwater";
    public const string DiveIdPrefix = "island_dive_";
    private const float WaveStart = 70f; // 해안선 밖 파도 시작 (잠수 물건은 모두 이 안쪽)
    private const float WaveFull = 110f; // 가장 센 파도
    private const int DecorSeed = 106;
    private const float KelpHeight = 2.8f; // 해초 모델 높이 (크기 1)
    private const float CoralHeight = 1.3f; // 산호 모델 높이 (크기 1)

    // 잠수해서 주울 물건 : (아이템, 난파선 해변 기준 좌우 x, 물가에서 바다 쪽 거리, 개수)
    private static readonly (string itemId, float x, float offshore, int quantity)[] DiveLoot =
    {
        ("item_scrap_parts", -4f, 24f, 2),
        ("item_iron_ore", 10f, 30f, 2),
        ("tool_fishing_rod_basic", -22f, 36f, 1),
        ("item_scrap_parts", 18f, 44f, 3),
        ("item_pearl", -12f, 52f, 1),
        ("item_arrow", 30f, 58f, 6),
        ("item_pearl", -35f, 62f, 1),
        ("equipment_small_backpack", 5f, 66f, 1)
    };

    // ---------------------------------------------------------------- 메뉴

    public static string BuildAll(bool saveScene)
    {
        StringBuilder report = new StringBuilder("[수영 · 잠수 만들기]\n");

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요.");
            return report.ToString();
        }

        GameObject island = GameObject.Find(IslandTerrainBuilder.RootName);

        if (island == null)
        {
            report.AppendLine("✗ 무인도가 없습니다. 22번 메뉴를 먼저 실행하세요.");
            return report.ToString();
        }

        report.AppendLine(ConfigurePlayer());
        report.AppendLine(BuildHud());
        report.AppendLine(ConfigureCamera());
        report.AppendLine(ConfigureWaves(island.transform));
        report.AppendLine(EnsureOceanUnderside(island.transform));
        report.AppendLine(BuildUnderwater(island.transform));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (saveScene)
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            report.AppendLine("게임 Scene 저장 완료");
        }

        report.Append(Validate(out _));
        return report.ToString();
    }

    // ---------------------------------------------------------------- 플레이어

    private static InputActionReference FindActionReference(string actionName)
    {
        return AssetDatabase.LoadAllAssetsAtPath(InputAssetPath).OfType<InputActionReference>().FirstOrDefault(reference => reference.action != null && reference.action.actionMap != null && reference.action.actionMap.name == "Player" && reference.action.name == actionName);
    }

    private static string ConfigurePlayer()
    {
        PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        if (movement == null)
        {
            return "✗ 게임 Scene에 플레이어(PlayerMovement)가 없습니다.";
        }

        Transform player = movement.transform;
        PlayerSwimming swimming = player.GetComponent<PlayerSwimming>();

        if (swimming == null)
        {
            swimming = player.gameObject.AddComponent<PlayerSwimming>();
        }

        InputActionReference dive = FindActionReference("Crouch");
        Transform visual = player.Find("PlayerVisual");
        Transform tools = player.Find("--- ToolHolder ---");
        Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
        ParticleSystem splash = BuildParticles(player, "SwimSplash", particleMaterial, false);
        ParticleSystem bubbles = BuildParticles(player, "SwimBubbles", particleMaterial, true);
        swimming.EditorAssign(dive, visual, tools != null ? tools.gameObject : null, splash, bubbles, IslandTerrainBuilder.SeaLevel);
        EditorUtility.SetDirty(swimming);
        return $"플레이어 수영 : 잠수 입력 {(dive != null ? "C (Crouch)" : "✗ 없음")} · 외형 {(visual != null ? "연결" : "✗ 없음")} · 손 도구 {(tools != null ? "연결" : "✗ 없음")} · 물보라 · 물방울";
    }

    private static ParticleSystem BuildParticles(Transform player, string name, Material material, bool bubbles)
    {
        Transform old = player.Find(name);

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject holder = new GameObject(name);
        holder.transform.SetParent(player, false);
        holder.transform.localPosition = bubbles ? new Vector3(0f, 1.55f, 0.25f) : Vector3.zero;
        ParticleSystem particles = holder.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = bubbles;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = bubbles ? 60 : 120;
        main.startLifetime = bubbles ? new ParticleSystem.MinMaxCurve(1.2f, 2f) : new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
        main.startSpeed = bubbles ? new ParticleSystem.MinMaxCurve(0.2f, 0.5f) : new ParticleSystem.MinMaxCurve(1.5f, 3.8f);
        main.startSize = bubbles ? new ParticleSystem.MinMaxCurve(0.04f, 0.11f) : new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
        main.startColor = bubbles ? new Color(0.75f, 0.95f, 1f, 0.7f) : new Color(0.88f, 0.96f, 1f, 0.85f);
        main.gravityModifier = bubbles ? -0.12f : 0.9f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = bubbles ? 7f : 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;

        if (bubbles)
        {
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.45f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // 위쪽으로 튐
        }

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, bubbles ? AnimationCurve.Linear(0f, 0.7f, 1f, 1.2f) : AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    // ---------------------------------------------------------------- 화면 막대

    private static string BuildHud()
    {
        GameObject canvas = GameObject.Find(CanvasName);
        Transform source = canvas != null ? FindDeep(canvas.transform, SourceBarName) : null;
        Transform anchor = canvas != null ? FindDeep(canvas.transform, AnchorBarName) : null;
        PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);

        if (source == null || anchor == null || movement == null)
        {
            return "✗ 화면 생존 막대(젖음 · 체온) 또는 플레이어를 찾지 못했습니다.";
        }

        PlayerSwimming swimming = movement.GetComponent<PlayerSwimming>();
        PlayerStamina stamina = movement.GetComponent<PlayerStamina>();

        if (UISpriteFactory.Icon("Bubble") == null) // 숨 아이콘 (처음 한 번 만듦)
        {
            UISpriteFactory.GenerateSwimIcons();
        }

        // 왼쪽 위 생존 막대 아래는 코인 · 음식 효과 · 의뢰 표시가 있어서, 핫바(가운데 아래 656 × 80) 양옆에 둔다
        RectTransform staminaBar = CloneBar(source, anchor.parent, StaminaBarName, "Stamina", "Bolt", ProjectUUIPalette.Stamina, new Vector2(1f, 0.5f), StaminaBarPosition, anchor.GetSiblingIndex() + 1);
        RectTransform breathBar = CloneBar(source, anchor.parent, BreathBarName, "Breath", "Bubble", ProjectUUIPalette.Breath, new Vector2(0f, 0.5f), BreathBarPosition, staminaBar.GetSiblingIndex() + 1);

        StaminaBarUI staminaUI = staminaBar.gameObject.AddComponent<StaminaBarUI>();
        staminaUI.EditorAssign(stamina, staminaBar.Find("StaminaFill").GetComponent<Image>(), staminaBar.Find("StaminaText").GetComponent<TMP_Text>());
        BreathBarUI breathUI = breathBar.gameObject.AddComponent<BreathBarUI>();
        breathUI.EditorAssign(swimming, breathBar.Find("BreathFill").GetComponent<Image>(), breathBar.Find("BreathText").GetComponent<TMP_Text>());
        staminaBar.GetComponent<CanvasGroup>().alpha = 0f;
        breathBar.GetComponent<CanvasGroup>().alpha = 0f;
        EditorUtility.SetDirty(staminaUI);
        EditorUtility.SetDirty(breathUI);
        return "화면 막대 : 핫바 왼쪽 스태미나 · 오른쪽 숨 (줄었을 때 · 헤엄칠 때만 보임)";
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static RectTransform CloneBar(Transform source, Transform parent, string rootName, string label, string icon, Color color, Vector2 pivot, Vector2 position, int siblingIndex)
    {
        Transform old = parent.Find(rootName);

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject clone = Object.Instantiate(source.gameObject, parent, false);
        clone.name = rootName;

        foreach (MonoBehaviour behaviour in clone.GetComponents<MonoBehaviour>())
        {
            if (behaviour is WetnessBarUI)
            {
                Object.DestroyImmediate(behaviour);
            }
        }

        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            child.name = child.name.Replace("Wetness", label);
            Image image = child.GetComponent<Image>();

            if (image != null && child.name.Contains("Fill"))
            {
                image.color = color;
                image.fillAmount = 1f;
            }

            if (image != null && child.name == "LP_Icon")
            {
                Sprite sprite = UISpriteFactory.Icon(icon);

                if (sprite != null)
                {
                    image.sprite = sprite;
                }

                image.color = color;
            }

            TMP_Text text = child.GetComponent<TMP_Text>();

            if (text != null)
            {
                text.text = $"{label.ToUpperInvariant()} 100 / 100";
            }
        }

        RectTransform rect = (RectTransform)clone.transform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.SetSiblingIndex(Mathf.Min(siblingIndex, parent.childCount - 1));

        if (clone.GetComponent<CanvasGroup>() == null)
        {
            clone.AddComponent<CanvasGroup>();
        }

        CanvasGroup group = clone.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return rect;
    }

    // ---------------------------------------------------------------- 카메라

    private static string ConfigureCamera()
    {
        ThirdPersonCameraFollow follow = Object.FindFirstObjectByType<ThirdPersonCameraFollow>(FindObjectsInactive.Include);
        Camera main = follow != null ? follow.GetComponent<Camera>() : Camera.main;

        if (main == null)
        {
            return "✗ 게임 카메라를 찾지 못했습니다.";
        }

        UnderwaterCameraEffect effect = main.GetComponent<UnderwaterCameraEffect>();

        if (effect == null)
        {
            effect = main.gameObject.AddComponent<UnderwaterCameraEffect>();
        }

        effect.EditorAssign(IslandTerrainBuilder.SeaLevel, follow);
        EditorUtility.SetDirty(effect);
        return "물속 화면 : 푸른 안개 (약 20m) · 푸른 배경 · 먹먹한 소리 · 3인칭 카메라 수면 피하기";
    }

    // ---------------------------------------------------------------- 파도 · 바다

    public static float[] CoastRadii()
    {
        float[] radii = new float[IslandShoreGuard.CoastSamples];

        for (int index = 0; index < radii.Length; index++)
        {
            radii[index] = IslandTerrainBuilder.CoastRadiusAt(index * 360f / radii.Length * Mathf.Deg2Rad);
        }

        return radii;
    }

    public static string ConfigureWaves(Transform island)
    {
        IslandShoreGuard guard = island.GetComponentInChildren<IslandShoreGuard>(true);

        if (guard == null)
        {
            return "✗ 바다 경계(ShoreGuard)가 없습니다. 22번 메뉴를 실행하세요.";
        }

        guard.EditorAssign(IslandTerrainBuilder.SeaLevel, guard.Player, guard.FallbackPoint, CoastRadii());
        SerializedObject serialized = new SerializedObject(guard);
        serialized.FindProperty("waveStart").floatValue = WaveStart;
        serialized.FindProperty("waveFull").floatValue = WaveFull;
        serialized.FindProperty("hardLimit").floatValue = WaveFull + 35f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(guard);
        return $"파도 경계 : 해안선 {IslandShoreGuard.CoastSamples}방향 · 해안선 밖 {WaveStart:0}m부터 밀기 시작 · {WaveFull:0}m에서 가장 셈 ({guard.WavePushSpeed}m/s)";
    }

    public static string EnsureOceanUnderside(Transform island)
    {
        Transform sea = island.Find("Sea");

        if (sea == null)
        {
            return "✗ 바다 묶음이 없습니다. 22번 메뉴를 실행하세요.";
        }

        Transform old = sea.Find("OceanUnderside");

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab("zone_sea", false);

        if (prefab == null)
        {
            return "✗ 바다 모델(zone_sea)을 만들지 못했습니다.";
        }

        GameObject underside = (GameObject)PrefabUtility.InstantiatePrefab(prefab, sea);
        underside.name = "OceanUnderside";
        underside.transform.SetPositionAndRotation(new Vector3(0f, IslandTerrainBuilder.SeaLevel - 0.03f, 0f), Quaternion.Euler(180f, 0f, 0f)); // 물속에서 올려다보는 수면
        underside.transform.localScale = new Vector3(6000f, 1f, 6000f);
        int water = LayerMask.NameToLayer("Water");

        foreach (Transform child in underside.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = water;
        }

        foreach (MeshRenderer renderer in underside.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        GameObjectUtility.SetStaticEditorFlags(underside, 0);
        return "물속에서 본 수면 (뒤집힌 바다 판)";
    }

    // ---------------------------------------------------------------- 바닷속

    private static Vector3 Seabed(float x, float offshore) // 난파선 해변(남쪽) 앞바다의 바닥
    {
        float shore = IslandTerrainBuilder.ShoreRadiusAt(-90f);
        float z = -(shore + offshore);
        return new Vector3(x, IslandTerrainBuilder.HeightAt(x, z), z);
    }

    private static GameObject Place(Transform parent, string modelId, Vector3 position, Quaternion rotation, float scale)
    {
        GameObject prefab = StylizedArtAssetFactory.GetOrCreateModelPrefab(modelId, false);

        if (prefab == null)
        {
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = Vector3.one * scale;
        GameObjectUtility.SetStaticEditorFlags(instance, StaticEditorFlags.BatchingStatic);
        return instance;
    }

    public static string BuildUnderwater(Transform island)
    {
        Transform old = island.Find(UnderwaterGroupName);

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        Transform group = new GameObject(UnderwaterGroupName).transform;
        group.SetParent(island, false);
        System.Random random = new System.Random(DecorSeed);
        List<Vector3> taken = DiveLoot.Select(loot => Seabed(loot.x, loot.offshore)).ToList();
        int kelp = 0;
        int coral = 0;

        bool Free(Vector3 point, float distance)
        {
            return taken.All(other => Vector2.Distance(new Vector2(point.x, point.z), new Vector2(other.x, other.z)) >= distance);
        }

        // 해초 · 산호 (난파선 앞바다, 깊이 1.5 ~ 12m)
        for (int attempt = 0; attempt < 200 && (kelp < 18 || coral < 12); attempt++)
        {
            float x = -75f + (float)random.NextDouble() * 150f;
            float offshore = 16f + (float)random.NextDouble() * 58f;
            Vector3 point = Seabed(x, offshore);

            if (point.y > IslandTerrainBuilder.SeaLevel - 1.5f || !Free(point, 3f))
            {
                continue;
            }

            bool isKelp = kelp < 18 && (coral >= 12 || random.NextDouble() < 0.6);
            float depth = IslandTerrainBuilder.SeaLevel - point.y;
            float scale = isKelp ? Mathf.Min(0.8f + (float)random.NextDouble() * 0.6f, (depth - 0.5f) / KelpHeight) : Mathf.Min(0.8f + (float)random.NextDouble() * 0.7f, (depth - 0.4f) / CoralHeight); // 물 밖으로 나오지 않게
            GameObject placed = Place(group, isKelp ? "zone_kelp" : "zone_coral", point + Vector3.down * 0.05f, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), Mathf.Max(0.3f, scale));

            if (placed == null)
            {
                continue;
            }

            taken.Add(point);
            kelp += isKelp ? 1 : 0;
            coral += isKelp ? 0 : 1;
        }

        // 바닷속 바위 · 가라앉은 짐 (난파선에서 떨어진 상자 · 나무통)
        int props = 0;
        (string model, float x, float offshore, float scale, Vector3 tilt)[] sunken =
        {
            ("rock_large", -28f, 40f, 1.6f, new Vector3(0f, 20f, 0f)),
            ("rock_large", 24f, 54f, 1.9f, new Vector3(0f, 140f, 0f)),
            ("rock_large", -8f, 64f, 1.4f, new Vector3(0f, 260f, 0f)),
            ("prop_crate", 12f, 31f, 1f, new Vector3(18f, 35f, 12f)),
            ("prop_crate", 7f, 67f, 1.1f, new Vector3(-14f, 80f, 22f)),
            ("prop_barrel", 16f, 45f, 1f, new Vector3(80f, 10f, 0f)),
            ("prop_barrel", -38f, 61f, 1f, new Vector3(75f, 120f, 0f))
        };

        foreach ((string model, float x, float offshore, float scale, Vector3 tilt) in sunken)
        {
            GameObject placed = Place(group, model, Seabed(x, offshore) + Vector3.down * 0.25f, Quaternion.Euler(tilt), scale);

            if (placed == null)
            {
                continue;
            }

            MeshFilter filter = placed.GetComponentInChildren<MeshFilter>();

            if (filter != null && filter.sharedMesh != null)
            {
                BoxCollider box = placed.AddComponent<BoxCollider>();
                box.center = filter.sharedMesh.bounds.center;
                box.size = filter.sharedMesh.bounds.size;
            }

            props++;
        }

        // 석호 해초 (부두 옆 물웅덩이)
        Vector2 lagoon = (IslandTerrainBuilder.LagoonMin + IslandTerrainBuilder.LagoonMax) * 0.5f;

        for (int index = 0; index < 5; index++)
        {
            Vector3 point = new Vector3(lagoon.x - 14f + index * 7f, 0f, lagoon.y - 22f + (index % 2) * 30f);
            point.y = IslandTerrainBuilder.HeightAt(point.x, point.z);

            if (point.y < IslandTerrainBuilder.SeaLevel - 1f && Place(group, "zone_kelp", point, Quaternion.Euler(0f, index * 67f, 0f), Mathf.Clamp((IslandTerrainBuilder.SeaLevel - point.y - 0.4f) / KelpHeight, 0.3f, 0.6f)) != null)
            {
                kelp++;
            }
        }

        // 잠수해서 주울 물건 (저장 ID 고정)
        WorldItemPickupRegistry registry = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        Transform lootGroup = new GameObject("DiveLoot").transform;
        lootGroup.SetParent(group, false);
        List<(string itemId, Vector3 position, int quantity)> loot = DiveLoot.Select(item => (item.itemId, Seabed(item.x, item.offshore), item.quantity)).ToList();
        Vector3 lagoonPearl = new Vector3(lagoon.x + 4f, 0f, lagoon.y - 6f);
        lagoonPearl.y = IslandTerrainBuilder.HeightAt(lagoonPearl.x, lagoonPearl.z);
        loot.Add(("item_pearl", lagoonPearl, 1));
        int pickups = 0;
        float deepest = 0f;

        for (int index = 0; index < loot.Count; index++)
        {
            (string itemId, Vector3 position, int quantity) = loot[index];

            if (registry == null || !registry.TryGetPickup(itemId, out WorldItemPickup prefab))
            {
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, lootGroup);
            instance.transform.SetPositionAndRotation(position + Vector3.up * 0.08f, Quaternion.Euler(0f, index * 53f, 0f));
            instance.name = $"Dive_{itemId}_{index}";
            SerializedObject serialized = new SerializedObject(instance.GetComponent<WorldItemPickup>());
            serialized.FindProperty("quantity").intValue = quantity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            WorldObjectIdentity identity = instance.GetComponent<WorldObjectIdentity>();

            if (identity == null)
            {
                identity = instance.AddComponent<WorldObjectIdentity>();
            }

            identity.AssignWorldObjectId($"{DiveIdPrefix}{index:00}_{itemId}");
            EditorUtility.SetDirty(identity);
            deepest = Mathf.Max(deepest, IslandTerrainBuilder.SeaLevel - position.y);
            pickups++;
        }

        return $"바닷속 : 해초 {kelp} · 산호 {coral} · 바위와 가라앉은 짐 {props} · 잠수해서 주울 물건 {pickups}개 (가장 깊은 곳 {deepest:0.0}m)";
    }

    // ---------------------------------------------------------------- 검증

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[수영 · 잠수 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine("Scene 검사 생략 (게임 Scene이 열려 있지 않음)");
            errorCount = 0;
            report.AppendLine("결과 : 오류 0개");
            return report.ToString();
        }

        GameObject island = GameObject.Find(IslandTerrainBuilder.RootName);
        PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        PlayerSwimming swimming = movement != null ? movement.GetComponent<PlayerSwimming>() : null;

        if (island == null)
        {
            report.AppendLine("무인도가 아직 없어 검사를 생략합니다 (22번 → 23번 메뉴).");
            errorCount = 0;
            report.AppendLine("결과 : 오류 0개");
            return report.ToString();
        }

        // 플레이어
        if (swimming == null)
        {
            Error("플레이어에 수영(PlayerSwimming)이 없습니다. 23번 메뉴를 실행하세요.");
        }
        else
        {
            if (swimming.DiveAction == null || swimming.DiveAction.action == null || swimming.DiveAction.action.name != "Crouch")
            {
                Error("잠수 입력이 C(Crouch) 액션에 연결되지 않았습니다.");
            }

            if (swimming.VisualRoot == null || swimming.VisualRoot.name != "PlayerVisual" || swimming.ToolHolder == null)
            {
                Error("수영 자세용 외형(PlayerVisual) 또는 손 도구 묶음이 연결되지 않았습니다.");
            }

            if (swimming.SplashParticles == null || swimming.BubbleParticles == null || swimming.SplashParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial == null)
            {
                Error("물보라 · 물방울 효과가 없거나 재질이 비었습니다.");
            }

            if (!Mathf.Approximately(swimming.SeaLevel, IslandTerrainBuilder.SeaLevel) || swimming.EnterDepth <= swimming.FloatDepth)
            {
                Error("수영 수면 · 깊이 설정이 섬과 맞지 않습니다.");
            }
        }

        // 화면 막대
        StaminaBarUI staminaBar = Object.FindFirstObjectByType<StaminaBarUI>(FindObjectsInactive.Include);
        BreathBarUI breathBar = Object.FindFirstObjectByType<BreathBarUI>(FindObjectsInactive.Include);

        if (staminaBar == null || breathBar == null || staminaBar.name != StaminaBarName || breathBar.name != BreathBarName)
        {
            Error("스태미나 · 숨 막대가 없습니다.");
        }
        else
        {
            foreach (MonoBehaviour bar in new MonoBehaviour[] { staminaBar, breathBar })
            {
                SerializedObject serialized = new SerializedObject(bar);

                if (serialized.FindProperty("fillImage").objectReferenceValue == null || serialized.FindProperty("valueText").objectReferenceValue == null || bar.GetComponent<CanvasGroup>() == null || bar.GetComponent<WetnessBarUI>() != null)
                {
                    Error($"{bar.name}의 채움 · 글자 연결이 비었습니다.");
                }

                RectTransform rect = (RectTransform)bar.transform;
                Image icon = rect.Find("LP_Icon") != null ? rect.Find("LP_Icon").GetComponent<Image>() : null;

                if (icon == null || icon.sprite == null || icon.sprite.name != (bar == (MonoBehaviour)staminaBar ? "ICON_Bolt" : "ICON_Bubble"))
                {
                    Error($"{bar.name}의 아이콘이 비었거나 다릅니다 (스태미나 번개 · 숨 물방울).");
                }

                if (rect.anchorMin != new Vector2(0.5f, 0f) || Vector2.Distance(rect.anchoredPosition, bar == (MonoBehaviour)staminaBar ? StaminaBarPosition : BreathBarPosition) > 1f)
                {
                    Error($"{bar.name}이(가) 핫바 옆 자리에 있지 않습니다. 23번 메뉴를 다시 실행하세요.");
                }
            }
        }

        // 카메라
        ThirdPersonCameraFollow follow = Object.FindFirstObjectByType<ThirdPersonCameraFollow>(FindObjectsInactive.Include);

        if (follow == null || follow.GetComponent<UnderwaterCameraEffect>() == null)
        {
            Error("게임 카메라에 물속 화면 효과(UnderwaterCameraEffect)가 없습니다.");
        }

        // 파도
        IslandShoreGuard guard = island.GetComponentInChildren<IslandShoreGuard>(true);

        if (guard == null || guard.CoastSampleCount != IslandShoreGuard.CoastSamples)
        {
            Error("바다 경계에 방향별 해안선이 없습니다. 23번 메뉴를 실행하세요.");
        }
        else
        {
            float worst = 0f;

            for (int index = 0; index < IslandShoreGuard.CoastSamples; index++)
            {
                float angle = index * 360f / IslandShoreGuard.CoastSamples * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float expected = IslandTerrainBuilder.CoastRadiusAt(angle);
                worst = Mathf.Max(worst, Mathf.Abs(guard.CoastRadiusAt(direction * 100f) - expected));
            }

            if (worst > 1f)
            {
                Error($"바다 경계의 해안선이 섬 모양과 {worst:0.0}m 다릅니다. 23번 메뉴를 다시 실행하세요.");
            }

            if (swimming != null && guard.WavePushSpeed <= swimming.FastSwimSpeed)
            {
                Error($"가장 센 파도({guard.WavePushSpeed}m/s)가 빠른 헤엄({swimming.FastSwimSpeed}m/s)보다 약해서 먼 바다로 나갈 수 있습니다.");
            }

            if (guard.HardLimit <= guard.WaveFull || guard.WaveFull <= guard.WaveStart)
            {
                Error("파도 거리 설정 순서가 잘못되었습니다 (시작 < 가장 셈 < 되돌림).");
            }

            if (CoastRadii().Max() + guard.HardLimit > IslandTerrainBuilder.TerrainSize * 0.5f - 10f)
            {
                Error("파도 경계가 Terrain 가장자리 밖에 있습니다.");
            }
        }

        // 바다 · 바닷속
        Transform sea = island.transform.Find("Sea");

        if (sea == null || sea.Find("OceanUnderside") == null)
        {
            Error("물속에서 본 수면(OceanUnderside)이 없습니다.");
        }

        Transform underwater = island.transform.Find(UnderwaterGroupName);
        WorldItemPickup[] loot = underwater != null ? underwater.GetComponentsInChildren<WorldItemPickup>(true) : new WorldItemPickup[0];
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int kelp = underwater != null ? underwater.GetComponentsInChildren<Transform>(true).Count(item => item.name.StartsWith("LP_zone_kelp", StringComparison.Ordinal)) : 0;
        int coral = underwater != null ? underwater.GetComponentsInChildren<Transform>(true).Count(item => item.name.StartsWith("LP_zone_coral", StringComparison.Ordinal)) : 0;

        if (guard != null && loot.Any(pickup => guard.DistanceBeyondCoast(pickup.transform.position) > guard.WaveStart - 3f))
        {
            Error("잠수 물건이 파도가 미는 곳에 있습니다 (파도 시작보다 안쪽에 두세요).");
        }

        if (loot.Length < 8)
        {
            Error($"잠수해서 주울 물건이 {loot.Length}개입니다 (8개 이상 필요).");
        }

        foreach (WorldItemPickup pickup in loot)
        {
            WorldObjectIdentity identity = pickup.GetComponent<WorldObjectIdentity>();

            if (identity == null || !identity.HasValidId || !identity.WorldObjectId.StartsWith(DiveIdPrefix, StringComparison.Ordinal) || !ids.Add(identity.WorldObjectId))
            {
                Error($"{pickup.name}의 저장 ID가 비었거나 겹칩니다.");
            }

            if (pickup.transform.position.y > IslandTerrainBuilder.SeaLevel - 1f)
            {
                Error($"{pickup.name}이(가) 물속이 아닙니다 (y {pickup.transform.position.y:0.0}).");
            }
        }

        if (kelp < 10 || coral < 6)
        {
            Error($"바닷속 해초 {kelp} · 산호 {coral}개가 너무 적습니다.");
        }

        KoreanFontBuilder.Validate(new[] { PlayerSwimming.SwimHintMessage, PlayerSwimming.LowBreathMessage, PlayerSwimming.DrowningMessage, PlayerSwimming.ExhaustedMessage, IslandShoreGuard.BlockedMessage }, Error, new StringBuilder());

        report.AppendLine($"플레이어 수영 {(swimming != null ? "연결" : "없음")} · 막대 {(staminaBar != null ? 1 : 0) + (breathBar != null ? 1 : 0)}/2 · 물속 화면 {(follow != null && follow.GetComponent<UnderwaterCameraEffect>() != null ? "연결" : "없음")}");
        report.AppendLine($"파도 : 해안선 {(guard != null ? guard.CoastSampleCount : 0)}방향 · 시작 {(guard != null ? guard.WaveStart : 0):0}m · 가장 셈 {(guard != null ? guard.WaveFull : 0):0}m");
        report.AppendLine($"바닷속 : 해초 {kelp} · 산호 {coral} · 잠수해서 주울 물건 {loot.Length}개");
        errorCount = errors;
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        return report.ToString();
    }
}
