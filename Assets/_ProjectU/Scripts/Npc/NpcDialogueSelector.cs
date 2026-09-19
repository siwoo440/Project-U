using System.Collections.Generic; // 목록

// 91일차: 대화 창에서 쓸 대사 고르기
// 여는 말 : 처음 만남 → 생일 인사 → 현재 단계에 맞는 인사
// 대화하기 : 나쁜 날씨 → 계절 → 현재 호감도 단계 → 잡담(단계 조건) → 이웃 이야기(114일차) → 이벤트(신뢰 이상) 순서로 돌아가며
public static class NpcDialogueSelector
{
    public const string Silent = "…"; // 대사가 없을 때

    public static string Opening(NpcCharacterData character, AffinityStage stage, bool firstMeeting, int day) // 여는 말
    {
        NpcDialogueSet dialogue = character != null ? character.DialogueSet : null;

        if (dialogue == null)
        {
            return Silent;
        }

        if (firstMeeting)
        {
            List<NpcDialogueSet.Line> first = dialogue.GetLines(NpcDialogueKind.FirstMeeting, AffinityStage.Love);

            if (first.Count > 0)
            {
                return first[0].Text;
            }
        }

        List<NpcDialogueSet.Line> greetings = dialogue.GetLines(NpcDialogueKind.Greeting, stage);

        if (greetings.Count == 0)
        {
            return Silent;
        }

        // 단계 조건이 높은 인사를 먼저 (신뢰 이상이면 신뢰 인사가 섞여 나옴)
        greetings.Sort((left, right) => right.Stage.CompareTo(left.Stage));
        int highest = (int)greetings[0].Stage;
        List<NpcDialogueSet.Line> best = greetings.FindAll(line => (int)line.Stage == highest);
        List<NpcDialogueSet.Line> pool = highest > 0 && day % 2 == 1 ? greetings : best;
        return pool[PositiveModulo(day, pool.Count)].Text;
    }

    public static List<string> TalkLines(NpcCharacterData character, AffinityStage stage, SeasonType season, WeatherType weather) // 대화하기 순서
    {
        List<string> lines = new List<string>();
        NpcDialogueSet dialogue = character != null ? character.DialogueSet : null;

        if (dialogue == null)
        {
            lines.Add(Silent);
            return lines;
        }

        if (NpcCalendar.IsBadWeather(weather) && dialogue.TryGetWeatherLine(weather, out NpcDialogueSet.Line weatherLine))
        {
            lines.Add(weatherLine.Text);
        }

        if (dialogue.TryGetSeasonLine(season, out NpcDialogueSet.Line seasonLine))
        {
            lines.Add(seasonLine.Text);
        }

        if (dialogue.TryGetStageLine(stage, out NpcDialogueSet.Line stageLine))
        {
            lines.Add(stageLine.Text);
        }

        foreach (NpcDialogueSet.Line talk in dialogue.GetLines(NpcDialogueKind.Talk, stage))
        {
            lines.Add(talk.Text);
        }

        lines.AddRange(NpcBanterManager.MentionLinesFor(character)); // 114일차: 관계 있는 이웃 이야기

        if (stage >= AffinityStage.Trust)
        {
            foreach (NpcDialogueSet.Line eventLine in dialogue.GetLines(NpcDialogueKind.Event, AffinityStage.Love))
            {
                lines.Add(eventLine.Text);
            }
        }

        if (lines.Count == 0)
        {
            lines.Add(Silent);
        }

        return lines;
    }

    public static string GiftReaction(NpcCharacterData character, GiftPreference preference, bool birthday) // 선물 반응
    {
        NpcDialogueSet dialogue = character != null ? character.DialogueSet : null;

        if (dialogue == null)
        {
            return Silent;
        }

        if (birthday && preference <= GiftPreference.Liked)
        {
            List<NpcDialogueSet.Line> birthdayLines = dialogue.GetLines(NpcDialogueKind.Birthday, AffinityStage.Love);

            if (birthdayLines.Count > 0)
            {
                return birthdayLines[0].Text;
            }
        }

        return dialogue.TryGetGiftLine(preference, out NpcDialogueSet.Line line) ? line.Text : Silent;
    }

    public static string StageName(AffinityStage stage) // 단계 표시 이름 (캐릭터 시트 코드표)
    {
        switch (stage)
        {
            case AffinityStage.Curious: return "호기심";
            case AffinityStage.Trust: return "신뢰";
            case AffinityStage.Affection: return "애정";
            case AffinityStage.Love: return "사랑";
            default: return "무관심";
        }
    }

    public static string PreferenceName(GiftPreference preference) // 선물 반응 표시 이름
    {
        switch (preference)
        {
            case GiftPreference.Loved: return "매우 좋아함";
            case GiftPreference.Liked: return "좋아함";
            case GiftPreference.Disliked: return "싫어함";
            case GiftPreference.Hated: return "매우 싫어함";
            default: return "보통";
        }
    }

    private static int PositiveModulo(int value, int count) // 음수 없는 나머지
    {
        return count <= 0 ? 0 : ((value % count) + count) % count;
    }
}
