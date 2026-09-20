using System; // 직렬화
using System.Collections.Generic; // 목록
using System.Linq; // 목록 계산
using UnityEngine; // Unity 기본 기능

[Serializable]
public sealed class FruitTreeSaveData // 119일차: 심어 둔 과일나무 하나
{
    [Tooltip("나무 종류 ID.")]
    public string speciesId = string.Empty; // 나무 종류
    [Tooltip("심은 자리.")]
    public SaveVector3Data position = new SaveVector3Data(); // 심은 자리
    [Tooltip("자란 날 수.")]
    public int grownDays; // 자란 날 수
    [Tooltip("아직 따지 않은 열매 수.")]
    public int readyFruit; // 남은 열매
    [Tooltip("이 계절에 이미 열매를 맺었는지.")]
    public bool fruitedThisSeason; // 이 계절 열매 여부
}

[Serializable]
public sealed class OrchardSaveData // 119일차: 과일나무 전체 상태
{
    [Tooltip("심어 둔 과일나무 목록.")]
    public List<FruitTreeSaveData> trees = new List<FruitTreeSaveData>(); // 과일나무 목록
}

public static class OrchardSaveBridge // 119일차: 과일나무 자란 정도 저장 연결
{
    private const float MatchDistance = 1.5f; // 같은 나무로 볼 거리 (m)

    public static bool TryCapture(SaveGameData saveData, out string errorMessage) // 저장
    {
        if (saveData == null)
        {
            errorMessage = "과일나무 저장 데이터가 없습니다.";
            return false;
        }

        FruitTree[] trees = UnityEngine.Object.FindObjectsByType<FruitTree>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        saveData.hasOrchardData = true;
        saveData.orchard = new OrchardSaveData
        {
            trees = trees.Select(tree => new FruitTreeSaveData
            {
                speciesId = tree.SpeciesId,
                position = SaveVector3Data.FromVector3(tree.transform.position),
                grownDays = tree.GrownDays,
                readyFruit = tree.ReadyFruit,
                fruitedThisSeason = tree.FruitedThisSeason
            }).ToList()
        };
        errorMessage = string.Empty;
        return true;
    }

    public static void Apply(SaveGameData saveData) // 불러오기 : 심어 둔 자리와 가까운 나무에 자란 정도를 돌려준다
    {
        if (saveData == null || !saveData.hasOrchardData || saveData.orchard == null || saveData.orchard.trees == null)
        {
            return;
        }

        List<FruitTree> trees = UnityEngine.Object.FindObjectsByType<FruitTree>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

        foreach (FruitTreeSaveData row in saveData.orchard.trees)
        {
            if (row == null)
            {
                continue;
            }

            Vector3 spot = row.position.ToVector3();
            FruitTree match = trees
                .Where(tree => tree.SpeciesId == row.speciesId && Vector3.Distance(tree.transform.position, spot) < MatchDistance)
                .OrderBy(tree => Vector3.Distance(tree.transform.position, spot))
                .FirstOrDefault();

            if (match == null)
            {
                continue;
            }

            match.ApplyState(row.grownDays, row.readyFruit, row.fruitedThisSeason);
            trees.Remove(match);
        }
    }
}
