using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Навигация по доске как в Канве / Miro.
/// - Drag по пустому месту (ЛКМ на фоне) = панорамирование
/// - Колёсико = зум к курсору
/// - Вешается на Background (Image который покрывает весь экран доски)
/// </summary>
public class BoardNavigator : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
{
    [Header("Целевой трансформ (BoardCanvas)")]
    [SerializeField] private RectTransform boardCanvas;

    [Header("Настройки зума")]
    [SerializeField] private float zoomMin = 0.25f;
    [SerializeField] private float zoomMax = 2.0f;
    [SerializeField] private float zoomStep = 0.08f;
    [SerializeField] private float zoomSmooth = 8f;

    [Header("Настройки пана")]
    [SerializeField] private float panSpeed = 1f;

    private float targetZoom;
    private float currentZoom = 1f;
    private bool isPanning = false;
    private Vector2 lastMousePos;
    private Canvas rootCanvas;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        targetZoom = currentZoom;
    }

    private void Update()
    {
        // Плавный зум
        if (!Mathf.Approximately(currentZoom, targetZoom))
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, zoomSmooth * Time.deltaTime);
            boardCanvas.localScale = Vector3.one * currentZoom;
        }
    }

    // ??? Pan (ЛКМ по фону) ???????????????????????????????????????????

    public void OnPointerDown(PointerEventData e)
    {
        // Начинаем пан только если кликнули именно по фону
        // (стикеры перехватывают клик раньше)
        if (e.button == PointerEventData.InputButton.Left)
        {
            isPanning = true;
            lastMousePos = e.position;
        }
    }

    public void OnDrag(PointerEventData e)
    {
        if (!isPanning) return;

        Vector2 delta = e.position - lastMousePos;
        lastMousePos = e.position;

        // Компенсируем масштаб Canvas и зум доски
        float scale = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
        boardCanvas.anchoredPosition += (delta / scale) * panSpeed;
    }

    public void OnPointerUp(PointerEventData e)
    {
        isPanning = false;
    }

    // ??? Зум к курсору ???????????????????????????????????????????????

    public void OnScroll(PointerEventData e)
    {
        float direction = e.scrollDelta.y > 0 ? 1f : -1f;
        float newZoom = Mathf.Clamp(targetZoom + direction * zoomStep, zoomMin, zoomMax);

        if (Mathf.Approximately(newZoom, targetZoom)) return;

        // Зум к позиции курсора — сдвигаем boardCanvas чтобы точка под курсором не уехала
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardCanvas.parent as RectTransform,
            e.position,
            e.pressEventCamera,
            out Vector2 localCursor
        );

        Vector2 canvasPos = boardCanvas.anchoredPosition;
        float ratio = newZoom / currentZoom;

        // Новая позиция чтобы курсор остался на том же месте
        boardCanvas.anchoredPosition = localCursor - (localCursor - canvasPos) * ratio;

        targetZoom = newZoom;
    }

    // ??? Публичный API ???????????????????????????????????????????????

    public void ResetView()
    {
        boardCanvas.anchoredPosition = Vector2.zero;
        targetZoom = 1f;
    }

    public void ZoomIn() => targetZoom = Mathf.Clamp(targetZoom + zoomStep * 3, zoomMin, zoomMax);
    public void ZoomOut() => targetZoom = Mathf.Clamp(targetZoom - zoomStep * 3, zoomMin, zoomMax);
}