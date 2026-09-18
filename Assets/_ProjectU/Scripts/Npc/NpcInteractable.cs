using System.Collections.Generic; // 목록
using UnityEngine; // Unity 기본 기능

[DisallowMultipleComponent] // 동일 컴포넌트 중복 방지
[RequireComponent(typeof(NpcAgent))] // NPC 필요
public sealed class NpcInteractable : InteractableBase // 90일차: NPC에게 다가가면 이름·하는 일 표시 / 91일차: 말을 걸면 대화 창 (창이 없으면 말풍선)
{
    [Tooltip("말풍선 표시 시간 (초).")]
    [SerializeField, Min(1f)] private float speechSeconds = 4.5f; // 말풍선 시간

    private NpcAgent npc; // NPC
    private GameUIManager uiManager; // 팝업 관리자
    private int talkCount; // 오늘 말 건 횟수
    private int talkDay = -1; // 말 건 날짜

    public override string PromptMessage // 안내 문구
    {
        get
        {
            NpcAgent agent = GetAgent();

            if (NpcManager.Instance != null && NpcManager.Instance.GetStallFor(agent) != null)
            {
                return $"F - {agent.DisplayName} | 대화 · 거래";
            }

            string activity = agent.HasArrived ? agent.CurrentActivity : "이동 중";
            return string.IsNullOrEmpty(activity) ? $"F - {agent.DisplayName}" : $"F - {agent.DisplayName} | {activity}";
        }
    }

    public override void Interact(GameObject interactor) // 말 걸기
    {
        NpcAgent agent = GetAgent();
        NpcManager manager = NpcManager.Instance;

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<GameUIManager>();
        }

        if (uiManager != null && uiManager.NpcDialoguePopup != null && uiManager.OpenNpcDialogue(agent))
        {
            return; // 대화 창 (가판대 상인은 창의 거래 버튼으로 상점)
        }

        if (interactor != null)
        {
            agent.FaceTowards(interactor.transform.position);
        }

        agent.Say(PickLine(agent, manager), speechSeconds);
    }

    private string PickLine(NpcAgent agent, NpcManager manager) // 오늘 몇 번째 말인지에 따라 인사 → 날씨·계절 → 잡담
    {
        NpcDialogueSet dialogue = agent.Character != null ? agent.Character.DialogueSet : null;

        if (dialogue == null)
        {
            return "…";
        }

        int day = manager != null ? manager.CurrentDay : 1;

        if (day != talkDay)
        {
            talkDay = day;
            talkCount = 0;
        }

        List<string> choices = new List<string>();
        List<NpcDialogueSet.Line> greetings = dialogue.GetLines(NpcDialogueKind.Greeting, AffinityStage.Uninterested);

        if (greetings.Count > 0)
        {
            choices.Add(greetings[day % greetings.Count].Text);
        }

        if (manager != null && NpcCalendar.IsBadWeather(manager.CurrentWeather) && dialogue.TryGetWeatherLine(manager.CurrentWeather, out NpcDialogueSet.Line weather))
        {
            choices.Add(weather.Text);
        }
        else if (manager != null && dialogue.TryGetSeasonLine(manager.CurrentSeason, out NpcDialogueSet.Line season))
        {
            choices.Add(season.Text);
        }

        foreach (NpcDialogueSet.Line talk in dialogue.GetLines(NpcDialogueKind.Talk, AffinityStage.Uninterested))
        {
            choices.Add(talk.Text);
        }

        string line = choices.Count > 0 ? choices[Mathf.Min(talkCount, choices.Count - 1)] : "…";
        talkCount = choices.Count > 0 ? (talkCount + 1) % choices.Count : 0;
        return line;
    }

    private NpcAgent GetAgent() // NPC 찾기
    {
        if (npc == null)
        {
            npc = GetComponent<NpcAgent>();
        }

        return npc;
    }
}
