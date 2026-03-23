using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Якорь слота 5W+H на доске.
/// Стикер перетаскивается сюда — слот принимает или отклоняет.
/// Визуально: большая карточка с вопросом (КТО? ЧТО? ГДЕ?...) и счётчиком.
/// </summary>
public class SlotAnchorUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Image background;
    [SerializeField] private GameObject filledIndicator;

    private SlotConfig config;
    private InvestigationBoardUI boardUI;

    // Цвета слотов
    private static readonly Color COLOR_EMPTY = new Color(0.15f, 0.15f, 0.15f, 0.7f);
    private static readonly Color COLOR_FILLED = new Color(0.2f, 0.35f, 0.2f, 0.8f);
    private static readonly Color COLOR_WHY = new Color(0.35f, 0.15f, 0.15f, 0.8f);

    public void Initialize(SlotConfig cfg, InvestigationBoardUI ui)
    {
        config = cfg;
        boardUI = ui;

        if (questionText) questionText.text = cfg.question;

        if (background)
            background.color = cfg.tag == StickerTag.Why ? COLOR_WHY : COLOR_EMPTY;

        UpdateCount();
    }

    public void OnDrop(PointerEventData e)
    {
        // Проверяем что перетаскивается StickerUI
        StickerUI sticker = e.pointerDrag?.GetComponent<StickerUI>();
        if (sticker == null) return;

        // Получаем id через рефлексию (поле private)
        var field = typeof(StickerUI).GetField("stickerId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        string id = field?.GetValue(sticker) as string;
        if (string.IsNullOrEmpty(id)) return;

        boardUI.TryDropStickerInSlot(id, config.tag);
        UpdateCount();
    }

    private void UpdateCount()
    {
        if (!countText) return;
        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;

        int count = mgr.GetSlotContents(config.tag).Count;
        int max = config.maxStickers;

        countText.text = max > 0 ? $"{count}/{max}" : $"{count}";

        bool filled = count > 0;
        if (background) background.color = filled ? COLOR_FILLED : COLOR_EMPTY;
        if (filledIndicator) filledIndicator.SetActive(filled);
    }
}