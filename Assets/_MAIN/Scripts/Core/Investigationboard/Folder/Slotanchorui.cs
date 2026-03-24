using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Один слот из 5W+H на доске.
/// Может одновременно вмещать несколько улик в зависимости от конфига.
/// Визуально: цветной квадрат с вопросом и отображением уликов.
/// Принимает улики в пределах своего радиуса при сбросе.
/// </summary>
// Измени заголовок класса
public class SlotAnchorUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image background;
    [SerializeField] private GameObject filledIndicator;
    [SerializeField] private Transform stickersContainer;
    [SerializeField] private GameObject stickerDisplayPrefab;
    public UIMultilineConnector lineConnector;

    // --- НОВОЕ: Настройки радиуса ---
    [Header("Радиус зоны сброса")]
    [SerializeField] private float dropRadius = 150f;
    [SerializeField] private bool showDropZoneDebug = true;
    [SerializeField] private Color dropZoneColor = new Color(0.3f, 0.8f, 0.3f, 0.2f);

    public SlotConfig config;
    private InvestigationBoardUI boardUI;
    private RectTransform rectTransform;
    public Dictionary<string, GameObject> displayedStickers = new();

    // Цвета слотов
    private static readonly Color COLOR_EMPTY = new Color(0.15f, 0.15f, 0.15f, 0.7f);
    private static readonly Color COLOR_FILLED = new Color(0.2f, 0.35f, 0.2f, 0.8f);
    private static readonly Color COLOR_WHY = new Color(0.35f, 0.15f, 0.15f, 0.8f);

    [Header("Эффекты")]
    [SerializeField] private float hoverScaleMultiplier = 1.1f; // На сколько увеличится слот
    private Vector3 originalScale;

    // В Awake:
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
    }

    // Реализация наведения
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Проверяем, тащит ли игрок что-то
        if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<StickerUI>() != null)
        {
            // Подсвечиваем слот (например, делаем ярче или увеличиваем)
            if (background) background.color += new Color(0.1f, 0.1f, 0.1f, 0f);
            transform.localScale = originalScale * hoverScaleMultiplier;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Возвращаем как было
        transform.localScale = originalScale;
        UpdateCount(); // Сбросит цвет к стандартному
    }
    public void Initialize(SlotConfig cfg, InvestigationBoardUI ui)
    {
        config = cfg;
        boardUI = ui;

        if (questionText) questionText.text = cfg.question;

        if (background)
            background.color = cfg.tag == StickerTag.Why ? COLOR_WHY : COLOR_EMPTY;

        UpdateCount();
    }

    public bool IsPositionInDropZone(Vector2 screenPosition)
    {
        // Переводим экранную позицию мыши в локальную позицию внутри RectTransform слота
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            screenPosition,
            null,
            out Vector2 localPoint
        );

        // Проверяем расстояние от центра (0,0 в локальных координатах) до точки касания
        return localPoint.magnitude <= dropRadius;
    }

    // --- НОВОЕ: Получить центр слота в мировых координатах ---
    public Vector2 GetSlotWorldCenter()
    {
        return rectTransform != null ? (Vector2)rectTransform.position : Vector2.zero;
    }

    // --- НОВОЕ: Получить радиус зоны сброса ---
    public float GetDropRadius()
    {
        return dropRadius;
    }

    public void OnDrop(PointerEventData e)
    {
        StickerUI sticker = e.pointerDrag?.GetComponent<StickerUI>();
        if (sticker == null) return;

        var field = typeof(StickerUI).GetField("stickerId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        string id = field?.GetValue(sticker) as string;
        if (string.IsNullOrEmpty(id)) return;

        // --- НОВОЕ: Проверяем находится ли позиция сброса в радиусе ---
        if (!IsPositionInDropZone(e.position))
        {
            Debug.Log($"[Board] Позиция сброса вне радиуса слота {config.tag}");
            return;
        }

        boardUI.TryDropStickerInSlot(id, config.tag);
        UpdateCount();
    }

    private void UpdateCount()
    {
        if (!countText) return;
        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;

        int count = mgr.GetSlotContents(config.tag).Count;
        int max = config.maxStickers;

        countText.text = max > 0 ? $"{count}/{max}" : $"{count}";

        bool filled = count > 0;
        if (background) background.color = filled ? COLOR_FILLED : COLOR_EMPTY;
        if (filledIndicator) filledIndicator.SetActive(filled);
        
        // --- НОВОЕ: обновляем отображение улик в слоте ---
        RefreshDisplayedStickers();
    }

    // --- НОВОЕ: отображение улик в слоте ---
    private void RefreshDisplayedStickers()
    {
        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;

        // Очищаем только визуальные иконки ВНУТРИ слота
        foreach (var go in displayedStickers.Values)
            Destroy(go);
        displayedStickers.Clear();

        if (stickersContainer == null) return;

        var stickerIds = mgr.GetSlotContents(config.tag);

        foreach (var id in stickerIds)
        {
            var stickerData = mgr.GetStickerById(id);
            if (stickerData == null) continue;

            GameObject display = Instantiate(stickerDisplayPrefab, stickersContainer);

            // Настройка иконки и текста внутри слота...
            var tmp = display.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) tmp.text = stickerData.title;

            var img = display.GetComponent<Image>();
            if (img && stickerData.icon)
                img.sprite = stickerData.icon;

            displayedStickers[id] = display;

            // КОРРЕКЦИЯ: Мы БОЛЬШЕ НЕ вызываем lineConnector.AddTarget здесь.
            // Линией управляет сам объект StickerUI на доске.
        }
    }

    // --- НОВОЕ: Отрисовка зоны сброса в редакторе и рантайме ---
    private void OnDrawGizmosSelected()
    {
        if (!showDropZoneDebug || rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return;
        }

        // Рисуем круг радиуса зоны сброса
        Gizmos.color = dropZoneColor;
        DrawCircle(rectTransform.position, dropRadius, 32);
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    // --- поддержка получения тега слота текущего объекта ---
    public StickerTag Tag => config != null ? config.tag : StickerTag.Why;

    public void RefreshCount() => UpdateCount();
}