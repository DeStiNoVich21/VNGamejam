using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ChoicePanel : MonoBehaviour
{
    public static ChoicePanel instance { get; private set; }

    [Header("Основные компоненты")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private VerticalLayoutGroup buttonLayoutGroup;

    [Header("Общий фон (Overlay)")]
    [SerializeField] private CanvasGroup backgroundOverlay;
    [SerializeField] private float overlayAlpha = 0.6f;
    [SerializeField] private float overlayFadeSpeed = 5f;

    [Header("Настройки наведения (Hover)")]
    [SerializeField] private bool useScaleOnHover = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private Color hoverBtnColor = Color.white;
    [SerializeField] private Color hoverTextColor = new Color(1f, 0.85f, 0f, 1f);

    [Header("Настройки моргания (Blink)")]
    [SerializeField] private float blinkIntensity = 2.5f;
    [SerializeField] private float blinkDuration = 0.25f;
    [SerializeField] private float pressScale = 0.96f;

    private List<ChoiceButtonData> buttons = new List<ChoiceButtonData>();
    private AudioSource audioSource;
    private CanvasGroupController cg;
    private Coroutine overlayCor;

    public ChoicePanelDecision lastDecision { get; private set; }
    public bool isWaitingOnUserChoice { get; private set; } = false;

    private void Awake()
    {
        instance = this;

        cg = new CanvasGroupController(this, canvasGroup);
        cg.alpha = 0f;
        cg.SetInteractableState(false);

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        if (backgroundOverlay != null) backgroundOverlay.alpha = 0;
    }

    public void Show(string question, string[] choices)
    {
        lastDecision = new ChoicePanelDecision(question, choices);
        isWaitingOnUserChoice = true;
        titleText.text = question;

        cg.Show();
        cg.SetInteractableState(true);
        StartCoroutine(GenerateChoices(choices));
    }

    private IEnumerator GenerateChoices(string[] choices)
    {
        const float PADDING_W = 400f;
        float maxWidth = 0;

        for (int i = 0; i < choices.Length; i++)
        {
            ChoiceButtonData data = GetOrCreateButton(i);

            data.button.onClick.RemoveAllListeners();
            int index = i;
            data.button.onClick.AddListener(() => AcceptAnswer(index));

            data.title.text = choices[i];
            data.isSelected = false;

            ResetButtonVisuals(data);

            maxWidth = Mathf.Max(maxWidth, Mathf.Clamp(data.title.preferredWidth + PADDING_W, 100, 1000));
        }

        foreach (var b in buttons) b.layout.preferredWidth = maxWidth;

        for (int i = 0; i < buttons.Count; i++)
            buttons[i].button.gameObject.SetActive(i < choices.Length);

        yield return new WaitForEndOfFrame();

        foreach (var b in buttons)
        {
            if (b.button.gameObject.activeSelf)
                b.layout.preferredHeight = 15f + (35f * b.title.textInfo.lineCount);
        }
    }

    private ChoiceButtonData GetOrCreateButton(int index)
    {
        if (index < buttons.Count) return buttons[index];

        GameObject obj = Instantiate(choiceButtonPrefab, buttonLayoutGroup.transform);
        var data = new ChoiceButtonData
        {
            button = obj.GetComponent<Button>(),
            title = obj.GetComponentInChildren<TextMeshProUGUI>(),
            layout = obj.GetComponent<LayoutElement>(),
            image = obj.GetComponent<Image>(),
            rect = obj.GetComponent<RectTransform>()
        };

        data.origBtnCol = data.image != null ? data.image.color : Color.white;
        data.origTxtCol = data.title.color;

        buttons.Add(data);
        SetupButtonEvents(data);
        return data;
    }

    private void SetupButtonEvents(ChoiceButtonData data)
    {
        EventTrigger trigger = data.button.gameObject.GetComponent<EventTrigger>() ?? data.button.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((_) => { OnHover(data, true); ToggleOverlay(true); });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((_) => { OnHover(data, false); ToggleOverlay(false); });
        trigger.triggers.Add(exit);

        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener((_) => { if (!data.isSelected) StartCoroutine(AnimateBlink(data)); });
        trigger.triggers.Add(down);
    }

    private void OnHover(ChoiceButtonData data, bool isHover)
    {
        if (data.isSelected || !isWaitingOnUserChoice) return;

        if (data.animCor != null) StopCoroutine(data.animCor);
        data.animCor = StartCoroutine(AnimateHover(data, isHover));

    }

    private IEnumerator AnimateHover(ChoiceButtonData data, bool isHover)
    {
        float t = 0;
        Color targetBtn = isHover ? hoverBtnColor : data.origBtnCol;
        Color targetTxt = isHover ? hoverTextColor : data.origTxtCol;
        Vector3 targetScale = Vector3.one * (isHover && useScaleOnHover ? hoverScale : 1f);

        while (t < 1f)
        {
            t += Time.deltaTime * 12f;
            if (data.image) data.image.color = Color.Lerp(data.image.color, targetBtn, t);
            data.title.color = Color.Lerp(data.title.color, targetTxt, t);
            data.rect.localScale = Vector3.Lerp(data.rect.localScale, targetScale, t);
            yield return null;
        }
    }

    private IEnumerator AnimateBlink(ChoiceButtonData data)
    {
        float elapsed = 0;
        while (elapsed < blinkDuration)
        {
            elapsed += Time.deltaTime;
            float wave = Mathf.Sin((elapsed / blinkDuration) * Mathf.PI);
            float factor = 1f + (wave * (blinkIntensity - 1f));

            if (data.image) data.image.color = data.origBtnCol * factor;
            data.title.color = hoverTextColor * factor; 
            data.rect.localScale = Vector3.one * (1f - (wave * (1f - pressScale)));
            yield return null;
        }

        data.title.color = hoverTextColor;
    }

    private void ToggleOverlay(bool show)
    {
        if (!backgroundOverlay) return;
        if (overlayCor != null) StopCoroutine(overlayCor);
        overlayCor = StartCoroutine(FadeOverlay(show ? overlayAlpha : 0));
    }

    private IEnumerator FadeOverlay(float target)
    {
        while (!Mathf.Approximately(backgroundOverlay.alpha, target))
        {
            backgroundOverlay.alpha = Mathf.MoveTowards(backgroundOverlay.alpha, target, Time.deltaTime * overlayFadeSpeed);
            yield return null;
        }
    }

    private void AcceptAnswer(int index)
    {
        if (!isWaitingOnUserChoice) return;

        isWaitingOnUserChoice = false;
        lastDecision.answerIndex = index;
        buttons[index].isSelected = true;

        ToggleOverlay(false);
        StartCoroutine(HideWithDelay());
    }

    private IEnumerator HideWithDelay()
    {
        yield return new WaitForSeconds(0.4f);
        cg.Hide();
        cg.SetInteractableState(false);
    }

    private void ResetButtonVisuals(ChoiceButtonData data)
    {
        if (data.animCor != null) StopCoroutine(data.animCor);
        if (data.image) data.image.color = data.origBtnCol;
        data.title.color = data.origTxtCol;
        data.rect.localScale = Vector3.one;
    }

    private class ChoiceButtonData
    {
        public Button button;
        public TextMeshProUGUI title;
        public LayoutElement layout;
        public Image image;
        public RectTransform rect;
        public Color origBtnCol, origTxtCol;
        public bool isSelected;
        public Coroutine animCor;
    }

    public class ChoicePanelDecision
    {
        public string question;
        public int answerIndex = -1;
        public string[] choices;

        public ChoicePanelDecision(string q, string[] c)
        {
            question = q;
            choices = c;
            answerIndex = -1;
        }
    }
}