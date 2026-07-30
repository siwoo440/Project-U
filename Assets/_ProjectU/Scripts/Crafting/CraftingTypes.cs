public enum CraftingFacilityType // 제작 시설 종류
{
    Hand = 0, // 맨손 제작
    Workbench = 1, // 작업대 제작
    Campfire = 2, // 모닥불 조리
    AdvancedWorkbench = 3 // 상급 작업대 제작
}

public enum CraftingUnlockType // 제작법 해금 종류
{
    Default = 0, // 기본 해금
    Blueprint = 1, // 설계도 해금
    Quest = 2 // 퀘스트 해금
}

public static class CraftingFacilityIds // 제작 시설 ID 관리
{
    public const string Hand = "facility_hand"; // 맨손 제작 ID
    public const string Workbench = "facility_workbench"; // 작업대 ID
    public const string Campfire = "facility_campfire"; // 모닥불 ID
    public const string AdvancedWorkbench = "facility_advanced_workbench"; // 상급 작업대 ID

    public static string GetFacilityId(CraftingFacilityType facilityType) // 시설 종류별 ID 반환
    {
        switch (facilityType) // 시설 종류 판정
        {
            case CraftingFacilityType.Hand: // 맨손 제작 판정
            {
                return Hand; // 맨손 제작 ID 반환
            }

            case CraftingFacilityType.Workbench: // 작업대 판정
            {
                return Workbench; // 작업대 ID 반환
            }

            case CraftingFacilityType.Campfire: // 모닥불 판정
            {
                return Campfire; // 모닥불 ID 반환
            }

            case CraftingFacilityType.AdvancedWorkbench: // 상급 작업대 판정
            {
                return AdvancedWorkbench; // 상급 작업대 ID 반환
            }

            default: // 정의되지 않은 시설
            {
                return string.Empty; // 빈 ID 반환
            }
        }
    }
}
