using UnityEditor;
using UnityEngine;

// 90일차: NPC 테스트 메뉴 (Play 중에만)
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

    [MenuItem(MenuRoot + "Log NPC Status", true)]
    [MenuItem(MenuRoot + "Snap All To Schedule", true)]
    [MenuItem(MenuRoot + "Skip 1 Hour (NPCs Walk)", true)]
    [MenuItem(MenuRoot + "Teleport Player To Village", true)]
    private static bool CanUse()
    {
        return EditorApplication.isPlaying && NpcManager.Instance != null;
    }
}
