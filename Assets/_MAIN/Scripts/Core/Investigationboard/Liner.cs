using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMultilineConnector : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private RectTransform centerObject;
    [SerializeField] private Sprite lineSprite;
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private float thickness = 4f;

    [SerializeField] private List<RectTransform> targets = new List<RectTransform>();
    private Dictionary<RectTransform, RectTransform> lines = new Dictionary<RectTransform, RectTransform>();

    private void Update()
    {
        if (centerObject == null) return;
        CleanupLines();

        foreach (var target in targets)
        {
            if (target == null) continue;
            RectTransform line = GetLine(target);
            DrawLineBetweenCenters(line, centerObject, target);
        }
    }

    private void DrawLineBetweenCenters(RectTransform line, RectTransform start, RectTransform end)
    {
        Vector3 startPos = start.TransformPoint(start.rect.center);
        Vector3 endPos = end.TransformPoint(end.rect.center);

        Vector2 localStart = transform.InverseTransformPoint(startPos);
        Vector2 localEnd = transform.InverseTransformPoint(endPos);

        Vector2 dir = localEnd - localStart;
        line.anchoredPosition = localStart;
        line.sizeDelta = new Vector2(dir.magnitude, thickness);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    private RectTransform GetLine(RectTransform target)
    {
        if (lines.ContainsKey(target)) return lines[target];

        GameObject obj = new GameObject("Connection", typeof(Image));
        obj.transform.SetParent(this.transform, false);
        obj.transform.SetAsLastSibling();
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        Image img = obj.GetComponent<Image>();
        img.sprite = lineSprite;
        img.color = lineColor;
        img.raycastTarget = false;

        lines.Add(target, rt);
        return rt;
    }

    public void AddTarget(RectTransform t)
    {
        if (t == null) return;
        if (!targets.Contains(t))
        {
            targets.Add(t);
        }
    }

    public void RemoveTarget(RectTransform t)
    {
        if (targets.Contains(t)) targets.Remove(t);
        if (lines.ContainsKey(t))
        {
            if (lines[t] != null) Destroy(lines[t].gameObject);
            lines.Remove(t);
        }
    }

    private void CleanupLines()
    {
        List<RectTransform> keys = new List<RectTransform>(lines.Keys);
        foreach (var key in keys)
        {
            if (key == null || !targets.Contains(key))
            {
                if (lines[key] != null) Destroy(lines[key].gameObject);
                lines.Remove(key);
            }
        }
    }
}