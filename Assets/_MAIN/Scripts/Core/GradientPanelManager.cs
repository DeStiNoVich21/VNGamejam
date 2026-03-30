using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Sirenix.OdinInspector;

/// <summary>
/// Управляет панелью градиента для визуализации выбора фантома.
/// Показывает градиент с иконкой фантома и плавно делает его прозрачным.
/// </summary>
public class GradientPanelManager : MonoBehaviour
{
    public static GradientPanelManager instance { get; private set; }

    [Title("UI Elements")]
    [SerializeField, Required]
    private CanvasGroup canvasGroup;

    [SerializeField, Required]
    private Image gradientImage;

    [SerializeField, Required]
    private Image phantomIcon;

    [Title("Settings")]
    [SerializeField, Range(0.1f, 5f)]
    private float fadeInDuration = 0.3f;

    [SerializeField, Range(0.5f, 10f)]
    private float fadeOutDuration = 2f;

    [SerializeField, Range(0f, 2f)]
    private float holdDuration = 0.5f;

    [Title("Phantom Sprites")]
    [InfoBox("Спрайты градиентов и иконок для каждого фантома")]
    [SerializeField] private PhantomVisuals dominion;
    [SerializeField] private PhantomVisuals zenith;
    [SerializeField] private PhantomVisuals stigma;

    private Coroutine activeCoroutine;

    [System.Serializable]
    public class PhantomVisuals
    {
        [PreviewField(100)]
        public Sprite gradientSprite;

        [PreviewField(100)]
        public Sprite iconSprite;
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        // Изначально панель невидима
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Показывает градиент для указанного фантома
    /// </summary>
    public void Show(PhantomManager.PhantomType type)
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        PhantomVisuals visuals = GetVisualsForType(type);
        if (visuals == null)
        {
            Debug.LogWarning($"[GradientPanel] Визуалы для {type} не настроены!");
            return;
        }

        activeCoroutine = StartCoroutine(ShowAndFadeSequence(visuals));
    }

    private PhantomVisuals GetVisualsForType(PhantomManager.PhantomType type)
    {
        switch (type)
        {
            case PhantomManager.PhantomType.Dominion: return dominion;
            case PhantomManager.PhantomType.Zenith: return zenith;
            case PhantomManager.PhantomType.Stigma: return stigma;
            default: return null;
        }
    }

    private IEnumerator ShowAndFadeSequence(PhantomVisuals visuals)
    {
        // Устанавливаем спрайты
        if (gradientImage != null)
            gradientImage.sprite = visuals.gradientSprite;

        if (phantomIcon != null)
            phantomIcon.sprite = visuals.iconSprite;

        // Активируем панель
        canvasGroup.gameObject.SetActive(true);

        // Быстрое появление
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Пауза на пике
        yield return new WaitForSeconds(holdDuration);

        // Плавное исчезновение
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // Деактивируем панель
        canvasGroup.gameObject.SetActive(false);

        activeCoroutine = null;
    }

    /// <summary>
    /// Моментально скрывает панель (для экстренных случаев)
    /// </summary>
    public void HideImmediate()
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
        activeCoroutine = null;
    }

    // --- Editor Helper ---
    [Button("Test Dominion"), ButtonGroup]
    private void TestDominion() => Show(PhantomManager.PhantomType.Dominion);

    [Button("Test Zenith"), ButtonGroup]
    private void TestZenith() => Show(PhantomManager.PhantomType.Zenith);

    [Button("Test Stigma"), ButtonGroup]
    private void TestStigma() => Show(PhantomManager.PhantomType.Stigma);
}