using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Всплывающее окно при получении новой мысли.
/// Появляется поверх доски, показывает иконку + текст мысли.
/// </summary>
public class ThoughtPopupUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image thoughtImage;
    [SerializeField] private TextMeshProUGUI thoughtTitle;
    [SerializeField] private TextMeshProUGUI thoughtText;
    [SerializeField] private float showDuration = 4f;
    [SerializeField] private float fadeSpeed = 3f;

    public void Show(ClueCombination combo)
    {
        if (thoughtImage != null) thoughtImage.sprite = combo.thoughtImage;
        if (thoughtTitle != null) thoughtTitle.text = combo.thoughtTitle;
        if (thoughtText != null) thoughtText.text = combo.thoughtText;

        StartCoroutine(ShowAndFade());
    }

    private IEnumerator ShowAndFade()
    {
        // Появляемся
        canvasGroup.alpha = 0f;
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(showDuration);

        // Исчезаем
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }
}