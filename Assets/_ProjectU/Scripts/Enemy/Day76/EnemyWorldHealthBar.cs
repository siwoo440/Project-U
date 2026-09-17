using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 76일차: 적 머리 위 World Space 체력바 (Prefab 없이 코드로 생성)
[DisallowMultipleComponent]
public sealed class EnemyWorldHealthBar : MonoBehaviour
{
    private const float PixelsPerUnit = 100f;
    private static readonly Vector2 BarSize = new Vector2(120f, 14f);

    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color trailColor = new Color(1f, 0.9f, 0.6f, 0.9f);
    [SerializeField] private Color highHealthColor = new Color(0.35f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.2f, 0.15f, 1f);
    [SerializeField, Min(0f)] private float trailDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float trailSpeed = 1.2f;

    private Canvas canvas;
    private RectTransform fillRect;
    private RectTransform trailRect;
    private Image fillImage;
    private TextMeshProUGUI nameLabel;

    private float targetNormalized = 1f;
    private float trailNormalized = 1f;
    private float trailStartTime;

    public static EnemyWorldHealthBar Create(Transform owner, string displayName)
    {
        GameObject root = new GameObject("WorldHealthBar", typeof(RectTransform));
        root.transform.SetParent(owner, false);
        root.layer = owner.gameObject.layer;

        EnemyWorldHealthBar bar = root.AddComponent<EnemyWorldHealthBar>();
        bar.Build(displayName);
        return bar;
    }

    public void SetNormalized(float normalized, bool immediate)
    {
        float clamped = Mathf.Clamp01(normalized);

        if (clamped < targetNormalized && !immediate)
        {
            trailStartTime = Time.time + trailDelay;
        }

        targetNormalized = clamped;

        if (immediate || trailNormalized < targetNormalized)
        {
            trailNormalized = targetNormalized;
        }

        ApplyWidths();
    }

    public void SetVisible(bool isVisible)
    {
        if (canvas != null)
        {
            canvas.enabled = isVisible;
        }
    }

    public void UpdatePlacement(Vector3 worldPosition, Camera viewCamera)
    {
        transform.position = worldPosition;

        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        float unitScale = 1f / PixelsPerUnit;
        transform.localScale = new Vector3(
            unitScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
            unitScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)),
            unitScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.z)));

        if (viewCamera != null)
        {
            transform.rotation = viewCamera.transform.rotation;
        }
    }

    private void Update()
    {
        if (trailNormalized <= targetNormalized || Time.time < trailStartTime)
        {
            return;
        }

        trailNormalized = Mathf.MoveTowards(trailNormalized, targetNormalized, trailSpeed * Time.deltaTime);
        ApplyWidths();
    }

    private void Build(string displayName)
    {
        RectTransform rootRect = (RectTransform)transform;
        rootRect.sizeDelta = BarSize;
        transform.localScale = Vector3.one / PixelsPerUnit;

        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        CreateImage("Background", rootRect, backgroundColor, Vector2.zero, Vector2.one);
        RectTransform inner = CreateRect("Inner", rootRect);
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(2f, 2f);
        inner.offsetMax = new Vector2(-2f, -2f);

        trailRect = CreateImage("Trail", inner, trailColor, Vector2.zero, Vector2.one).rectTransform;
        fillImage = CreateImage("Fill", inner, highHealthColor, Vector2.zero, Vector2.one);
        fillRect = fillImage.rectTransform;

        if (!string.IsNullOrEmpty(displayName))
        {
            RectTransform labelRect = CreateRect("Name", rootRect);
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(80f, 22f);
            labelRect.anchoredPosition = new Vector2(0f, 2f);

            nameLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            nameLabel.text = displayName;
            nameLabel.fontSize = 16f;
            nameLabel.alignment = TextAlignmentOptions.Bottom;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nameLabel.overflowMode = TextOverflowModes.Overflow;
            nameLabel.raycastTarget = false;
            nameLabel.outlineWidth = 0.2f;
            nameLabel.outlineColor = new Color32(0, 0, 0, 255);
        }

        ApplyWidths();
    }

    private void ApplyWidths()
    {
        if (fillRect == null)
        {
            return;
        }

        fillRect.anchorMax = new Vector2(targetNormalized, 1f);
        trailRect.anchorMax = new Vector2(trailNormalized, 1f);
        fillImage.color = Color.Lerp(lowHealthColor, highHealthColor, Mathf.InverseLerp(0.2f, 0.7f, targetNormalized));
    }

    private static RectTransform CreateRect(string objectName, RectTransform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static Image CreateImage(string objectName, RectTransform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = CreateRect(objectName, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
