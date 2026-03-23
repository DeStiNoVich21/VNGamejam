using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Один элемент в списке инвентаря.
/// Показывает иконку, название, тег улики.
/// Drag из списка = стикер появляется на доске и начинает перетаскиваться.
/// </summary>
public class StickerListItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI tagText;
    [SerializeField] private Image tagColor;
    [SerializeField] private Image background;
    [SerializeField] private GameObject hoverHighlight;

    // Цвета тегов
    private static readonly System.Collections.Generic.Dictionary<StickerTag, Color> TAG_COLORS = new()
    {
        { StickerTag.Who,   new Color(0.9f, 0.5f, 0.3f) },
        { StickerTag.What,  new Color(0.3f, 0.6f, 0.9f) },
        { StickerTag.Where, new Color(0.4f, 0.8f, 0.4f) },
        { StickerTag.When,  new Color(0.8f, 0.8f, 0.3f) },
        { StickerTag.Why,   new Color(0.9f, 0.3f, 0.3f) },
        { StickerTag.How,   new Color(0.7f, 0.4f, 0.9f) },
        { StickerTag.Fact,  new Color(0.6f, 0.9f, 0.9f) },
    };

    private string stickerId;
    private StickerData data;

    // Призрак который тащится за курсором при drag
    private GameObject dragGhost;
    private Canvas rootCanvas;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (hoverHighlight) hoverHighlight.SetActive(false);
    }

    public void Initialize(StickerData d)
    {
        data = d;
        stickerId = d.stickerId;

        if (titleText) titleText.text = d.title;
        if (tagText) tagText.text = d.tag.ToString().ToUpper();
        if (icon && d.icon) icon.sprite = d.icon;

        if (tagColor && TAG_COLORS.TryGetValue(d.tag, out Color c))
            tagColor.color = c;
    }

    // ??? Hover ???????????????????????????????????????????????????????

    public void OnPointerEnter(PointerEventData e)
    {
        if (hoverHighlight) hoverHighlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (hoverHighlight) hoverHighlight.SetActive(false);
    }

    // ??? Drag из инвентаря на доску ??????????????????????????????????

    public void OnBeginDrag(PointerEventData e)
    {
        // Создаём призрак стикера который тянется за курсором
        dragGhost = new GameObject("DragGhost");
        dragGhost.transform.SetParent(rootCanvas.transform, false);
        dragGhost.transform.SetAsLastSibling();

        var rt = dragGhost.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(180, 120);

        var img = dragGhost.AddComponent<Image>();
        img.color = new Color(0.98f, 0.95f, 0.78f, 0.85f);

        // Текст на призраке
        var go = new GameObject("T");
        go.transform.SetParent(dragGhost.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = data?.title ?? "";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        var trt = go.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        // Блокируем raycast чтобы ghost не перехватывал события
        dragGhost.AddComponent<CanvasGroup>().blocksRaycasts = false;

        UpdateGhostPos(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        UpdateGhostPos(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (dragGhost != null) { Destroy(dragGhost); dragGhost = null; }

        // Проверяем куда отпустили
        // Если над SlotAnchorUI — TryPlaceInSlot
        // Если над BoardCanvas — просто добавляем на доску в эту позицию
        var raycast = e.pointerCurrentRaycast.gameObject;
        if (raycast == null) return;

        // Попали в слот
        var slot = raycast.GetComponentInParent<SlotAnchorUI>();
        if (slot != null)
        {
            InvestigationBoardUI.instance?.TryDropStickerInSlot(stickerId, data.tag);
            return;
        }

        // Попали на доску — добавляем в позицию курсора
        var boardUI = InvestigationBoardUI.instance;
        if (boardUI == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardUI.BoardCanvas, e.position, e.pressEventCamera, out Vector2 localPos);

        InvestigationBoardManager.instance.SetPosition(stickerId, localPos);
        // Стикер уже в инвентаре менеджера — UI подхватит через событие
    }

    private void UpdateGhostPos(Vector2 screenPos)
    {
        if (dragGhost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            screenPos, null, out Vector2 lp);
        dragGhost.GetComponent<RectTransform>().anchoredPosition = lp;
    }
}