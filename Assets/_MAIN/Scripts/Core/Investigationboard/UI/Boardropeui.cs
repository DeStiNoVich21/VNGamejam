using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Верёвка между двумя стикерами на доске.
/// Обновляется каждый кадр — следует за карточками при перемещении.
/// </summary>
public class BoardRopeUI : MonoBehaviour
{
    [SerializeField] private Image lineImg;
    [SerializeField] private Color colorNormal = new Color(0.6f, 0.4f, 0.2f, 0.85f); // верёвка
    [SerializeField] private Color colorDraft = new Color(1f, 1f, 1f, 0.5f);

    private RectTransform from;
    private RectTransform to;
    private bool isDraft = false;
    private Vector2 draftEnd;
    private RectTransform rt;

    private void Awake() => rt = GetComponent<RectTransform>();

    private void LateUpdate()
    {
        if (isDraft)
            Draw(from.anchoredPosition, draftEnd);
        else if (from != null && to != null)
            Draw(from.anchoredPosition, to.anchoredPosition);
    }

    public void Initialize(RectTransform a, RectTransform b)
    {
        from = a; to = b; isDraft = false;
        if (lineImg) lineImg.color = colorNormal;
    }

    public void InitializeDraft(RectTransform a)
    {
        from = a; isDraft = true; draftEnd = a.anchoredPosition;
        if (lineImg) lineImg.color = colorDraft;
    }

    public void SetEnd(Vector2 pos) => draftEnd = pos;

    private void Draw(Vector2 start, Vector2 end)
    {
        Vector2 dir = end - start;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = start;
        rt.sizeDelta = new Vector2(length, 3f);
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        rt.pivot = new Vector2(0f, 0.5f);
    }
}