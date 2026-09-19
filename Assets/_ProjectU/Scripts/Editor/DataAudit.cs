using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 96일차: 전체 데이터 점검 도구
// 1. 아이템 외형 연결 : 아이콘 · 바닥 Prefab · 손에 든 외형 · 저장 목록(ItemDatabase) · 등록 목록(GameDataRegistry)
//    다른 데이터 연결 : 건축물 Prefab · 제작 · 요리 결과와 재료 · 작물 단계 모델 · 가축 모델 · NPC 초상 · 적 전투 데이터 · 전리품 표
// 2. ID 점검 : 규칙 · 종류별 접두사 · 중복 (같은 종류 · 다른 종류) · 등록 목록 누락
// 3. 외형 설정(Visual Profile) : ID 규칙 · 종류와 접두사 · 외형 Prefab · 실제로 쓰는 Prefab이 있는지 · Prefab이 가리키는 설정이 있는지
// 4. 깨진 참조 : 사라진 스크립트 · 사라진 Asset 참조 (데이터 · Prefab · 게임 Scene)
// 5. 쓰이지 않는 데이터 : 아무도 참조하지 않는 데이터 · 전리품 표 · 아이템 아이콘
// 결과를 Docs/Audit/DataAudit.md 표로 남긴다. 오류는 반드시 고칠 것, 경고는 확인할 것.
public static class DataAudit
{
    public const string ReportPath = "Docs/Audit/DataAudit.md";
    private const string DataRoot = "Assets/_ProjectU/Data";
    private const string PrefabRoot = "Assets/_ProjectU/Prefabs";
    private const string ProjectRoot = "Assets/_ProjectU";
    private const string ItemIconFolder = "Assets/_ProjectU/UI/Icons/Items";
    private const string ItemDatabasePath = "Assets/_ProjectU/Data/Databases/ItemDatabase.asset";
    private const string RegistryPath = "Assets/_ProjectU/Data/Registry/GameDataRegistry.asset";
    private const string PickupRegistryPath = "Assets/_ProjectU/Prefabs/Items/Day75/WorldItemPickupRegistry_Day75.asset";
    private const string HeldVisualsPath = "Assets/_ProjectU/Data/Items/HeldItemVisuals.asset";

    public enum Severity { Error, Warning }

    public sealed class Finding
    {
        public string Section;
        public Severity Level;
        public string Target;
        public string Message;
    }

    public sealed class ItemRow { public string Id; public string Name; public string Category; public bool Icon; public bool Pickup; public string Held; public bool Database; public bool Registry; }
    public sealed class IdRow { public string Type; public int Count; public string Prefix; public int Problems; }
    public sealed class ProfileRow { public string Id; public string Asset; public string Category; public string Prefab; public string UsedBy; }

    public sealed class Result
    {
        public readonly List<Finding> Findings = new List<Finding>();
        public readonly List<ItemRow> Items = new List<ItemRow>();
        public readonly List<IdRow> Ids = new List<IdRow>();
        public readonly List<ProfileRow> Profiles = new List<ProfileRow>();
        public int AssetsScanned;
        public int PrefabsScanned;
        public int SceneObjectsScanned;
        public bool SceneLoaded;
        public int Errors => Findings.Count(finding => finding.Level == Severity.Error);
        public int Warnings => Findings.Count(finding => finding.Level == Severity.Warning);

        public void Add(string section, Severity level, string target, string message)
        {
            Findings.Add(new Finding { Section = section, Level = level, Target = target, Message = message });
        }
    }

    // 종류별 ID 접두사 (아이템은 ItemDataValidator가 분류별로 검사)
    private static readonly (Type type, string label, string prefix, Func<Object, string> id)[] IdTypes =
    {
        (typeof(ItemData), "아이템", null, asset => ((ItemData)asset).ItemId),
        (typeof(CraftingRecipeData), "제작법", "recipe_", asset => ((CraftingRecipeData)asset).RecipeId),
        (typeof(CookingRecipeData), "요리법", "cook_", asset => ((CookingRecipeData)asset).RecipeId),
        (typeof(BuildRecipeData), "건축물", "structure_", asset => ((BuildRecipeData)asset).RecipeId),
        (typeof(EnemyCombatData), "적", "enemy_", asset => ((EnemyCombatData)asset).EnemyId),
        (typeof(CropData), "작물", "crop_", asset => ((CropData)asset).CropId),
        (typeof(FishData), "물고기", "fish_", asset => ((FishData)asset).FishId),
        (typeof(AnimalData), "가축", "animal_", asset => ((AnimalData)asset).AnimalId),
        (typeof(NpcCharacterData), "NPC", "char_", asset => ((NpcCharacterData)asset).CharacterId),
        (typeof(NpcShopData), "NPC 상점", "shop_", asset => ((NpcShopData)asset).ShopId),
        (typeof(StorageTypeData), "보관함 종류", "storage_", asset => ((StorageTypeData)asset).StorageTypeId),
        (typeof(ContentVisualProfile), "외형 설정", "visual_", asset => ((ContentVisualProfile)asset).ProfileId)
    };

    // GameDataRegistry 목록 이름 → 종류
    private static readonly (string property, Type type)[] RegistryLists =
    {
        ("items", typeof(ItemData)), ("craftingRecipes", typeof(CraftingRecipeData)), ("buildRecipes", typeof(BuildRecipeData)),
        ("enemies", typeof(EnemyCombatData)), ("crops", typeof(CropData)), ("fish", typeof(FishData)),
        ("visualProfiles", typeof(ContentVisualProfile)), ("npcs", typeof(NpcCharacterData))
    };

    // 외형 설정 분류 → Profile ID 접두사
    private static readonly Dictionary<ContentVisualCategory, string> ProfilePrefixes = new Dictionary<ContentVisualCategory, string>
    {
        { ContentVisualCategory.Item, "visual_item_" }, { ContentVisualCategory.Weapon, "visual_weapon_" }, { ContentVisualCategory.Enemy, "visual_enemy_" },
        { ContentVisualCategory.Buildable, "visual_buildable_" }, { ContentVisualCategory.Resource, "visual_resource_" }
    };

    // 참조가 없어도 되는 데이터 (ID로 찾거나 도구가 직접 읽음)
    private static readonly Type[] LookedUpById = { typeof(CropData), typeof(FishData), typeof(NpcCharacterData), typeof(GameDataRegistry), typeof(ItemDatabase) };

    // ---------------------------------------------------------------- 메뉴

    // ContentIntegrationValidator · 14번 메뉴에서 사용 (오류 수만 센다)
    public static string Validate(out int errorCount)
    {
        Result result = Analyze();
        errorCount = result.Errors;
        return Summary(result);
    }

    public static string Summary(Result result)
    {
        StringBuilder report = new StringBuilder("[데이터 점검]\n");
        report.AppendLine($"데이터 {result.AssetsScanned}개 · Prefab {result.PrefabsScanned}개 · Scene 오브젝트 {(result.SceneLoaded ? result.SceneObjectsScanned.ToString() : "건너뜀")}");
        report.AppendLine($"아이템 {result.Items.Count}종 : 아이콘 {result.Items.Count(row => row.Icon)} · 바닥 Prefab {result.Items.Count(row => row.Pickup)} · 손 외형 {result.Items.Count(row => row.Held != "없음")} · 저장 목록 {result.Items.Count(row => row.Database)} · 등록 목록 {result.Items.Count(row => row.Registry)}");
        report.AppendLine($"ID : {string.Join(" · ", result.Ids.Select(row => $"{row.Type} {row.Count}"))}");
        report.AppendLine($"외형 설정 {result.Profiles.Count}개 : 외형 Prefab 있음 {result.Profiles.Count(row => !row.Prefab.StartsWith("없음", StringComparison.Ordinal))} · 쓰는 곳 있음 {result.Profiles.Count(row => row.UsedBy != "없음")}");

        foreach (Finding finding in result.Findings.Where(finding => finding.Level == Severity.Warning))
        {
            report.AppendLine($"△ [{finding.Section}] {finding.Target} : {finding.Message}");
        }

        foreach (Finding finding in result.Findings.Where(finding => finding.Level == Severity.Error))
        {
            report.AppendLine($"✗ [{finding.Section}] {finding.Target} : {finding.Message}");
        }

        report.AppendLine(result.Errors == 0 ? $"결과 : 오류 0개 (경고 {result.Warnings}개)" : $"결과 : 오류 {result.Errors}개 (경고 {result.Warnings}개)");
        return report.ToString();
    }

    // ---------------------------------------------------------------- 점검

    public static Result Analyze()
    {
        Result result = new Result();
        Dictionary<Type, List<Object>> assets = LoadDataAssets(result);
        CheckItems(result, assets);
        CheckLinks(result, assets);
        CheckIds(result, assets);
        CheckProfiles(result, assets);
        CheckBrokenReferences(result);
        CheckUnused(result, assets);
        result.Findings.Sort((left, right) => left.Level != right.Level ? left.Level.CompareTo(right.Level) : string.CompareOrdinal(left.Section + left.Target, right.Section + right.Target));
        return result;
    }

    private static Dictionary<Type, List<Object>> LoadDataAssets(Result result)
    {
        Dictionary<Type, List<Object>> assets = new Dictionary<Type, List<Object>>();

        foreach (var (type, _, _, _) in IdTypes)
        {
            List<Object> list = new List<Object>();

            foreach (string guid in AssetDatabase.FindAssets($"t:{type.Name}", new[] { ProjectRoot }))
            {
                Object asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);

                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            list.Sort((left, right) => string.CompareOrdinal(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right)));
            assets[type] = list;
            result.AssetsScanned += list.Count;
        }

        return assets;
    }

    private static string PathOf(Object asset) => asset != null ? AssetDatabase.GetAssetPath(asset) : "-";

    // 1. 아이템 외형 연결
    private static void CheckItems(Result result, Dictionary<Type, List<Object>> assets)
    {
        const string section = "아이템";
        ItemDatabase database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);
        WorldItemPickupRegistry pickups = AssetDatabase.LoadAssetAtPath<WorldItemPickupRegistry>(PickupRegistryPath);
        HeldItemVisualSet held = AssetDatabase.LoadAssetAtPath<HeldItemVisualSet>(HeldVisualsPath);
        HashSet<Object> registered = ReadRegistryList(registry, "items");

        if (database == null) result.Add(section, Severity.Error, ItemDatabasePath, "ItemDatabase가 없습니다.");
        if (pickups == null) result.Add(section, Severity.Error, PickupRegistryPath, "바닥 Prefab 목록이 없습니다.");
        if (held == null) result.Add(section, Severity.Error, HeldVisualsPath, "손에 든 외형 목록이 없습니다.");

        foreach (ItemData item in assets[typeof(ItemData)].Cast<ItemData>().OrderBy(item => item.ItemId, StringComparer.Ordinal))
        {
            string id = string.IsNullOrEmpty(item.ItemId) ? item.name : item.ItemId;
            ItemRow row = new ItemRow { Id = id, Name = item.DisplayName, Category = item.ItemCategory.ToString() };
            row.Icon = item.Icon != null;
            row.Database = database != null && database.TryGetItem(item.ItemId, out ItemData stored) && stored == item;
            row.Registry = registered.Contains(item);
            WorldItemPickup pickup = null;
            row.Pickup = pickups != null && pickups.TryGetPickup(item, out pickup) && pickup != null;
            row.Held = held != null && held.TryGet(item, out _) ? "목록" : item.ToolType != ToolType.None ? "도구 전용" : item.WeaponAttackType == WeaponAttackType.Ranged ? "활 전용" : "없음";
            result.Items.Add(row);

            if (!row.Icon)
            {
                result.Add(section, Severity.Error, id, "아이콘이 없어 가방에서 빈칸으로 보입니다.");
            }
            else if (AssetDatabase.GetAssetPath(item.Icon) != $"{ItemIconFolder}/ICON_{item.ItemId}.png")
            {
                result.Add(section, Severity.Warning, id, $"아이콘 파일 이름이 ID와 다릅니다. ({AssetDatabase.GetAssetPath(item.Icon)})");
            }

            if (!row.Pickup)
            {
                result.Add(section, Severity.Error, id, "바닥 Prefab이 없어 떨어뜨리면 사라집니다.");
            }
            else if (pickup.ItemData != item)
            {
                result.Add(section, Severity.Error, id, $"바닥 Prefab({pickup.name})이 다른 아이템({(pickup.ItemData != null ? pickup.ItemData.ItemId : "없음")})으로 설정되어 있습니다.");
            }

            if (row.Held == "없음")
            {
                result.Add(section, Severity.Error, id, "손에 들었을 때 외형이 없습니다.");
            }

            if (!row.Database)
            {
                result.Add(section, Severity.Error, id, "ItemDatabase에 없어 저장 파일에서 복원할 수 없습니다.");
            }

            if (!row.Registry)
            {
                result.Add(section, Severity.Error, id, "GameDataRegistry에 없습니다.");
            }
        }

        if (pickups != null)
        {
            foreach (WorldItemPickupRegistry.PickupEntry entry in pickups.Entries)
            {
                if (entry?.ItemData != null && !assets[typeof(ItemData)].Contains(entry.ItemData))
                {
                    result.Add(section, Severity.Warning, entry.ItemData.ItemId, "바닥 Prefab 목록에 있지만 게임 데이터 폴더 밖의 아이템입니다.");
                }
            }
        }
    }

    // 1-2. 다른 데이터의 연결 : 건축물 Prefab · 제작 · 요리 결과와 재료 · 작물 단계 모델 · 가축 모델 · NPC 초상 · 적 데이터
    private static void CheckLinks(Result result, Dictionary<Type, List<Object>> assets)
    {
        const string section = "연결";

        void CheckIngredients(string owner, IReadOnlyList<CraftingIngredient> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
            {
                result.Add(section, Severity.Error, owner, "재료가 없습니다.");
                return;
            }

            foreach (CraftingIngredient ingredient in ingredients)
            {
                if (ingredient?.ItemData == null)
                {
                    result.Add(section, Severity.Error, owner, "비어 있는 재료 칸이 있습니다.");
                }
            }
        }

        foreach (BuildRecipeData recipe in assets[typeof(BuildRecipeData)].Cast<BuildRecipeData>())
        {
            if (recipe.PlacedPrefab == null) result.Add(section, Severity.Error, recipe.RecipeId, "설치된 건축물 Prefab이 없습니다.");
            if (recipe.PreviewPrefab == null) result.Add(section, Severity.Error, recipe.RecipeId, "설치 미리보기 Prefab이 없습니다.");

            if (recipe.RequiredTool == ToolType.None || recipe.Ingredients.Count > 0) // 밭처럼 도구로 만드는 건축물은 재료가 없어도 된다
            {
                CheckIngredients(recipe.RecipeId, recipe.Ingredients);
            }
        }

        foreach (CraftingRecipeData recipe in assets[typeof(CraftingRecipeData)].Cast<CraftingRecipeData>())
        {
            if (recipe.ResultItem == null) result.Add(section, Severity.Error, recipe.RecipeId, "제작 결과 아이템이 없습니다.");
            CheckIngredients(recipe.RecipeId, recipe.Ingredients);
        }

        foreach (CookingRecipeData recipe in assets[typeof(CookingRecipeData)].Cast<CookingRecipeData>())
        {
            if (recipe.ResultItem == null) result.Add(section, Severity.Error, recipe.RecipeId, "요리 결과 아이템이 없습니다.");
            CheckIngredients(recipe.RecipeId, recipe.Ingredients);
        }

        foreach (CropData crop in assets[typeof(CropData)].Cast<CropData>())
        {
            for (int index = 0; index < crop.GrowthStages.Count; index++)
            {
                if (crop.GrowthStages[index] == null || crop.GrowthStages[index].VisualPrefab == null)
                {
                    result.Add(section, Severity.Error, crop.CropId, $"자라는 단계 {index + 1}의 모델이 없습니다.");
                }
            }
        }

        foreach (AnimalData animal in assets[typeof(AnimalData)].Cast<AnimalData>())
        {
            if (animal.ModelPrefab == null) result.Add(section, Severity.Error, animal.AnimalId, "가축 모델이 없습니다.");
            if (animal.Icon == null) result.Add(section, Severity.Error, animal.AnimalId, "가축 아이콘이 없습니다.");
            if (animal.FeedItem == null || animal.ProductItem == null) result.Add(section, Severity.Error, animal.AnimalId, "먹이 또는 생산물 아이템이 없습니다.");
        }

        foreach (NpcCharacterData character in assets[typeof(NpcCharacterData)].Cast<NpcCharacterData>())
        {
            if (character.IsPlaced && character.Portrait == null)
            {
                result.Add(section, Severity.Error, character.CharacterId, "마을에 나오는 NPC인데 초상이 없습니다.");
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EnemyHealth health = prefab != null ? prefab.GetComponentInChildren<EnemyHealth>(true) : null;

            if (health == null)
            {
                continue;
            }

            if (!(new SerializedObject(health).FindProperty("combatData").objectReferenceValue is EnemyCombatData))
            {
                result.Add(section, Severity.Error, prefab.name, "적 전투 데이터가 없습니다.");
            }

            EnemyLootDropper dropper = prefab.GetComponentInChildren<EnemyLootDropper>(true);

            if (dropper == null || new SerializedObject(dropper).FindProperty("lootTable").objectReferenceValue == null)
            {
                result.Add(section, Severity.Warning, prefab.name, "적 전리품 표가 없어 아무것도 떨어뜨리지 않습니다.");
            }
        }
    }

    // 2. ID 점검
    private static void CheckIds(Result result, Dictionary<Type, List<Object>> assets)
    {
        const string section = "ID";
        Dictionary<string, (string label, Object asset)> everyId = new Dictionary<string, (string, Object)>(StringComparer.Ordinal);
        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>(RegistryPath);

        foreach ((Type type, string label, string prefix, Func<Object, string> getId) in IdTypes)
        {
            IdRow row = new IdRow { Type = label, Count = assets[type].Count, Prefix = prefix ?? "분류별" };
            Dictionary<string, Object> seen = new Dictionary<string, Object>(StringComparer.Ordinal);

            foreach (Object asset in assets[type])
            {
                string id = getId(asset);

                if (string.IsNullOrWhiteSpace(id))
                {
                    result.Add(section, Severity.Error, PathOf(asset), $"{label} ID가 비어 있습니다.");
                    row.Problems++;
                    continue;
                }

                if (!GameDataRegistry.IsValidContentId(id))
                {
                    result.Add(section, Severity.Error, id, $"{label} ID가 규칙(영어 소문자 · 숫자 · 밑줄)에 맞지 않습니다.");
                    row.Problems++;
                }

                if (prefix != null && !id.StartsWith(prefix, StringComparison.Ordinal))
                {
                    result.Add(section, Severity.Warning, id, $"{label} ID는 '{prefix}'로 시작하는 것이 규칙입니다.");
                    row.Problems++;
                }

                if (seen.TryGetValue(id, out Object other))
                {
                    result.Add(section, Severity.Error, id, $"{label} ID가 겹칩니다. ({PathOf(other)} · {PathOf(asset)})");
                    row.Problems++;
                    continue;
                }

                seen.Add(id, asset);

                if (everyId.TryGetValue(id, out (string label, Object asset) owner))
                {
                    result.Add(section, Severity.Error, id, $"{owner.label}와 {label}가 같은 ID를 씁니다. (저장 · 검색에서 섞임)");
                    row.Problems++;
                }
                else
                {
                    everyId.Add(id, (label, asset));
                }
            }

            result.Ids.Add(row);
        }

        if (registry == null)
        {
            result.Add(section, Severity.Error, RegistryPath, "GameDataRegistry가 없습니다.");
            return;
        }

        foreach ((string property, Type type) in RegistryLists)
        {
            HashSet<Object> registered = ReadRegistryList(registry, property);

            foreach (Object asset in assets[type])
            {
                if (type != typeof(ItemData) && !registered.Contains(asset)) // 아이템은 1번에서 이미 검사
                {
                    result.Add(section, Severity.Error, PathOf(asset), $"GameDataRegistry의 {property} 목록에 없습니다. (14번 메뉴가 등록 목록을 다시 모읍니다)");
                }
            }

            foreach (Object asset in registered)
            {
                if (!assets[type].Contains(asset))
                {
                    result.Add(section, Severity.Warning, PathOf(asset), $"GameDataRegistry의 {property} 목록에 게임 데이터 폴더 밖의 Asset이 있습니다.");
                }
            }
        }
    }

    private static HashSet<Object> ReadRegistryList(GameDataRegistry registry, string property)
    {
        HashSet<Object> result = new HashSet<Object>();

        if (registry == null)
        {
            return result;
        }

        SerializedProperty list = new SerializedObject(registry).FindProperty(property);

        for (int index = 0; list != null && index < list.arraySize; index++)
        {
            Object value = list.GetArrayElementAtIndex(index).objectReferenceValue;

            if (value != null)
            {
                result.Add(value);
            }
        }

        return result;
    }

    // 3. 외형 설정
    private static void CheckProfiles(Result result, Dictionary<Type, List<Object>> assets)
    {
        const string section = "외형 설정";
        List<ContentVisualProfile> profiles = assets[typeof(ContentVisualProfile)].Cast<ContentVisualProfile>().ToList();
        Dictionary<string, ContentVisualProfile> byId = new Dictionary<string, ContentVisualProfile>(StringComparer.Ordinal);
        Dictionary<ContentVisualProfile, List<string>> users = new Dictionary<ContentVisualProfile, List<string>>();
        HashSet<string> contentIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (ContentVisualProfile profile in profiles)
        {
            users[profile] = new List<string>();

            if (!string.IsNullOrEmpty(profile.ProfileId) && !byId.ContainsKey(profile.ProfileId))
            {
                byId.Add(profile.ProfileId, profile);
            }
        }

        foreach (ItemData item in assets[typeof(ItemData)].Cast<ItemData>()) contentIds.Add(item.ItemId);
        foreach (EnemyCombatData enemy in assets[typeof(EnemyCombatData)].Cast<EnemyCombatData>()) contentIds.Add(enemy.EnemyId);
        foreach (BuildRecipeData recipe in assets[typeof(BuildRecipeData)].Cast<BuildRecipeData>()) contentIds.Add(recipe.RecipeId);

        // Prefab · Scene 오브젝트의 외형 연결
        foreach ((string owner, GameObject root) in PrefabsAndScene(result))
        {
            foreach (ContentVisualIdentity identity in root.GetComponentsInChildren<ContentVisualIdentity>(true))
            {
                if (!identity.TryGetVisualProfileId(out string profileId, out string error))
                {
                    result.Add(section, Severity.Error, owner, $"외형 ID를 만들 수 없습니다 : {error}");
                    continue;
                }

                if (!byId.TryGetValue(profileId, out ContentVisualProfile profile))
                {
                    result.Add(section, Severity.Error, owner, $"외형 설정 {profileId} 가 없습니다.");
                    continue;
                }

                AddUser(users, profile, owner);

                if (!identity.UseExplicitVisualProfileId && !contentIds.Contains(identity.ContentId))
                {
                    result.Add(section, Severity.Warning, owner, $"콘텐츠 ID {identity.ContentId} 인 데이터가 없습니다.");
                }
            }

            foreach (EquippedToolView view in root.GetComponentsInChildren<EquippedToolView>(true)) // 97일차: 장착 외형이 쓰는 무기 외형 설정
            {
                SerializedProperty weapons = new SerializedObject(view).FindProperty("weaponProfiles");

                for (int index = 0; weapons != null && index < weapons.arraySize; index++)
                {
                    AddUser(users, weapons.GetArrayElementAtIndex(index).objectReferenceValue as ContentVisualProfile, $"{owner} (장착 외형)");
                }
            }

            foreach (ContentVisualProfileBinder binder in root.GetComponentsInChildren<ContentVisualProfileBinder>(true))
            {
                SerializedObject serialized = new SerializedObject(binder);

                if (serialized.FindProperty("visualProfile")?.objectReferenceValue is ContentVisualProfile direct)
                {
                    AddUser(users, direct, owner);
                }

                SerializedProperty byRegistry = serialized.FindProperty("resolveFromRegistryById");
                string registryId = serialized.FindProperty("visualProfileId")?.stringValue;

                if (byRegistry != null && byRegistry.boolValue && !string.IsNullOrEmpty(registryId))
                {
                    if (byId.TryGetValue(registryId, out ContentVisualProfile profile))
                    {
                        AddUser(users, profile, owner);
                    }
                    else
                    {
                        result.Add(section, Severity.Error, owner, $"외형 설정 {registryId} 가 없습니다.");
                    }
                }
            }
        }

        foreach (ContentVisualProfile profile in profiles.OrderBy(profile => profile.ProfileId, StringComparer.Ordinal))
        {
            string id = string.IsNullOrEmpty(profile.ProfileId) ? profile.name : profile.ProfileId;
            List<string> usedBy = users[profile].Distinct().OrderBy(name => name, StringComparer.Ordinal).ToList();
            result.Profiles.Add(new ProfileRow
            {
                Id = id, Asset = profile.name, Category = profile.Category.ToString(),
                Prefab = profile.VisualPrefab != null ? profile.VisualPrefab.name : profile.CreatePlaceholderWhenPrefabMissing ? "없음 (임시 모양)" : "없음",
                UsedBy = usedBy.Count > 0 ? string.Join(", ", usedBy) : "없음"
            });

            if (ProfilePrefixes.TryGetValue(profile.Category, out string prefix) && !id.StartsWith(prefix, StringComparison.Ordinal))
            {
                result.Add(section, Severity.Error, id, $"{profile.Category} 외형 설정 ID는 '{prefix}'로 시작해야 찾을 수 있습니다.");
            }

            if (profile.VisualPrefab == null)
            {
                result.Add(section, profile.CreatePlaceholderWhenPrefabMissing ? Severity.Warning : Severity.Error, id, profile.CreatePlaceholderWhenPrefabMissing ? "외형 Prefab이 없어 임시 모양으로 보입니다." : "외형 Prefab도 임시 모양도 없어 아무것도 보이지 않습니다.");
            }

            if (usedBy.Count == 0)
            {
                result.Add(section, Severity.Warning, id, "이 외형 설정을 쓰는 Prefab이 없습니다.");
            }
        }
    }

    private static void AddUser(Dictionary<ContentVisualProfile, List<string>> users, ContentVisualProfile profile, string owner)
    {
        if (profile != null && users.TryGetValue(profile, out List<string> list))
        {
            list.Add(owner);
        }
    }

    // Prefab 폴더의 모든 Prefab + 열려 있는 게임 Scene의 최상위 오브젝트
    private static IEnumerable<(string owner, GameObject root)> PrefabsAndScene(Result result)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                yield return (Path.GetFileNameWithoutExtension(path), prefab);
            }
        }

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.IsValid() && scene.isLoaded && scene.path.StartsWith(ProjectRoot, StringComparison.Ordinal))
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                yield return ($"Scene/{root.name}", root);
            }
        }
    }

    // 4. 깨진 참조 : 사라진 스크립트 · 가리키던 Asset이 지워진 참조
    private static void CheckBrokenReferences(Result result)
    {
        const string section = "깨진 참조";

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot, PrefabRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset == null)
                {
                    result.Add(section, Severity.Error, path, "스크립트가 사라진 데이터입니다.");
                    continue;
                }

                if (asset is ScriptableObject)
                {
                    CheckObject(result, section, path, asset);
                }
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                continue;
            }

            result.PrefabsScanned++;
            CheckHierarchy(result, section, Path.GetFileNameWithoutExtension(path), prefab);
        }

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        result.SceneLoaded = scene.IsValid() && scene.isLoaded && Object.FindFirstObjectByType<GameplaySaveController>(FindObjectsInactive.Include) != null;

        if (result.SceneLoaded)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                result.SceneObjectsScanned += root.GetComponentsInChildren<Transform>(true).Length;
                CheckHierarchy(result, section, $"Scene/{root.name}", root);
            }
        }
    }

    private static void CheckHierarchy(Result result, string section, string owner, GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string where = child == root.transform ? owner : $"{owner}/{child.name}";

            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
            {
                result.Add(section, Severity.Error, where, "스크립트가 사라진 컴포넌트가 있습니다.");
            }

            foreach (Component component in child.GetComponents<Component>())
            {
                if (component != null && !(component is Transform))
                {
                    CheckObject(result, section, where, component);
                }
            }
        }
    }

    private static void CheckObject(Result result, string section, string where, Object target)
    {
        SerializedProperty property = new SerializedObject(target).GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
            {
                result.Add(section, Severity.Error, where, $"{target.GetType().Name}.{property.propertyPath} 가 지워진 Asset을 가리킵니다.");
            }
        }
    }

    // 5. 쓰이지 않는 데이터
    private static void CheckUnused(Result result, Dictionary<Type, List<Object>> assets)
    {
        const string section = "쓰이지 않음";
        Dictionary<string, List<string>> referencedBy = BuildReverseReferences();

        foreach (var (type, label, _, getId) in IdTypes)
        {
            if (LookedUpById.Contains(type))
            {
                continue;
            }

            foreach (Object asset in assets[type])
            {
                string path = AssetDatabase.GetAssetPath(asset);
                List<string> owners = referencedBy.TryGetValue(path, out List<string> list) ? list.Where(owner => owner != RegistryPath && owner != ItemDatabasePath).ToList() : new List<string>();

                if (owners.Count == 0 && type != typeof(ContentVisualProfile)) // 외형 설정은 3번에서 검사
                {
                    result.Add(section, Severity.Warning, getId(asset), $"{label} 데이터를 쓰는 곳이 없습니다. ({path})");
                }
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:EnemyLootTable", new[] { ProjectRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!referencedBy.TryGetValue(path, out List<string> owners) || !owners.Any(owner => owner.EndsWith(".prefab", StringComparison.Ordinal)))
            {
                result.Add(section, Severity.Warning, Path.GetFileNameWithoutExtension(path), "이 전리품 표를 쓰는 적이 없습니다.");
            }
        }

        HashSet<string> iconPaths = new HashSet<string>(assets[typeof(ItemData)].Cast<ItemData>().Where(item => item.Icon != null).Select(item => AssetDatabase.GetAssetPath(item.Icon)), StringComparer.Ordinal);

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ItemIconFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (!iconPaths.Contains(path))
            {
                result.Add(section, Severity.Warning, Path.GetFileName(path), "어떤 아이템도 쓰지 않는 아이콘입니다.");
            }
        }
    }

    // Asset 경로 → 그 Asset을 직접 참조하는 Asset 경로들 (Scene · Prefab · 데이터 · 애니메이터)
    private static Dictionary<string, List<string>> BuildReverseReferences()
    {
        Dictionary<string, List<string>> referencedBy = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || AssetDatabase.IsValidFolder(path)
                || !(path.EndsWith(".unity", StringComparison.Ordinal) || path.EndsWith(".prefab", StringComparison.Ordinal) || path.EndsWith(".asset", StringComparison.Ordinal) || path.EndsWith(".controller", StringComparison.Ordinal)))
            {
                continue;
            }

            foreach (string dependency in AssetDatabase.GetDependencies(path, false))
            {
                if (dependency == path)
                {
                    continue;
                }

                if (!referencedBy.TryGetValue(dependency, out List<string> owners))
                {
                    owners = new List<string>();
                    referencedBy.Add(dependency, owners);
                }

                owners.Add(path);
            }
        }

        return referencedBy;
    }

    // ---------------------------------------------------------------- 표 (Markdown)

    public static string WriteMarkdown(Result result)
    {
        StringBuilder md = new StringBuilder();
        string Mark(bool value) => value ? "O" : "X";

        md.AppendLine("# Project U 데이터 점검");
        md.AppendLine();
        md.AppendLine("`Tools > Project U > Data Audit` 또는 `Build Content > 14. Data Audit Fixes`가 만든 표입니다. 데이터를 바꾸면 다시 실행하세요.");
        md.AppendLine();
        md.AppendLine($"- 점검 : 데이터 {result.AssetsScanned}개 · Prefab {result.PrefabsScanned}개 · Scene 오브젝트 {(result.SceneLoaded ? result.SceneObjectsScanned.ToString() : "건너뜀 (게임 Scene이 열려 있지 않음)")}");
        md.AppendLine($"- 결과 : 오류 {result.Errors}개 · 경고 {result.Warnings}개");
        md.AppendLine();

        md.AppendLine("## 1. 문제 목록");
        md.AppendLine();

        if (result.Findings.Count == 0)
        {
            md.AppendLine("문제가 없습니다.");
        }
        else
        {
            md.AppendLine("| 구분 | 영역 | 대상 | 내용 |");
            md.AppendLine("| --- | --- | --- | --- |");

            foreach (Finding finding in result.Findings)
            {
                md.AppendLine($"| {(finding.Level == Severity.Error ? "오류" : "경고")} | {finding.Section} | {finding.Target} | {finding.Message} |");
            }
        }

        md.AppendLine();
        md.AppendLine("## 2. ID");
        md.AppendLine();
        md.AppendLine("| 종류 | 개수 | 접두사 | 문제 |");
        md.AppendLine("| --- | --- | --- | --- |");

        foreach (IdRow row in result.Ids)
        {
            md.AppendLine($"| {row.Type} | {row.Count} | {row.Prefix} | {row.Problems} |");
        }

        md.AppendLine();
        md.AppendLine("## 3. 외형 설정 (Visual Profile)");
        md.AppendLine();
        md.AppendLine("| Profile ID | Asset | 분류 | 외형 Prefab | 쓰는 곳 |");
        md.AppendLine("| --- | --- | --- | --- | --- |");

        foreach (ProfileRow row in result.Profiles)
        {
            md.AppendLine($"| {row.Id} | {row.Asset} | {row.Category} | {row.Prefab} | {row.UsedBy} |");
        }

        md.AppendLine();
        md.AppendLine("## 4. 아이템 외형 연결");
        md.AppendLine();
        md.AppendLine("| 아이템 ID | 이름 | 분류 | 아이콘 | 바닥 Prefab | 손 외형 | 저장 목록 | 등록 목록 |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");

        foreach (ItemRow row in result.Items)
        {
            md.AppendLine($"| {row.Id} | {row.Name} | {row.Category} | {Mark(row.Icon)} | {Mark(row.Pickup)} | {row.Held} | {Mark(row.Database)} | {Mark(row.Registry)} |");
        }

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, md.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
        return ReportPath;
    }
}
