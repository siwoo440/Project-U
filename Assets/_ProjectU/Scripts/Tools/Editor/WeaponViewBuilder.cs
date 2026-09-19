using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// 97일차: 무기 외형 1인칭 · 3인칭 분리 도구
// 1. 도끼(visual_weapon_axe) · 활(visual_weapon_bow) 외형 설정에 손 외형(3인칭 · 1인칭 위치 · 회전 · 크기)을 넣는다
//    3인칭 값은 지금 몸 옆 도구 거치대(ToolHolder)에 붙은 저폴리 모델 위치를 그대로 읽어 온다
// 2. 게임 Scene의 Main Camera 아래에 1인칭 무기 거치대(FirstPersonToolHolder)를 만들고
//    장착 외형(EquippedToolView) · 휘두르기 연출(ToolSwingAnimation)에 연결한다
// 3. GameDataRegistry 등록 목록을 다시 모으고 검사한다
// 여러 번 실행해도 같은 값이 된다. 실행 후 Ctrl+S로 Scene을 저장해야 반영된다.
public static class WeaponViewBuilder
{
    private const string DialogTitle = "Project U 무기 외형";
    private const string ScenePath = "Assets/_ProjectU/Scenes/20_Gameplay.unity";
    private const string ProfileFolder = "Assets/_ProjectU/Data/VisualProfiles/Weapons/";
    public const string FirstPersonHolderName = "FirstPersonToolHolder";

    public sealed class WeaponSpec
    {
        public string ItemId;
        public string ProfileId;
        public string AssetName;
        public string DisplayName;
        public string ModelId;
        public string ThirdPersonVisual; // ToolHolder 아래 기존 전용 외형 이름 (3인칭 위치를 읽어 옴)
        public Vector3 FirstPersonGrip; // 1인칭 : Camera 기준 손잡이(잡는 곳) 위치
        public float GripHeight; // 모델 바닥에서 잡는 곳까지 높이 (모델 원래 크기 기준)
        public Vector3 FirstPersonTilt; // 1인칭 : 모델을 돌린 뒤 앞(X) · 옆(Z)으로 기울이는 각도
        public float FirstPersonYaw; // 1인칭 : 모델 자체 방향 (날 · 활 배가 앞을 보게)
        public float FirstPersonScale; // 1인칭 크기
    }

    // 1인칭 : 도끼는 오른손 아래에서 앞으로 기울여 들고, 활은 왼손에 조금 기울여 세운다
    public static readonly WeaponSpec[] Specs =
    {
        new WeaponSpec
        {
            ItemId = "tool_axe", ProfileId = "visual_weapon_axe", AssetName = "VP_Weapon_Axe", DisplayName = "AXE VISUAL", ModelId = "tool_stone_axe", ThirdPersonVisual = "AxeVisual",
            FirstPersonGrip = new Vector3(0.4f, -0.38f, 0.56f), GripHeight = 0.12f, FirstPersonTilt = new Vector3(38f, 0f, 12f), FirstPersonYaw = -90f, FirstPersonScale = 0.55f
        },
        new WeaponSpec
        {
            ItemId = "weapon_bow", ProfileId = "visual_weapon_bow", AssetName = "VP_Weapon_Bow", DisplayName = "BOW VISUAL", ModelId = "tool_bow", ThirdPersonVisual = "BowVisual",
            FirstPersonGrip = new Vector3(-0.36f, -0.2f, 0.72f), GripHeight = 0.56f, FirstPersonTilt = new Vector3(0f, 0f, 14f), FirstPersonYaw = -90f, FirstPersonScale = 0.46f
        }
    };

    // ---------------------------------------------------------------- 전체 적용

    public static string BuildAll()
    {
        StringBuilder report = new StringBuilder("[무기 외형 생성]\n");
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();

        if (scene.path != ScenePath)
        {
            report.AppendLine($"✗ 게임 Scene({ScenePath})을 열고 다시 실행하세요. 3인칭 위치를 Scene에서 읽어야 합니다.");
            return report.ToString();
        }

        EquippedToolView view = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);
        ThirdPersonCameraFollow follow = Object.FindFirstObjectByType<ThirdPersonCameraFollow>(FindObjectsInactive.Include);

        if (view == null || follow == null)
        {
            report.AppendLine("✗ 장착 외형(EquippedToolView) 또는 Camera 관리자(ThirdPersonCameraFollow)가 없습니다.");
            return report.ToString();
        }

        List<ContentVisualProfile> profiles = new List<ContentVisualProfile>();

        try
        {
            EditorUtility.DisplayProgressBar(DialogTitle, "무기 외형 설정", 0.3f);

            foreach (WeaponSpec spec in Specs)
            {
                ContentVisualProfile profile = CreateOrUpdateProfile(spec, view.transform, report);

                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayProgressBar(DialogTitle, "등록 목록", 0.6f);
            GameDataRegistryEditor.CreateOrRefreshDefaultRegistry();
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Transform holder = EnsureFirstPersonHolder(follow.transform, view.gameObject.layer);
        Transform arrow = FindNockedArrow();
        view.EditorAssignWeaponViews(follow, holder, profiles.ToArray(), arrow);
        EditorUtility.SetDirty(view);
        ToolSwingAnimation swing = view.GetComponent<ToolSwingAnimation>();

        if (swing != null)
        {
            swing.EditorAssignFirstPersonHolder(holder);
            EditorUtility.SetDirty(swing);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        report.AppendLine($"1인칭 무기 거치대 : {follow.name}/{FirstPersonHolderName} (Layer {LayerMask.LayerToName(holder.gameObject.layer)})");
        report.AppendLine($"장착 외형 연결 : 무기 외형 설정 {profiles.Count}개 · 휘두르기 연출 {(swing != null ? "연결" : "없음")} · 장전 화살 {(arrow != null ? arrow.name : "없음")}");
        report.AppendLine("Scene 변경 완료 : Ctrl+S로 저장하세요.");
        report.Append(Validate(out _));
        return report.ToString();
    }

    private static ContentVisualProfile CreateOrUpdateProfile(WeaponSpec spec, Transform toolHolder, StringBuilder report)
    {
        string path = ProfileFolder + spec.AssetName + ".asset";
        ContentVisualProfile profile = AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(path);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<ContentVisualProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        GameObject model = StylizedArtAssetFactory.LoadModelPrefab(spec.ModelId);

        if (model == null)
        {
            report.AppendLine($"✗ {spec.ModelId} 저폴리 모델이 없습니다. Art 메뉴로 모델을 먼저 만드세요.");
            return null;
        }

        // 3인칭 : 기존 전용 외형(AxeVisual · BowVisual)에 붙은 저폴리 모델의 위치를 도구 거치대 기준으로 읽는다
        Transform legacy = toolHolder.Find(spec.ThirdPersonVisual);
        StylizedVisualReplacement record = legacy != null ? legacy.GetComponent<StylizedVisualReplacement>() : null;
        Transform source = record != null && record.GeneratedVisual != null ? record.GeneratedVisual.transform : legacy;
        Vector3 thirdPosition = Vector3.zero;
        Quaternion thirdRotation = Quaternion.identity;
        Vector3 thirdScale = Vector3.one;

        if (source != null)
        {
            Matrix4x4 local = toolHolder.worldToLocalMatrix * source.localToWorldMatrix;
            thirdPosition = local.GetColumn(3);
            thirdRotation = Quaternion.LookRotation(local.GetColumn(2), local.GetColumn(1));
            thirdScale = new Vector3(local.GetColumn(0).magnitude, local.GetColumn(1).magnitude, local.GetColumn(2).magnitude);
        }
        else
        {
            report.AppendLine($"[참고] {spec.ThirdPersonVisual} 가 없어 3인칭 위치를 기본값으로 둡니다.");
        }

        // 1인칭 : 모델 방향(날 · 활 배를 앞으로) → 옆으로 기울이기 → 앞으로 기울이기, 잡는 곳이 Grip 위치에 오게
        Quaternion firstRotation = Quaternion.Euler(spec.FirstPersonTilt.x, 0f, 0f) * Quaternion.Euler(0f, 0f, spec.FirstPersonTilt.z) * Quaternion.Euler(0f, spec.FirstPersonYaw, 0f);
        Vector3 firstPosition = spec.FirstPersonGrip - firstRotation * new Vector3(0f, spec.GripHeight * spec.FirstPersonScale, 0f);

        SerializedObject serialized = new SerializedObject(profile);
        serialized.FindProperty("profileId").stringValue = spec.ProfileId;
        serialized.FindProperty("displayName").stringValue = spec.DisplayName;
        serialized.FindProperty("category").intValue = (int)ContentVisualCategory.Weapon;
        serialized.FindProperty("visualPrefab").objectReferenceValue = model;
        serialized.FindProperty("createPlaceholderWhenPrefabMissing").boolValue = true;
        serialized.FindProperty("visualLocalScale").vector3Value = Vector3.one;
        serialized.FindProperty("useHeldViews").boolValue = true;
        serialized.FindProperty("thirdPersonPosition").vector3Value = Round(thirdPosition);
        serialized.FindProperty("thirdPersonEulerAngles").vector3Value = Round(thirdRotation.eulerAngles);
        serialized.FindProperty("thirdPersonScale").vector3Value = Round(thirdScale);
        serialized.FindProperty("firstPersonPosition").vector3Value = Round(firstPosition);
        serialized.FindProperty("firstPersonEulerAngles").vector3Value = Round(firstRotation.eulerAngles);
        serialized.FindProperty("firstPersonScale").vector3Value = Vector3.one * spec.FirstPersonScale;

        if (serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(profile);
        }

        report.AppendLine($"{spec.AssetName} ({spec.ProfileId}) : 모델 {model.name} · 3인칭 {Round(thirdPosition)} · 1인칭 {Round(firstPosition)}");
        return profile;
    }

    private static Vector3 Round(Vector3 value) // 반복 실행해도 같은 값이 되게 소수 넷째 자리로 맞춘다
    {
        return new Vector3(Mathf.Round(value.x * 10000f) / 10000f, Mathf.Round(value.y * 10000f) / 10000f, Mathf.Round(value.z * 10000f) / 10000f);
    }

    private static Transform FindNockedArrow() // 활 당기기(PlayerBowChargeController)가 켜고 끄는 장전 화살
    {
        PlayerBowChargeController bow = Object.FindFirstObjectByType<PlayerBowChargeController>(FindObjectsInactive.Include);
        GameObject arrow = bow != null ? new SerializedObject(bow).FindProperty("nockedArrowVisual").objectReferenceValue as GameObject : null;
        return arrow != null ? arrow.transform : null;
    }

    private static Transform EnsureFirstPersonHolder(Transform cameraTransform, int layer)
    {
        Transform holder = cameraTransform.Find(FirstPersonHolderName);

        if (holder == null)
        {
            holder = new GameObject(FirstPersonHolderName).transform;
            holder.SetParent(cameraTransform, false);
        }

        holder.localPosition = Vector3.zero;
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one;
        holder.gameObject.layer = layer;
        return holder;
    }

    // ---------------------------------------------------------------- 검사

    public static string Validate(out int errorCount)
    {
        StringBuilder report = new StringBuilder("[무기 외형 검증]\n");
        int errors = 0;

        void Error(string message)
        {
            errors++;
            report.AppendLine("✗ " + message);
        }

        Dictionary<string, ContentVisualProfile> profiles = new Dictionary<string, ContentVisualProfile>();

        foreach (WeaponSpec spec in Specs)
        {
            ContentVisualProfile profile = AssetDatabase.LoadAssetAtPath<ContentVisualProfile>(ProfileFolder + spec.AssetName + ".asset");
            ItemData item = FindItem(spec.ItemId);

            if (profile == null)
            {
                Error($"{spec.AssetName} 가 없습니다. 15번 메뉴를 실행하세요.");
                continue;
            }

            profiles[spec.ProfileId] = profile;

            if (profile.ProfileId != spec.ProfileId || profile.Category != ContentVisualCategory.Weapon) Error($"{spec.AssetName} : ID · 분류가 {spec.ProfileId} · Weapon 이 아닙니다.");
            if (!profile.UseHeldViews) Error($"{spec.AssetName} : 손 외형(Held Views)이 꺼져 있거나 모델이 없습니다.");
            if (profile.ThirdPersonScale.x <= 0f || profile.FirstPersonScale.x <= 0f) Error($"{spec.AssetName} : 크기가 0입니다.");

            if (item == null)
            {
                Error($"아이템 {spec.ItemId} 가 없습니다.");
            }
            else if (!ContentVisualProfileIdUtility.TryBuildProfileId(ContentVisualIdentityCategory.Weapon, item.ItemId, false, string.Empty, out string resolved, out string idError) || resolved != spec.ProfileId)
            {
                Error($"아이템 {spec.ItemId} 가 무기 외형 {spec.ProfileId} 로 연결되지 않습니다. {idError}");
            }
        }

        GameDataRegistry registry = AssetDatabase.LoadAssetAtPath<GameDataRegistry>("Assets/_ProjectU/Data/Registry/GameDataRegistry.asset");

        if (registry != null)
        {
            registry.RebuildLookup(false);

            foreach (string id in profiles.Keys)
            {
                if (!registry.TryGetVisualProfile(id, out _)) Error($"{id} 가 GameDataRegistry에 없습니다.");
            }
        }

        EquippedToolView view = Object.FindFirstObjectByType<EquippedToolView>(FindObjectsInactive.Include);

        if (view == null || EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            report.AppendLine("[건너뜀] 게임 Scene이 열려 있지 않아 Scene 연결 검사를 생략했습니다.");
        }
        else
        {
            SerializedObject serialized = new SerializedObject(view);
            Transform holder = serialized.FindProperty("firstPersonHolder").objectReferenceValue as Transform;
            ThirdPersonCameraFollow follow = serialized.FindProperty("cameraFollow").objectReferenceValue as ThirdPersonCameraFollow;
            SerializedProperty list = serialized.FindProperty("weaponProfiles");
            HashSet<Object> linked = new HashSet<Object>();

            for (int index = 0; index < list.arraySize; index++)
            {
                linked.Add(list.GetArrayElementAtIndex(index).objectReferenceValue);
            }

            if (follow == null) Error("장착 외형에 Camera 관리자가 연결되지 않았습니다.");
            if (holder == null || follow == null || holder.parent != follow.transform) Error($"1인칭 무기 거치대가 Camera 아래에 없습니다 ({FirstPersonHolderName}).");

            foreach (ContentVisualProfile profile in profiles.Values)
            {
                if (!linked.Contains(profile)) Error($"장착 외형에 {profile.ProfileId} 가 연결되지 않았습니다.");
            }

            Transform arrow = FindNockedArrow();

            if (arrow != null && serialized.FindProperty("nockedArrow").objectReferenceValue != arrow)
            {
                Error("장착 외형에 장전 화살이 연결되지 않아 새 활 외형에서 화살이 안 보입니다.");
            }

            ToolSwingAnimation swing = view.GetComponent<ToolSwingAnimation>();

            if (swing != null && new SerializedObject(swing).FindProperty("firstPersonHolder").objectReferenceValue != holder)
            {
                Error("휘두르기 연출에 1인칭 무기 거치대가 연결되지 않았습니다.");
            }
        }

        report.AppendLine($"무기 외형 설정 {profiles.Count}/{Specs.Length}개 (도끼 · 활)");
        report.AppendLine(errors == 0 ? "결과 : 오류 0개" : $"결과 : 오류 {errors}개");
        errorCount = errors;
        return report.ToString();
    }

    private static ItemData FindItem(string itemId)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_ProjectU/Data" }))
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));

            if (item != null && item.ItemId == itemId)
            {
                return item;
            }
        }

        return null;
    }
}
