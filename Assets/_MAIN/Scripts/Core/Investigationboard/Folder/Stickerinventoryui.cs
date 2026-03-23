using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// Панель инвентаря улик — список стикеров которые добавлены но ещё не в слотах.
/// Открывается кнопкой сбоку доски.
/// Стикер из списка можно перетащить на доску.
///
/// Иерархия:
///   InventoryPanel
///     Header (TMP — "УЛИКИ")
///     FilterRow (кнопки Who/What/Where/When/Why/How/All)
///     ScrollView ? Content (вертикальный layout)
///     ToggleButton (открыть/скрыть панель)
/// </summary>
public class StickerInventoryUI : MonoBehaviour
{
    [Title("UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private StickerListItemUI itemPrefab;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Title("Фильтры")]
    [SerializeField] private Transform filterRow;
    [SerializeField] private Button filterButtonPrefab; // префаб кнопки фильтра

    private bool isPanelOpen = true;
    private StickerTag? activeFilter = null;
    private List<StickerListItemUI> spawnedItems = new();

    private void Awake()
    {
        if (toggleButton)
            toggleButton.onClick.AddListener(TogglePanel);

        BuildFilterButtons();
    }

    private void OnEnable()
    {
        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;
        mgr.onStickerAdded += _ => Refresh();
        mgr.onStickerRemoved += _ => Refresh();
        mgr.onStickerPlaced += (_, __) => Refresh();
        mgr.onStickerUnplaced += (_, __) => Refresh();
    }

    private void OnDisable()
    {
        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;
        mgr.onStickerAdded -= _ => Refresh();
        mgr.onStickerRemoved -= _ => Refresh();
        mgr.onStickerPlaced -= (_, __) => Refresh();
        mgr.onStickerUnplaced -= (_, __) => Refresh();
    }

    // ??? Построить кнопки фильтра ????????????????????????????????????

    private void BuildFilterButtons()
    {
        if (filterRow == null || filterButtonPrefab == null) return;

        // Кнопка "Все"
        SpawnFilterButton("ВСЕ", null);

        foreach (StickerTag tag in System.Enum.GetValues(typeof(StickerTag)))
        {
            if (tag == StickerTag.Fact) continue; // факты отдельно
            StickerTag captured = tag;
            SpawnFilterButton(tag.ToString().ToUpper(), captured);
        }

        SpawnFilterButton("ФАКТЫ", StickerTag.Fact);
    }

    private void SpawnFilterButton(string label, StickerTag? tag)
    {
        Button btn = Instantiate(filterButtonPrefab, filterRow);
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp) tmp.text = label;

        StickerTag? captured = tag;
        btn.onClick.AddListener(() =>
        {
            activeFilter = captured;
            Refresh();
            UpdateFilterHighlight(btn);
        });
    }

    private void UpdateFilterHighlight(Button active)
    {
        foreach (Transform t in filterRow)
        {
            var btn = t.GetComponent<Button>();
            if (btn == null) continue;
            var colors = btn.colors;
            colors.normalColor = btn == active
                ? new Color(0.3f, 0.6f, 0.3f)
                : new Color(0.2f, 0.2f, 0.2f);
            btn.colors = colors;
        }
    }

    // ??? Обновить список ?????????????????????????????????????????????

    public void Refresh()
    {
        // Очищаем
        foreach (var item in spawnedItems) Destroy(item.gameObject);
        spawnedItems.Clear();

        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;

        // Инвентарь = стикеры добавленные но не в слоте
        List<string> ids = mgr.GetInventory();

        foreach (var id in ids)
        {
            StickerData data = mgr.GetStickerById(id);
            if (data == null) continue;

            // Применяем фильтр
            if (activeFilter.HasValue && data.tag != activeFilter.Value) continue;

            StickerListItemUI item = Instantiate(itemPrefab, contentRoot);
            item.Initialize(data);
            spawnedItems.Add(item);
        }

        // Заголовок кнопки
        if (toggleButtonText)
            toggleButtonText.text = isPanelOpen ? "? УЛИКИ" : "? УЛИКИ";
    }

    // ??? Открыть / Закрыть ???????????????????????????????????????????

    public void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        if (inventoryPanel) inventoryPanel.SetActive(isPanelOpen);
        if (isPanelOpen) Refresh();

        if (toggleButtonText)
            toggleButtonText.text = isPanelOpen ? "? УЛИКИ" : "? УЛИКИ";
    }
}