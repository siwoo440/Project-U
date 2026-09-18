using UnityEditor;
using UnityEngine;

// 90일차: NPC 테스트 메뉴 (Play 중에만) / 91일차: 호감도 · 선물 테스트
public static class NpcDebugMenu
{
    private const string MenuRoot = "Tools/Project U/Debug (Play Mode)/NPC/";

    [MenuItem(MenuRoot + "Log NPC Status", false, 90)]
    private static void LogStatus()
    {
        Debug.Log(NpcManager.Instance.DescribeAll());
    }

    [MenuItem(MenuRoot + "Snap All To Schedule", false, 91)]
    private static void SnapAll()
    {
        NpcManager.Instance.Refresh(true);
        Debug.Log(NpcManager.Instance.DescribeAll());
    }

    [MenuItem(MenuRoot + "Skip 1 Hour (NPCs Walk)", false, 92)]
    private static void SkipHour()
    {
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>();
        cycle.AdvanceToHour(cycle.CurrentHour + 1f);
        NpcManager.Instance.Refresh(false); // 1시간은 순간 이동 없이 걸어서 이동
        Debug.Log(NpcManager.Instance.DescribeAll());
    }

    [MenuItem(MenuRoot + "Teleport Player To Village", false, 93)]
    private static void TeleportPlayer()
    {
        PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.position = new Vector3(0f, 0.1f, 25f);

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    [MenuItem(MenuRoot + "Affinity +20 (Nearest NPC)", false, 100)]
    private static void AddAffinity()
    {
        NpcAgent npc = FindNearest();
        NpcRelationshipManager.Instance.ChangeAffinity(npc.Character, 20);
        Debug.Log($"{npc.DisplayName} 호감도 {NpcRelationshipManager.Instance.GetAffinity(npc.Character)} ({NpcDialogueSelector.StageName(NpcRelationshipManager.Instance.GetStage(npc.Character))})");
    }

    [MenuItem(MenuRoot + "Give Loved Gift Item (Nearest NPC)", false, 101)]
    private static void GiveLovedItem()
    {
        NpcAgent npc = FindNearest();
        PlayerInventory inventory = Object.FindFirstObjectByType<PlayerInventory>();

        foreach (NpcGiftProfile.Entry entry in npc.Character.GiftProfile.Entries)
        {
            if (entry.Item != null && entry.Preference == GiftPreference.Loved)
            {
                inventory.AddItem(entry.Item, 1);
                Debug.Log($"{npc.DisplayName}가 매우 좋아하는 {entry.Item.DisplayName}을(를) 가방에 넣었습니다.");
                return;
            }
        }
    }

    [MenuItem(MenuRoot + "Reset All Relationships", false, 102)]
    private static void ResetRelationships()
    {
        NpcRelationshipManager.Instance.ResetForLoad();
        Debug.Log("모든 NPC 관계를 처음 상태로 되돌렸습니다.");
    }

    private static NpcAgent FindNearest() // 플레이어와 가장 가까운 NPC
    {
        Transform player = Object.FindFirstObjectByType<PlayerInventory>().transform;
        NpcAgent nearest = null;
        float best = float.MaxValue;

        foreach (NpcAgent agent in NpcManager.Instance.Agents)
        {
            if (agent == null)
            {
                continue;
            }

            float distance = (agent.transform.position - player.position).sqrMagnitude;

            if (distance < best)
            {
                best = distance;
                nearest = agent;
            }
        }

        return nearest;
    }

    [MenuItem(MenuRoot + "Affinity +20 (Nearest NPC)", true)]
    [MenuItem(MenuRoot + "Give Loved Gift Item (Nearest NPC)", true)]
    [MenuItem(MenuRoot + "Reset All Relationships", true)]
    private static bool CanUseRelationships()
    {
        return EditorApplication.isPlaying && NpcManager.Instance != null && NpcRelationshipManager.Instance != null && NpcManager.Instance.Agents.Count > 0;
    }

    [MenuItem(MenuRoot + "Log NPC Status", true)]
    [MenuItem(MenuRoot + "Snap All To Schedule", true)]
    [MenuItem(MenuRoot + "Skip 1 Hour (NPCs Walk)", true)]
    [MenuItem(MenuRoot + "Teleport Player To Village", true)]
    private static bool CanUse()
    {
        return EditorApplication.isPlaying && NpcManager.Instance != null;
    }
}
