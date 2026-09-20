using System;
using System.Collections.Generic;
using UnityEngine;

// 78일차: 저폴리 임시 모델 도감
// 모든 모델은 바닥(y = 0) 중심 기준이며 앞 방향은 +Z 이다.
// 건축·가구 모델은 가로·세로·높이 1 크기 상자 안에 만들어 실제 오브젝트 크기에 맞춰 늘려 쓴다.
public static partial class StylizedModelLibrary
{
    public enum FitMode
    {
        // 기존 외형 영역에 가로·세로·높이를 각각 맞춤 (건축물·가구)
        Stretch = 0,
        // 비율을 유지하며 기존 외형 높이에 맞춤 (나무·캐릭터)
        UniformHeight = 1,
        // 비율을 유지하며 기존 외형의 가장 큰 변에 맞춤 (작은 아이템)
        UniformLargest = 2,
        // 비율을 유지하며 바닥 면적에 맞춤 (모닥불)
        UniformFootprint = 3
    }

    public readonly struct ModelInfo
    {
        public readonly string Id;
        public readonly FitMode Fit;
        public readonly Action<LowPolyMeshBuilder> Build;

        public ModelInfo(string id, FitMode fit, Action<LowPolyMeshBuilder> build)
        {
            Id = id;
            Fit = fit;
            Build = build;
        }
    }

    private static Dictionary<string, ModelInfo> catalog;

    public static IReadOnlyDictionary<string, ModelInfo> Catalog
    {
        get
        {
            if (catalog == null)
            {
                catalog = new Dictionary<string, ModelInfo>(StringComparer.Ordinal);
                RegisterNature();
                RegisterItems();
                RegisterBuildables();
                RegisterCharacters();
                RegisterProps();
                RegisterFarming();
                RegisterFishing();
                RegisterCooking();
                RegisterLivestock();
                RegisterMarket();
                RegisterNpc();
                RegisterZones();
                RegisterNpcGoods();
                RegisterZoneHomes();
                RegisterIsland();
                RegisterCaves(); // 116일차: 동굴
                RegisterMinerals(); // 117일차: 광물 · 주괴 · 도구 등급 · 용광로
            }

            return catalog;
        }
    }

    public static bool TryBuild(string modelId, LowPolyMeshBuilder builder)
    {
        if (!Catalog.TryGetValue(modelId, out ModelInfo info))
        {
            return false;
        }

        builder.Clear();
        info.Build(builder);
        return true;
    }

    private static void Register(string id, FitMode fit, Action<LowPolyMeshBuilder> build)
    {
        catalog[id] = new ModelInfo(id, fit, build);
    }

    // ---------------------------------------------------------------- 공통 도우미

    private static Quaternion Euler(float x, float y, float z)
    {
        return Quaternion.Euler(x, y, z);
    }

    private static float Rand(System.Random random, float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }

    // 가로로 누운 통나무 (X축 방향)
    private static void AddLog(LowPolyMeshBuilder b, Vector3 center, float length, float radius, float yaw, StylizedColor bark, StylizedColor cut)
    {
        b.Push(center, Euler(0f, yaw, 90f), Vector3.one);
        b.AddFrustum(bark, new Vector3(0f, -length * 0.5f, 0f), radius, radius * 0.94f, length, 7, false, false);
        b.AddDisc(cut, new Vector3(0f, length * 0.5f, 0f), radius * 0.94f, 7);
        b.Push(new Vector3(0f, -length * 0.5f, 0f), Euler(180f, 0f, 0f), Vector3.one);
        b.AddDisc(cut, Vector3.zero, radius, 7);
        b.Pop();
        b.Pop();
    }

    // 나뭇잎 덩어리
    private static void AddFoliage(LowPolyMeshBuilder b, Vector3 center, Vector3 radii, StylizedColor color, int seed)
    {
        b.AddLowPolySphere(color, center, radii, 1, 0.16f, seed);
    }
}
