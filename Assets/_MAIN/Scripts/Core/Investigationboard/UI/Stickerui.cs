using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Стикер на доске И в инвентаре.
/// ЛКМ = детали. ПКМ = убрать со слота и в инвентарь или верёвка.
/// Drag ЛКМ = перемещение по доске. 
/// Drag ПКМ = верёвка между стикерами.
/// </summary>
public class StickerUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image tagBadge;
    [SerializeField] private TextMeshProUGUI tagText;
    [SerializeField] private Image phantomStripe;
    [SerializeField] private Image icon;
    [SerializeField] private Image tagColor;
    [SerializeField] private GameObject hoverHighlight;

    [Header("Настройки")]
    [SerializeField] private float slotDetachDistance = 150f;

    [Header("Настройки цветов (Odin)")]
    [SerializeField] private Color colorNormal = new Color(0.98f, 0.95f, 0.78f);
    [SerializeField] private Color colorFact = new Color(0.85f, 0.85f, 0.85f);

    [InfoBox("Здесь можно настроить цвета для типов Фантомов и Тегов")]
    [SerializeField, ShowInInspector]
    private Dictionary<PhantomManager.PhantomType, Color> phantomColors = new()
    {
        { PhantomManager.PhantomType.Dominion,    new Color(0.4f,  0.65f, 1.0f)  },
        { PhantomManager.PhantomType.Zenith, new Color(0.7f,  0.4f,  0.9f)  },
        { PhantomManager.PhantomType.Stigma,     new Color(0.35f, 0.85f, 0.5f)  }
    };

    [SerializeField, ShowInInspector]
    private Dictionary<StickerTag, Color> tagColors = new()
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
    private InvestigationBoardUI boardUI;
    private RectTransform rt;
    private Canvas rootCanvas;
    private bool isDragging = false;
    private Vector2 dragStartPos;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>(); // Добавьте это
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (hoverHighlight) hoverHighlight.SetActive(false);
    }

    public void Initialize(StickerData d, InvestigationBoardUI ui = null)
    {
        data = d;
        stickerId = d.stickerId;
        boardUI = ui;

        if (titleText) titleText.text = d.title;
        if (bodyText) bodyText.text = d.bodyText;
        if (tagText) tagText.text = d.tag.ToString().ToUpper();

        if (icon && d.icon)
            icon.sprite = d.icon;

        // ИСПОЛЬЗУЕМ НОВЫЕ ПЕРЕМЕННЫЕ
        if (background)
            background.color = d.isPhantomFact ? colorFact : colorNormal;

        if (tagColor && tagColors.TryGetValue(d.tag, out Color tagCol))
            tagColor.color = tagCol;

        if (phantomStripe)
        {
            phantomStripe.gameObject.SetActive(d.isPhantomFact);
            if (d.isPhantomFact &&
                phantomColors.TryGetValue(d.phantomSource, out Color c))
                phantomStripe.color = c;
        }
    }

    // ??? Hover ??????????????????????????????????????????????????????????

    public void OnPointerEnter(PointerEventData e)
    {
        if (hoverHighlight) hoverHighlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (hoverHighlight) hoverHighlight.SetActive(false);
    }

    // ??? Клик ??????????????????????????????????????????????????????????

    public void OnPointerClick(PointerEventData e)
    {
        if (isDragging) return;
        
        // ЛКМ — показать детали
        if (e.button == PointerEventData.InputButton.Left && boardUI != null)
            boardUI.ShowDetail(stickerId);
        
        // ПКМ — убрать с доски и полностью удалить из системы
        if (e.button == PointerEventData.InputButton.Right)
        {
            var mgr = InvestigationBoardManager.instance;
            if (mgr == null) return;

            // Вариант 1: Если просто вернуть в инвентарь (стикер остаётся в системе)
            var currentSlot = mgr.GetStickerSlot(stickerId);
            if (currentSlot.HasValue)
            {// Перед тем как удалиться, стикер чистит линию в слоте
                if (boardUI.TryGetSlotAnchor(currentSlot.Value.ToString(), out var targetAnchor))
                {
                    if (targetAnchor.lineConnector != null)
                        targetAnchor.lineConnector.RemoveTarget(this.rt);
                }
                mgr.UnplaceFromSlots(stickerId);
                Debug.Log($"[Board] Стикер '{stickerId}' удалён из слота {currentSlot.Value}");
            }
            else
            {
                // Если стикер просто на доске без слота — удаляем его из инвентаря тоже
                mgr.RemoveSticker(stickerId);
                Debug.Log($"[Board] Стикер '{stickerId}' полностью удалён из инвентаря");
            }

            // Удаляем визуал со слоя стикеров
            if (boardUI != null)
                boardUI.RemoveStickerFromBoard(stickerId);

            Debug.Log($"[Board] Стикер '{stickerId}' возвращён в инвентарь и удалён с доски");
        }
    }

    // ??? Перетаскивание ??????????????????????????????????????????????

    public void OnBeginDrag(PointerEventData e)
    {
        // ПКМ + драг = верёвка
        if (e.button == PointerEventData.InputButton.Right)
        {
            if (boardUI != null)
                boardUI.BeginRopeDrag(stickerId);
            return;
        }

        // ЛКМ + драг = движение по доске
        if (e.button != PointerEventData.InputButton.Left) return;

        isDragging = true;
        canvasGroup.blocksRaycasts = false; // СТИКЕР ТЕПЕРЬ "ПРОЗРАЧЕН" ДЛЯ ЛУЧЕЙ
        dragStartPos = rt.anchoredPosition;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (boardUI != null && boardUI.IsDraggingRope) return;
        if (!isDragging) return;

        // 1. Перемещение
        rt.anchoredPosition += e.delta / rootCanvas.scaleFactor;
        InvestigationBoardManager.instance.SetPosition(stickerId, rt.anchoredPosition);

        // 2. Логика открепления
        var mgr = InvestigationBoardManager.instance;
        var currentSlot = mgr.GetStickerSlot(stickerId);

        // Если стикер СЕЙЧАС привязан к слоту (currentSlot не null)
        if (currentSlot.HasValue)
        {
            string slotKey = currentSlot.Value.ToString();

            if (boardUI.TryGetSlotAnchor(slotKey, out var targetAnchor))
            {
                float distance = Vector2.Distance(rt.anchoredPosition, targetAnchor.GetComponent<RectTransform>().anchoredPosition);

                // Если отошли слишком далеко
                if (distance > slotDetachDistance)
                {
                    // УДАЛЯЕМ линию из коннектора
                    if (targetAnchor.lineConnector != null)
                    {
                        targetAnchor.lineConnector.RemoveTarget(this.rt);
                    }

                    // УДАЛЯЕМ логическую привязку в менеджере
                    mgr.UnplaceFromSlots(stickerId);

                    Debug.Log($"[Board] Стикер '{stickerId}' оторван от слота {slotKey} (дистанция {distance})");
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (isDragging)
        {
            canvasGroup.blocksRaycasts = true;
            var raycastResult = e.pointerCurrentRaycast.gameObject;
            var foundSlot = raycastResult != null ? raycastResult.GetComponentInParent<SlotAnchorUI>() : null;

            // Проверяем радиус через метод слота
            if (foundSlot != null && foundSlot.IsPositionInDropZone(e.position))
            {
                // Логика привязки в менеджере
                boardUI.TryDropStickerInSlot(stickerId, data.tag);

                // ВИЗУАЛ: StickerUI сам добавляет себя в коннектор слота
                if (foundSlot.lineConnector != null)
                {
                    foundSlot.lineConnector.AddTarget(this.rt);
                    Debug.Log($"[Sticker] Я сам добавился в линию слота {foundSlot.config.tag}");
                }
            }

            isDragging = false;
        }
    }
    public void OnPointerUp(PointerEventData e)
    {
        // Если тянули верёвку и отпустили над этим стикером
        if (boardUI != null && boardUI.IsDraggingRope && boardUI.RopeSourceId != stickerId)
            boardUI.EndRopeDrag(stickerId);
    }

    // ??? Эффект отказа ??????????????????????????????????????????????

    public void PlayRejectEffect() => StartCoroutine(RejectShake());

    private IEnumerator RejectShake()
    {
        Vector2 origin = rt.anchoredPosition;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = origin +
                new Vector2(Mathf.Sin(t * 50f) * 6f * (1f - t / 0.3f), 0);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }
}