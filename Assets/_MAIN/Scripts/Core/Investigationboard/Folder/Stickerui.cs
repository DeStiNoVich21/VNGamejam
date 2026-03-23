using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Один стикер на доске.
/// ЛКМ = детали. ПКМ = начать верёвку. Drag = перетащить.
/// Визуально: пожелтевшая бумажка с рукописным текстом.
/// Факты фантомов — цвет по фантому.
/// </summary>
public class StickerUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler,
    IDragHandler, IEndDragHandler, IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image tagBadge;
    [SerializeField] private TextMeshProUGUI tagText;
    [SerializeField] private Image phantomStripe; // цветная полоска для фактов

    // Цвета стикеров
    private static readonly Color COLOR_NORMAL = new Color(0.98f, 0.95f, 0.78f); // пожелтевшая бумага
    private static readonly Color COLOR_FACT = new Color(0.85f, 0.85f, 0.85f); // чуть серее
    private static readonly Dictionary<PhantomManager.PhantomType, Color> PHANTOM_COLORS = new()
    {
        { PhantomManager.PhantomType.Genesis,    new Color(0.4f,  0.65f, 1.0f)  },
        { PhantomManager.PhantomType.Melancholy, new Color(0.7f,  0.4f,  0.9f)  },
        { PhantomManager.PhantomType.Fury,       new Color(1.0f,  0.35f, 0.35f) },
        { PhantomManager.PhantomType.Stigma,     new Color(0.35f, 0.85f, 0.5f)  },
        { PhantomManager.PhantomType.Ego,        new Color(0.9f,  0.9f,  0.9f)  },
    };

    private string stickerId;
    private InvestigationBoardUI boardUI;
    private RectTransform rt;
    private Canvas rootCanvas;
    private bool isDragging = false;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(StickerData data, InvestigationBoardUI ui)
    {
        stickerId = data.stickerId;
        boardUI = ui;

        if (titleText) titleText.text = data.title;
        if (bodyText) bodyText.text = data.bodyText;
        if (tagText) tagText.text = data.tag.ToString().ToUpper();

        // Цвет фона
        if (background)
            background.color = data.isPhantomFact ? COLOR_FACT : COLOR_NORMAL;

        // Полоска цвета фантома
        if (phantomStripe)
        {
            phantomStripe.gameObject.SetActive(data.isPhantomFact);
            if (data.isPhantomFact &&
                PHANTOM_COLORS.TryGetValue(data.phantomSource, out Color c))
                phantomStripe.color = c;
        }
    }

    // ??? Клик ????????????????????????????????????????????????????????

    public void OnPointerClick(PointerEventData e)
    {
        if (isDragging) return;
        if (e.button == PointerEventData.InputButton.Left)
            boardUI.ShowDetail(stickerId);
    }

    // ??? Перетаскивание стикера ??????????????????????????????????????

    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Right)
        {
            boardUI.BeginRopeDrag(stickerId);
            return;
        }
        isDragging = true;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData e)
    {
        if (boardUI.IsDraggingRope)
        {
            // Передаём позицию курсора в UI
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                boardUI.BoardCanvas, e.position, null, out Vector2 lp);
            // BoardRopeUI обновляется в Update сам
            return;
        }

        if (!isDragging) return;
        rt.anchoredPosition += e.delta / rootCanvas.scaleFactor;
        InvestigationBoardManager.instance.SetPosition(stickerId, rt.anchoredPosition);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (boardUI.IsDraggingRope) { boardUI.EndRopeDrag(null); return; }
        isDragging = false;
    }

    public void OnPointerUp(PointerEventData e)
    {
        // Если тянули верёвку и отпустили над этим стикером
        if (boardUI.IsDraggingRope && boardUI.RopeSourceId != stickerId)
            boardUI.EndRopeDrag(stickerId);
    }

    // ??? Эффект отказа (нельзя положить в слот) ??????????????????????

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