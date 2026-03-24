using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StickerListItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI элементы списка")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI tagText;
    [SerializeField] private Image tagColor;
    [SerializeField] private Image background;
    [SerializeField] private GameObject hoverHighlight;

    [Header("Префаб призрака")]
    [InfoBox("Сюда нужно перетащить префаб StickerUI или его упрощенную версию")]
    [SerializeField] private GameObject ghostPrefab;

    // Цвета тегов (оставляем как есть)
    [SerializeField, ShowInInspector]
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

    // --- Hover ---
    public void OnPointerEnter(PointerEventData e) => hoverHighlight?.SetActive(true);
    public void OnPointerExit(PointerEventData e) => hoverHighlight?.SetActive(false);

    // --- Drag ---
    public void OnBeginDrag(PointerEventData e)
    {
        if (ghostPrefab == null)
        {
            Debug.LogError("Не назначен Ghost Prefab в StickerListItemUI!");
            return;
        }

        // 1. Спавним префаб вместо создания "пустышки"
        dragGhost = Instantiate(ghostPrefab, rootCanvas.transform);
        dragGhost.transform.SetAsLastSibling();

        // 2. Инициализируем его данными (если на нем есть скрипт StickerUI)
        var ghostStickerScript = dragGhost.GetComponent<StickerUI>();
        if (ghostStickerScript != null)
        {
            ghostStickerScript.Initialize(data);
        }

        // 3. Отключаем Raycast, чтобы мышь видела доску сквозь призрака
        var cg = dragGhost.GetComponent<CanvasGroup>();
        if (cg == null) cg = dragGhost.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.7f; // Делаем его слегка прозрачным для эффекта

        UpdateGhostPos(e.position);
    }

    public void OnDrag(PointerEventData e)
    {
        UpdateGhostPos(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (dragGhost != null) { Destroy(dragGhost); dragGhost = null; }

        var raycast = e.pointerCurrentRaycast.gameObject;
        if (raycast == null) return;

        var boardUI = InvestigationBoardUI.instance;
        if (boardUI == null) return;

        // Попали в слот
        var slot = raycast.GetComponentInParent<SlotAnchorUI>();
        if (slot != null)
        {
            boardUI.TryDropStickerInSlot(stickerId, data.tag);
            return;
        }

        // Попали на доску
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardUI.BoardCanvas, e.position, e.pressEventCamera, out Vector2 localPos);

        InvestigationBoardManager.instance.SetPosition(stickerId, localPos);
        boardUI.SpawnStickerOnBoard(data);
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