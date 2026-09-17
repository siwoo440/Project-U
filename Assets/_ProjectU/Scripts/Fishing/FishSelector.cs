using System.Collections.Generic; // 목록 기능
using UnityEngine; // Unity 기본 기능

// 83일차: 현재 조건(물가·계절·날씨·시각·낚싯대)에 맞는 물고기를 출현 가중치로 뽑는다.
// 미끼를 쓰면 일반 등급보다 높은 물고기의 가중치가 올라간다.
public static class FishSelector
{
    private static readonly List<FishData> candidateBuffer = new List<FishData>(8); // 재사용 후보 목록

    public static float GetWeight(FishData fish, in FishingConditions conditions, float rareBaitMultiplier) // 조건 적용 가중치
    {
        if (fish == null || !fish.CanAppear(conditions.WaterBody, conditions.Season, conditions.Weather, conditions.Hour, conditions.RodTier)) // 출현 가능 확인
        {
            return 0f; // 출현 불가
        }

        float weight = fish.SpawnWeight; // 기본 가중치
        return conditions.UsingBait && fish.Rarity >= FishRarity.Uncommon ? weight * Mathf.Max(1f, rareBaitMultiplier) : weight; // 미끼 보정 결과 반환
    }

    public static int CollectCandidates(IReadOnlyList<FishData> fishList, in FishingConditions conditions, List<FishData> results) // 출현 가능한 물고기 수집
    {
        results.Clear(); // 이전 결과 제거

        if (fishList == null) // 목록 확인
        {
            return 0; // 후보 없음
        }

        for (int index = 0; index < fishList.Count; index++) // 전체 물고기 순회
        {
            if (GetWeight(fishList[index], conditions, 1f) > 0f) // 출현 가능 확인
            {
                results.Add(fishList[index]); // 후보 추가
            }
        }

        return results.Count; // 후보 수 반환
    }

    public static float GetChance(IReadOnlyList<FishData> fishList, FishData target, in FishingConditions conditions, float rareBaitMultiplier) // 특정 물고기가 뽑힐 확률 (0~1)
    {
        float total = GetTotalWeight(fishList, conditions, rareBaitMultiplier); // 전체 가중치
        return total > 0f ? GetWeight(target, conditions, rareBaitMultiplier) / total : 0f; // 비율 반환
    }

    public static bool TrySelect(IReadOnlyList<FishData> fishList, in FishingConditions conditions, float rareBaitMultiplier, float roll, out FishData selected) // 가중치 랜덤 선택 (roll 0~1)
    {
        selected = null; // 기본 결과
        CollectCandidates(fishList, conditions, candidateBuffer); // 후보 수집

        if (candidateBuffer.Count == 0) // 후보 확인
        {
            return false; // 선택 실패
        }

        float total = 0f; // 전체 가중치

        for (int index = 0; index < candidateBuffer.Count; index++) // 후보 순회
        {
            total += GetWeight(candidateBuffer[index], conditions, rareBaitMultiplier); // 가중치 합산
        }

        float target = Mathf.Clamp01(roll) * total; // 목표 누적값
        float cumulative = 0f; // 누적 가중치

        for (int index = 0; index < candidateBuffer.Count; index++) // 후보 순회
        {
            cumulative += GetWeight(candidateBuffer[index], conditions, rareBaitMultiplier); // 누적
            selected = candidateBuffer[index]; // 마지막 후보 기록 (roll = 1 대비)

            if (target < cumulative) // 구간 확인
            {
                break; // 선택 확정
            }
        }

        return selected != null; // 선택 결과 반환
    }

    private static float GetTotalWeight(IReadOnlyList<FishData> fishList, in FishingConditions conditions, float rareBaitMultiplier) // 전체 가중치 합
    {
        float total = 0f; // 합계

        if (fishList == null) // 목록 확인
        {
            return total; // 0 반환
        }

        for (int index = 0; index < fishList.Count; index++) // 전체 물고기 순회
        {
            total += GetWeight(fishList[index], conditions, rareBaitMultiplier); // 합산
        }

        return total; // 합계 반환
    }
}
