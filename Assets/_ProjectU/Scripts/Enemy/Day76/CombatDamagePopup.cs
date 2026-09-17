using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 76일차: 피격 지점에 떠올랐다 사라지는 피해 숫자 (간단한 풀 사용)
[DisallowMultipleComponent]
public sealed class CombatDamagePopup : MonoBehaviour
{
    private const int MaxPoolSize = 24;
    private static readonly Stack<CombatDamagePopup> Pool = new Stack<CombatDamagePopup>();
    private static Transform poolRoot;

    [SerializeField, Min(0.1f)] private float lifetime = 0.8f;
    [SerializeField] private float riseDistance = 0.9f;

    private TextMeshPro label;
    private Color baseColor;
    private float baseSize;
    private float startTime;
    private Vector3 startPosition;
    private Vector3 drift;

    public static void Spawn(Vector3 worldPosition, float damage, Color color, float size)
    {
        if (damage <= 0f)
        {
            return;
        }

        CombatDamagePopup popup = null;

        // Scene 전환으로 파괴된 항목은 건너뛰기
        while (popup == null && Pool.Count > 0)
        {
            popup = Pool.Pop();
        }

        if (popup == null)
        {
            popup = CreateNew();
        }

        string text = damage >= 10f ? Mathf.RoundToInt(damage).ToString() : damage.ToString("0.#");
        popup.Play(worldPosition, text, color, size);
    }

    // 81일차: 수확 알림처럼 숫자가 아닌 글자를 띄운다
    public static void SpawnText(Vector3 worldPosition, string text, Color color, float size)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        CombatDamagePopup popup = null;

        while (popup == null && Pool.Count > 0)
        {
            popup = Pool.Pop();
        }

        if (popup == null)
        {
            popup = CreateNew();
        }

        popup.Play(worldPosition, text, color, size);
    }

    private static CombatDamagePopup CreateNew()
    {
        if (poolRoot == null)
        {
            poolRoot = new GameObject("CombatDamagePopups").transform;
        }

        GameObject popupObject = new GameObject("DamagePopup");
        popupObject.transform.SetParent(poolRoot, false);

        TextMeshPro text = popupObject.AddComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(6f, 1f);
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 255);

        CombatDamagePopup popup = popupObject.AddComponent<CombatDamagePopup>();
        popup.label = text;
        return popup;
    }

    private void Play(Vector3 worldPosition, string text, Color color, float size)
    {
        gameObject.SetActive(true);
        label.text = text;
        baseColor = color;
        baseSize = size;
        label.fontSize = size;
        label.color = color;

        startTime = Time.time;
        startPosition = worldPosition;
        Vector2 random = Random.insideUnitCircle * 0.25f;
        drift = new Vector3(random.x, 0f, random.y);
        UpdateVisual();
    }

    private void LateUpdate()
    {
        if (Time.time - startTime >= lifetime)
        {
            Release();
            return;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float t = Mathf.Clamp01((Time.time - startTime) / lifetime);
        float rise = 1f - (1f - t) * (1f - t);
        Camera viewCamera = Camera.main;
        Vector3 towardCamera = Vector3.zero;

        if (viewCamera != null)
        {
            transform.rotation = viewCamera.transform.rotation;
            towardCamera = -viewCamera.transform.forward * 0.3f;
        }

        transform.position = startPosition + towardCamera + drift * t + Vector3.up * (riseDistance * rise);

        // 처음에 크게 튀었다가 원래 크기로 돌아오는 연출
        float punch = t < 0.15f ? Mathf.Lerp(1.5f, 1f, t / 0.15f) : 1f;
        label.fontSize = baseSize * punch;

        Color color = baseColor;
        color.a = t < 0.6f ? baseColor.a : Mathf.Lerp(baseColor.a, 0f, (t - 0.6f) / 0.4f);
        label.color = color;
    }

    private void Release()
    {
        gameObject.SetActive(false);

        if (Pool.Count < MaxPoolSize)
        {
            Pool.Push(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Pool.Clear();
        poolRoot = null;
    }
}
