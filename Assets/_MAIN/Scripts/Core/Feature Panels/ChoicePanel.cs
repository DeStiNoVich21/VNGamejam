using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ChoicePanel : MonoBehaviour
{
    public static ChoicePanel instance { get; private set; }

    private const float BUTTON_MIN_WIDTH = 50;
    private const float BUTTON_MAX_WIDTH = 1000;
    private const float BUTTON_WIDTH_PADDING = 400;

    private const float BUTTON_HIEGHT_PER_LINE = 30;
    private const float BUTTON_HIEGHT_PADDING = 10;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private VerticalLayoutGroup buttonLayoutGroup;

    [Header("Animation Colors")]
    [SerializeField] private Color hoverColor = new Color(1, 1, 1, 0.2f);
    [SerializeField] private Color clickColor = Color.white;
    private Color normalColor = new Color(1, 1, 1, 0.05f);

    private CanvasGroupController cg = null;
    private List<ChoiceButton> buttons = new List<ChoiceButton>();
    public ChoicePanelDecision lastDecision { get; private set; } = null;

    public bool isWaitingOnUserChoice { get; private set; } = false;

    private void Awake()
    {
        instance = this;
        cg = new CanvasGroupController(this, canvasGroup);

        cg.alpha = 0f;
        cg.SetInteractableState(false);
    }

    public void Show(string question, string[] choices)
    {
        lastDecision = new ChoicePanelDecision(question, choices);
        isWaitingOnUserChoice = true;

        cg.Show();
        cg.SetInteractableState(active: true);

        titleText.text = question;
        StartCoroutine(GenerateChoices(choices));
    }

    private IEnumerator GenerateChoices(string[] choices)
    {
        float maxWidth = 0;

        for (int i = 0; i < choices.Length; i++)
        {
            ChoiceButton choiceButton;
            if (i < buttons.Count)
            {
                choiceButton = buttons[i];
            }
            else
            {
                GameObject newButtonObject = Instantiate(choiceButtonPrefab, buttonLayoutGroup.transform);
                newButtonObject.SetActive(true);

                Button newButton = newButtonObject.GetComponent<Button>();
                newButton.transition = Button.Transition.None;

                choiceButton = new ChoiceButton
                {
                    button = newButton,
                    layout = newButton.GetComponent<LayoutElement>(),
                    title = newButton.GetComponentInChildren<TextMeshProUGUI>(),
                    image = newButton.GetComponent<Image>(),
                    trigger = newButton.gameObject.AddComponent<EventTrigger>()
                };

                var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((data) => { OnHover(newButton.GetComponent<Image>(), true); });
                choiceButton.trigger.triggers.Add(entryEnter);

                var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((data) => { OnHover(newButton.GetComponent<Image>(), false); });
                choiceButton.trigger.triggers.Add(entryExit);

                buttons.Add(choiceButton);
            }

            choiceButton.button.onClick.RemoveAllListeners();
            int buttonIndex = i;
            choiceButton.button.onClick.AddListener(() => AcceptAnswer(buttonIndex));
            choiceButton.title.text = choices[i];
            choiceButton.image.color = normalColor;

            float buttonWidth = Mathf.Clamp(BUTTON_WIDTH_PADDING + choiceButton.title.preferredWidth, BUTTON_MIN_WIDTH, BUTTON_MAX_WIDTH);
            maxWidth = Mathf.Max(maxWidth, buttonWidth);
        }

        foreach (var button in buttons)
        {
            button.layout.preferredWidth = maxWidth;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].button.gameObject.SetActive(i < choices.Length);
        }

        yield return new WaitForEndOfFrame();

        foreach (var button in buttons)
        {
            int lines = button.title.textInfo.lineCount;
            button.layout.preferredHeight = BUTTON_HIEGHT_PADDING + (BUTTON_HIEGHT_PER_LINE * lines);
        }
    }

    private void OnHover(Image targetImage, bool isHovering)
    {
        if (!isWaitingOnUserChoice) return;
        targetImage.color = isHovering ? hoverColor : normalColor;
    }

    private void AcceptAnswer(int index)
    {
        if (index < 0 || index > lastDecision.choices.Length - 1) return;

        // 1. —разу записываем индекс, чтобы LL_Choice его увидел
        lastDecision.answerIndex = index;

        // 2. “еперь говорим, что выбор сделан
        isWaitingOnUserChoice = false;

        cg.SetInteractableState(false);

        // 3. «апускаем анимацию (теперь она не мешает логике)
        StartCoroutine(FlashAndHide(index));
    }

    private IEnumerator FlashAndHide(int index)
    {
        Image img = buttons[index].image;
        for (int i = 0; i < 3; i++)
        {
            img.color = clickColor;
            yield return new WaitForSecondsRealtime(0.08f);
            img.color = normalColor;
            yield return new WaitForSecondsRealtime(0.08f);
        }

        // ћы уже установили индекс в AcceptAnswer, так что здесь эту строку удал€ем
        Hide();
    }

    public void Hide()
    {
        cg.Hide();
        cg.SetInteractableState(false);
    }

    public class ChoicePanelDecision
    {
        public string question = string.Empty;
        public int answerIndex = -1;
        public string[] choices = new string[0];

        public ChoicePanelDecision(string question, string[] choices)
        {
            this.question = question;
            this.choices = choices;
            answerIndex = -1;
        }
    }

    private struct ChoiceButton
    {
        public Button button;
        public TextMeshProUGUI title;
        public LayoutElement layout;
        public Image image;
        public EventTrigger trigger;
    }
}