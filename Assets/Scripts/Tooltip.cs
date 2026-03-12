using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class Tooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private Vector2 padding = new Vector2(10, 10); // отступ вокруг текста
    [SerializeField] private Vector2 offset = new Vector2(150, 20); // смещение от курсора
    public RectTransform rectTransform;
    public Canvas canvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // скрыть по умолчанию
    }

    /// <summary>
    /// Устанавливает текст и автоматически меняет размер тултипа
    /// </summary>
    public void SetText(string value)
    {
        text.text = value;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform); // пересчитать размеры
        rectTransform.sizeDelta = new Vector2(
            text.preferredWidth + padding.x,
            text.preferredHeight + padding.y
        );
    }

    /// <summary>
    /// Показывает тултип рядом с курсором
    /// </summary>
    public void Show(Vector2 screenPosition)
    {
        canvasGroup.alpha = 1f;
        UpdatePosition(screenPosition);
    }

    /// <summary>
    /// Скрывает тултип
    /// </summary>
    public void Hide()
    {
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Обновляет позицию тултипа (лучше вызывать каждый кадр, если мышь движется)
    /// </summary>
    public void UpdatePosition(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition + offset,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        // Ограничение по экрану (чтобы не уходил за края)
        Vector2 clamped = localPoint;
        Vector2 canvasSize = (canvas.transform as RectTransform).sizeDelta;
        clamped.x = Mathf.Clamp(clamped.x, -canvasSize.x / 2 + rectTransform.sizeDelta.x / 2, canvasSize.x / 2 - rectTransform.sizeDelta.x / 2);
        clamped.y = Mathf.Clamp(clamped.y, -canvasSize.y / 2 + rectTransform.sizeDelta.y / 2, canvasSize.y / 2 - rectTransform.sizeDelta.y / 2);

        rectTransform.localPosition = clamped;
    }
}



[System.Serializable]
public class TooltipManager
{
    [SerializeField] Tooltip prefab;
    Tooltip current;
    public void Show(string text, Vector2 screenPos, Canvas canvas)
    {
        if (!current)
        {
            current = Object.Instantiate(prefab,screenPos,Quaternion.identity);
            current.rectTransform.SetParent(canvas.transform);
            current.rectTransform.localScale = new Vector3(1, 1, 1);
            current.canvas = canvas;
        }
        current.SetText(text);
        current.Show(screenPos);
    }

    public void Hide()
    {
        if (current)
            current.Hide();
    }
}
