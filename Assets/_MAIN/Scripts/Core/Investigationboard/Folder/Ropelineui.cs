using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Верёвка между двумя карточками.
/// Использует UI Line через несколько Image-сегментов или простой LineRenderer.
/// Цвет: красный = нет комбинации, зелёный = есть комбинация.
/// </summary>
public class RopeLineUI : MonoBehaviour
{
    [SerializeField] private Image lineImage;
    [SerializeField] private Color colorValid = new Color(0.4f, 0.9f, 0.4f, 0.85f);
    [SerializeField] private Color colorInvalid = new Color(0.85f, 0.3f, 0.3f, 0.7f);
    [SerializeField] private Color colorDraft = new Color(1f, 1f, 1f, 0.5f);

    private RectTransform from;
    private RectTransform to;
    private bool isDraft = false;
    private Vector2 draftEnd;

    private RectTransform rt;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (isDraft)
            DrawLine(from.anchoredPosition, draftEnd);
        else if (from != null && to != null)
            DrawLine(from.anchoredPosition, to.anchoredPosition);
    }

    // Финальная верёвка между двумя карточками
    public void Initialize(RectTransform fromCard, RectTransform toCard, bool isValid)
    {
        from = fromCard;
        to = toCard;
        isDraft = false;
        lineImage.color = isValid ? colorValid : colorInvalid;
    }

    // Черновая верёвка при перетаскивании
    public void InitializeDraft(RectTransform fromCard)
    {
        from = fromCard;
        isDraft = true;
        draftEnd = fromCard.anchoredPosition;
        lineImage.color = colorDraft;
    }

    public void UpdateDraftEnd(Vector2 pos) => draftEnd = pos;

    // Рисуем линию через RectTransform (повернуть и растянуть Image)
    private void DrawLine(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = start;
        rt.sizeDelta = new Vector2(length, 4f); // толщина 4px
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        rt.pivot = new Vector2(0f, 0.5f);
    }

    public void SetValid(bool valid)
    {
        lineImage.color = valid ? colorValid : colorInvalid;
    }
}