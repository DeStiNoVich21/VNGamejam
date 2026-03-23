using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// Карточка улики на доске.
/// Поддерживает: перетаскивание, клик для деталей, начало/конец верёвки,
/// подсветку магнетизма, эффект притяжения/отталкивания.
/// </summary>
public class ClueCardUI : MonoBehaviour,
    IPointerClickHandler, IPointerDownHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI элементы")]
    [SerializeField] private Image background;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private Image tierStripe;
    [SerializeField] private GameObject magnetGlow;        // подсветка при магнетизме
    [SerializeField] private GameObject ropeAnchor;        // кнопка начала верёвки (маленький кружок)

    private string clueId;
    private SynthesisBoardUI boardUI;
    private RectTransform rectTransform;
    private Canvas rootCanvas;

    private bool isDraggingCard = false;
    private bool isHovered = false;
    private Coroutine co_magnetEffect = null;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(ClueData clue, SynthesisBoardUI ui)
    {
        clueId = clue.id;
        boardUI = ui;

        if (cardImage != null) cardImage.sprite = clue.image;
        if (titleText != null) titleText.text = clue.title;
        if (tierText != null) tierText.text = clue.tier.ToString();

        if (magnetGlow != null) magnetGlow.SetActive(false);
    }

    public void SetTierColor(Color color)
    {
        if (tierStripe != null) tierStripe.color = color;
    }

    // ??? Перетаскивание карточки ?????????????????????????????????????

    public void OnBeginDrag(PointerEventData e)
    {
        // Если зажат правый кнопка или shift — начинаем верёвку, не карточку
        if (e.button == PointerEventData.InputButton.Right)
        {
            boardUI.BeginRopeDrag(clueId);
            return;
        }
        isDraggingCard = true;
        transform.SetAsLastSibling(); // поверх других карточек
    }

    public void OnDrag(PointerEventData e)
    {
        if (boardUI.IsDraggingRope)
        {
            boardUI.UpdateRopeDrag(e.position);
            return;
        }

        if (!isDraggingCard) return;

        // Двигаем карточку
        rectTransform.anchoredPosition +=
            e.delta / rootCanvas.scaleFactor;

        // Сохраняем позицию в менеджере
        SynthesisBoardManager.instance.SetCardPosition(
            clueId, rectTransform.anchoredPosition);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (boardUI.IsDraggingRope)
        {
            boardUI.EndRopeDrag(null); // отпустили в пустоту
            return;
        }
        isDraggingCard = false;
    }

    // ??? Клик — открыть детали ???????????????????????????????????????

    public void OnPointerClick(PointerEventData e)
    {
        if (isDraggingCard) return;
        if (e.button == PointerEventData.InputButton.Left)
            boardUI.ShowDetail(clueId);
    }

    // ??? Hover — принять верёвку ?????????????????????????????????????

    public void OnPointerEnter(PointerEventData e)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData e)
    {
        isHovered = false;

        // Если тянем верёвку и вышли из карточки — сбрасываем цель
        if (boardUI.IsDraggingRope && boardUI.RopeSourceId != clueId)
            SetMagnetHighlight(false);
    }

    public void OnPointerDown(PointerEventData e)
    {
        // Если верёвку отпустили над этой карточкой
        if (boardUI.IsDraggingRope && boardUI.RopeSourceId != clueId)
            boardUI.EndRopeDrag(clueId);
    }

    // ??? Подсветка магнетизма ????????????????????????????????????????

    public void SetMagnetHighlight(bool active)
    {
        if (magnetGlow != null) magnetGlow.SetActive(active);
    }

    // ??? Эффект притяжения/отталкивания ?????????????????????????????

    public void PlayMagnetEffect(bool attract)
    {
        if (co_magnetEffect != null) StopCoroutine(co_magnetEffect);
        co_magnetEffect = StartCoroutine(MagnetEffect(attract));
    }

    private IEnumerator MagnetEffect(bool attract)
    {
        Vector2 origin = rectTransform.anchoredPosition;
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (attract)
            {
                // Небольшая вибрация "притяжения" — пульс масштаба
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 6) * 0.05f;
                transform.localScale = Vector3.one * scale;
            }
            else
            {
                // Отталкивание — небольшой сдвиг в сторону
                float offset = Mathf.Sin(t * Mathf.PI * 8) * 6f * (1f - t);
                rectTransform.anchoredPosition = origin + new Vector2(offset, 0);
            }
            yield return null;
        }

        transform.localScale = Vector3.one;
        rectTransform.anchoredPosition = origin;
        co_magnetEffect = null;
    }
}