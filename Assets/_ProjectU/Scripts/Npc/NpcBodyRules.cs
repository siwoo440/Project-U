using UnityEngine; // Unity 기본 기능

public enum NpcMotionStyle // 100일차: 하반신 모양별 움직임
{
    Walk = 0, // 걷기 (위아래 흔들림)
    Slither = 1, // 미끄러지기 (뱀 · 인어 꼬리, 좌우로 흔들림)
    Hover = 2, // 떠다니기 (유령, 멈춰 있어도 둥실)
    Gallop = 3, // 말 걸음 (크게 흔들림)
    Skitter = 4, // 여러 다리 (거미 · 전갈 · 촉수, 빠르고 작게)
    Jelly = 5, // 젤리 (슬라임, 눌렸다 늘어남)
    Hop = 6 // 깡충 (보물상자)
}

public readonly struct NpcBodyMetrics // 하반신 모양별 크기 · 움직임 규칙
{
    public readonly float AgentRadius; // 길찾기 반지름
    public readonly float Height; // 몸 높이 (충돌체 · 길찾기)
    public readonly float ColliderRadius; // 몸 충돌체 반지름
    public readonly bool NeedsBodyBlock; // 뒤로 긴 몸통 충돌체가 더 필요한지 (말 · 뱀 · 전갈 · 거미 · 촉수)
    public readonly float SpeedScale; // 걷기 속도 배율
    public readonly NpcMotionStyle Motion; // 움직임
    public readonly bool WaterBound; // 물가에만 서는지

    public NpcBodyMetrics(float agentRadius, float height, float colliderRadius, bool needsBodyBlock, float speedScale, NpcMotionStyle motion, bool waterBound)
    {
        AgentRadius = agentRadius;
        Height = height;
        ColliderRadius = colliderRadius;
        NeedsBodyBlock = needsBodyBlock;
        SpeedScale = speedScale;
        Motion = motion;
        WaterBound = waterBound;
    }
}

public static class NpcBodyRules // 100일차: NPC 모델 모양 값 → 충돌체 · 길찾기 · 걷기 방식
{
    public const float DefaultAgentRadius = 0.3f; // 사람 체형 (90일차 값)
    public const float DefaultHeight = 1.7f;
    public const float DefaultColliderRadius = 0.32f;

    public static NpcBodyMetrics Get(StylizedModelLibrary.NpcLook look) // 모양 값으로 규칙 계산
    {
        bool water = look.Body == StylizedModelLibrary.NpcBody.FishTail
            || look.Body == StylizedModelLibrary.NpcBody.Tentacles
            || look.Species == StylizedModelLibrary.NpcSpecies.Shark;

        if (look.Body == StylizedModelLibrary.NpcBody.Legs)
        {
            return new NpcBodyMetrics(DefaultAgentRadius, DefaultHeight, DefaultColliderRadius, false, 1f, NpcMotionStyle.Walk, water);
        }

        StylizedModelLibrary.NpcBodyShape shape = StylizedModelLibrary.GetBodyShape(look.Body);
        float height = Mathf.Max(1.2f, StylizedModelLibrary.GetNpcStandingHeight(look) - 0.1f);
        float agentRadius = Mathf.Clamp(shape.FootprintRadius, DefaultAgentRadius, 0.6f);
        float colliderRadius = Mathf.Clamp(shape.FootprintRadius, DefaultColliderRadius, 0.55f);
        bool bodyBlock = shape.FootprintLength > colliderRadius * 2.6f;

        switch (look.Body)
        {
            case StylizedModelLibrary.NpcBody.SnakeTail:
            case StylizedModelLibrary.NpcBody.FishTail:
                return new NpcBodyMetrics(agentRadius, height, colliderRadius, bodyBlock, 0.9f, NpcMotionStyle.Slither, water);
            case StylizedModelLibrary.NpcBody.HorseBody:
                return new NpcBodyMetrics(agentRadius, height, colliderRadius, bodyBlock, 1.3f, NpcMotionStyle.Gallop, water);
            case StylizedModelLibrary.NpcBody.SpiderLegs:
            case StylizedModelLibrary.NpcBody.ScorpionBody:
            case StylizedModelLibrary.NpcBody.Tentacles:
                return new NpcBodyMetrics(agentRadius, height, colliderRadius, bodyBlock, look.Body == StylizedModelLibrary.NpcBody.Tentacles ? 0.75f : 1.05f, NpcMotionStyle.Skitter, water);
            case StylizedModelLibrary.NpcBody.Floating:
                return new NpcBodyMetrics(agentRadius, height, colliderRadius, false, 0.85f, NpcMotionStyle.Hover, water);
            case StylizedModelLibrary.NpcBody.SlimeBase:
                return new NpcBodyMetrics(agentRadius, height, colliderRadius, false, 0.75f, NpcMotionStyle.Jelly, water);
            case StylizedModelLibrary.NpcBody.MimicChest:
                return new NpcBodyMetrics(agentRadius, height, colliderRadius, false, 0.8f, NpcMotionStyle.Hop, water);
            default:
                return new NpcBodyMetrics(agentRadius, height, colliderRadius, bodyBlock, 1f, NpcMotionStyle.Walk, water);
        }
    }

    public static bool TryGet(string characterId, out NpcBodyMetrics metrics) // 캐릭터 ID → 규칙 (모델이 없으면 사람 체형)
    {
        if (StylizedModelLibrary.TryGetNpcLook(StylizedModelLibrary.GetNpcModelId(characterId), out StylizedModelLibrary.NpcLook look))
        {
            metrics = Get(look);
            return true;
        }

        metrics = new NpcBodyMetrics(DefaultAgentRadius, DefaultHeight, DefaultColliderRadius, false, 1f, NpcMotionStyle.Walk, false);
        return false;
    }
}
