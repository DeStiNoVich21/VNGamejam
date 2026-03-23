using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Попап результата Синтеза — показывает итоговый Summary кейса.
/// </summary>
public class SynthesisPopupUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private Button closeButton;
    [SerializeField] private float fadeSpeed = 4f;

    private void Awake()
    {
        if (cg) cg.alpha = 0f;
        if (closeButton) closeButton.onClick.AddListener(() => StartCoroutine(FadeOut()));
    }

    public void Show(CaseConclusion conclusion)
    {
        if (titleText) titleText.text = conclusion.title;
        if (summaryText) summaryText.text = conclusion.summaryText;
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        while (cg.alpha < 1f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 1f, fadeSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator FadeOut()
    {
        while (cg.alpha > 0f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, 0f, fadeSpeed * Time.deltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }
}