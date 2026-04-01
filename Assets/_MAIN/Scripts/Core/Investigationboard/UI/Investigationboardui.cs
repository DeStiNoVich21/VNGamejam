using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.UI;

using UnityEngine.EventSystems;

using TMPro;

using Sirenix.OdinInspector;



public class InvestigationBoardUI : MonoBehaviour,

    IPointerDownHandler, IDragHandler, IScrollHandler

{

    public static InvestigationBoardUI instance { get; private set; }



    [Title("Корень")]

    [SerializeField] private GameObject boardRoot;

    [SerializeField] private CanvasGroup boardCG;

    [SerializeField] private RectTransform boardCanvas;

    [SerializeField] private RectTransform stickersLayer;

    [SerializeField] private RectTransform ropesLayer;

    [SerializeField] private RectTransform slotsLayer;



    // --- ИСПРАВЛЕННЫЙ БЛОК: Список слотов ---

    [Title("Ручная расстановка слотов")]

    [InfoBox("Перетащите сюда SlotAnchorUI, которые вы расставили на доске вручную.")]

    // Добавляем ShowInInspector на случай, если SerializeField не подхватывается Odin-ом

    [SerializeField, ShowInInspector]

    private List<SlotAnchorUI> manualSlots = new List<SlotAnchorUI>();

    [Title("Префабы")]

    [SerializeField] private StickerUI stickerPrefab;

    [SerializeField] private BoardRopeUI ropePrefab;

    [SerializeField] private SynthesisPopupUI synthesisPrefab;



    [Title("Детали стикера")]

    [SerializeField] private GameObject detailPanel;

    [SerializeField] private TextMeshProUGUI detailTitle;

    [SerializeField] private TextMeshProUGUI detailBody;

    [SerializeField] private Transform detailCommentsRoot;

    [SerializeField] private PhantomCommentEntryUI commentPrefab;



    [Title("Кнопка Синтез")]

    [SerializeField] private Button synthesisButton;

    [SerializeField] private GameObject synthesisReadyGlow;



    [Title("Навигация")]

    [SerializeField] private float zoomMin = 0.3f;

    [SerializeField] private float zoomMax = 1.5f;

    [SerializeField] private float zoomStep = 0.1f;

    [SerializeField] private float openSpeed = 6f;



    [Title("Инвентарь")]

    [SerializeField] private GameObject inventoryPanel;

    [SerializeField] private Transform inventoryContentRoot;

    [SerializeField] private StickerListItemUI inventoryItemPrefab;

    [SerializeField] private Button inventoryToggleButton;

    [SerializeField] private TextMeshProUGUI inventoryToggleButtonText;



    [Title("Фильтры инвентаряя")]

    [SerializeField] private Transform filterRow;

    [SerializeField] private Button filterButtonPrefab;







    public bool isOpen = false;

    private float currentZoom = 1f;

    private Vector2 dragStart;

    private bool isPanningBoard = false;



    private Dictionary<string, StickerUI> spawnedStickers = new();

    private Dictionary<string, SlotAnchorUI> spawnedSlots = new Dictionary<string, SlotAnchorUI>();

    private Dictionary<string, BoardRopeUI> spawnedRopes = new();



    private bool isDraggingRope = false;

    private string ropeSourceId = "";

    private BoardRopeUI draftRope = null;

    private Coroutine co_fade;



    private bool isInventoryOpen = true;

    private StickerTag? activeInventoryFilter = null;

    private List<StickerListItemUI> spawnedInventoryItems = new();



    private void Awake()

    {

        if (instance == null) instance = this;

        else { Destroy(gameObject); return; }



        boardRoot.SetActive(false);

        boardCG.alpha = 0f;

        detailPanel.SetActive(false);



        if (synthesisButton) synthesisButton.onClick.AddListener(OnSynthesisClick);

        if (synthesisReadyGlow) synthesisReadyGlow.SetActive(false);

        if (inventoryToggleButton) inventoryToggleButton.onClick.AddListener(ToggleInventoryPanel);



        BuildInventoryFilterButtons();

        // Инициализируем вручную расставленные слоты сразу

        InitializeManualSlots();

    }

    public void RemoveStickerFromBoard(string stickerId)
    {
        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;

        // 1. ЛОГИКА: Сообщаем менеджеру, что улика больше не на доске и не в слоте
        // Это автоматически вернет её в GetInventory()
        mgr.UnplaceSticker(stickerId);

        // 2. ВИЗУАЛ: Удаляем объект с доски
        if (spawnedStickers.TryGetValue(stickerId, out var s))
        {
            Destroy(s.gameObject);
            spawnedStickers.Remove(stickerId);
        }

        // 3. НИТИ: Чистим связанные веревки
        List<string> ropesToRemove = new List<string>();
        foreach (var kvp in spawnedRopes)
        {
            if (kvp.Key.Contains(stickerId))
                ropesToRemove.Add(kvp.Key);
        }

        foreach (var key in ropesToRemove)
        {
            if (spawnedRopes.TryGetValue(key, out var rope))
            {
                Destroy(rope.gameObject);
                spawnedRopes.Remove(key);
            }
        }

        // 4. ОБНОВЛЕНИЕ: Перерисовываем список инвентаря справа
        RefreshInventory();

        Debug.Log($"[Board] Улика {stickerId} возвращена в инвентарь.");
    }

    // --- НОВОЕ: Регистрация ваших слотов со сцены ---

    private void InitializeManualSlots()

    {

        spawnedSlots.Clear();

        var caseData = InvestigationBoardManager.instance.GetActiveCase();



        foreach (var anchor in manualSlots)

        {

            if (anchor == null) continue;



            // Находим конфиг слота в данных дела по тегу, указанному в скрипте якоря

            // (Убедитесь, что в SlotAnchorUI есть поле или свойство, возвращающее его StickerTag)

            var config = caseData.GetAllSlots().Find(s => s.tag == anchor.GetTag());



            if (config != null)

            {

                anchor.Initialize(config, this);

                spawnedSlots[config.tag.ToString()] = anchor;

            }

            else

            {

                Debug.LogWarning($"[Board] Слот с тегом {anchor.GetTag()} не найден в конфиге активного дела!");

            }

        }

    }



    private void OnEnable()

    {

        var mgr = InvestigationBoardManager.instance;

        if (mgr == null) return;

        mgr.onStickerAdded += OnStickerAdded;

        mgr.onStickerRemoved += OnStickerRemoved;

        mgr.onRopeAdded += OnRopeAdded;

        mgr.onRopeRemoved += OnRopeRemoved;

        mgr.onSynthesisReady += OnSynthesisReady;

        mgr.onStickerPlaced += OnStickerPlaced;

        mgr.onStickerUnplaced += OnStickerUnplaced;

    }



    private void OnDisable()

    {

        var mgr = InvestigationBoardManager.instance;

        if (mgr == null) return;

        mgr.onStickerAdded -= OnStickerAdded;

        mgr.onStickerRemoved -= OnStickerRemoved;

        mgr.onRopeAdded -= OnRopeAdded;

        mgr.onRopeRemoved -= OnRopeRemoved;

        mgr.onSynthesisReady -= OnSynthesisReady;

        mgr.onStickerPlaced -= OnStickerPlaced;

        mgr.onStickerUnplaced -= OnStickerUnplaced;

    }



    public void Open()

    {

        if (isOpen) return;

        isOpen = true;

        boardRoot.SetActive(true);

        // BuildSlotAnchors больше не вызываем

        RefreshInventory();

        RefreshAllRopes();

        UpdateSynthesisButton();



        if (co_fade != null) StopCoroutine(co_fade);

        co_fade = StartCoroutine(Fade(1f));

    }



    public void Close()

    {

        if (!isOpen) return;

        isOpen = false;

        detailPanel.SetActive(false);

        if (co_fade != null) StopCoroutine(co_fade);

        co_fade = StartCoroutine(Fade(0f, () => boardRoot.SetActive(false)));

    }



    private IEnumerator Fade(float target, System.Action onDone = null)

    {

        while (Mathf.Abs(boardCG.alpha - target) > 0.01f)

        {

            boardCG.alpha = Mathf.MoveTowards(boardCG.alpha, target, openSpeed * Time.deltaTime);

            yield return null;

        }

        boardCG.alpha = target;

        onDone?.Invoke();

        co_fade = null;

    }



    // --- Навигация ---

    public void OnPointerDown(PointerEventData e)

    {

        if (e.button == PointerEventData.InputButton.Middle)

        {

            isPanningBoard = true;

            dragStart = e.position;

        }

    }



    public void OnDrag(PointerEventData e)

    {

        if (isPanningBoard)

        {

            boardCanvas.anchoredPosition += (e.position - dragStart) / currentZoom;

            dragStart = e.position;

        }



        if (isDraggingRope && draftRope != null)

        {

            RectTransformUtility.ScreenPointToLocalPointInRectangle(boardCanvas, e.position, null, out Vector2 localPos);

            draftRope.SetEnd(localPos);

        }

    }



    public void OnScroll(PointerEventData e)

    {

        float delta = e.scrollDelta.y > 0 ? zoomStep : -zoomStep;

        currentZoom = Mathf.Clamp(currentZoom + delta, zoomMin, zoomMax);

        boardCanvas.localScale = Vector3.one * currentZoom;

    }



    // --- Стикеры и верёвки ---

    public void SpawnStickerOnBoard(StickerData data)

    {

        if (data == null || spawnedStickers.ContainsKey(data.stickerId)) return;

        StickerUI sticker = Instantiate(stickerPrefab, stickersLayer);

        sticker.Initialize(data, this);

        sticker.GetComponent<RectTransform>().anchoredPosition = InvestigationBoardManager.instance.GetPosition(data.stickerId);

        spawnedStickers[data.stickerId] = sticker;

    }



    private void OnStickerAdded(StickerData data) => RefreshInventory();



    private void OnStickerRemoved(string id)

    {

        if (spawnedStickers.TryGetValue(id, out var s)) { Destroy(s.gameObject); spawnedStickers.Remove(id); }

        RefreshInventory();

    }



    private void RefreshAllRopes()

    {

        foreach (var rope in InvestigationBoardManager.instance.GetRopes()) SpawnRope(rope);

    }



    private RectTransform GetRopeStartpoint(string stickerId)

    {

        var slot = InvestigationBoardManager.instance.GetStickerSlot(stickerId);

        if (slot.HasValue && spawnedSlots.TryGetValue(slot.Value.ToString(), out var anchor))

            return anchor.GetComponent<RectTransform>();



        if (spawnedStickers.TryGetValue(stickerId, out var sticker))

            return sticker.GetComponent<RectTransform>();

        return null;

    }



    private RectTransform GetRopeEndpoint(string stickerId)

    {

        if (spawnedStickers.TryGetValue(stickerId, out var sticker))

            return sticker.GetComponent<RectTransform>();

        return null;

    }



    private void SpawnRope(BoardRope rope)

    {

        string key = RopeKey(rope.idA, rope.idB);

        if (spawnedRopes.ContainsKey(key)) return;



        RectTransform pointA = GetRopeStartpoint(rope.idA);

        RectTransform pointB = GetRopeEndpoint(rope.idB);



        if (pointA == null || pointB == null)

        {

            pointA = GetRopeStartpoint(rope.idB);

            pointB = GetRopeEndpoint(rope.idA);

        }



        if (pointA != null && pointB != null)

        {

            BoardRopeUI ui = Instantiate(ropePrefab, ropesLayer);

            ui.Initialize(pointA, pointB);

            spawnedRopes[key] = ui;

        }

    }



    private void OnRopeAdded(BoardRope rope) { if (isOpen) SpawnRope(rope); }

    private void OnRopeRemoved(BoardRope rope)

    {

        string key = RopeKey(rope.idA, rope.idB);

        if (spawnedRopes.TryGetValue(key, out var ui)) { Destroy(ui.gameObject); spawnedRopes.Remove(key); }

    }



    public void BeginRopeDrag(string fromId)

    {

        isDraggingRope = true;

        ropeSourceId = fromId;

        draftRope = Instantiate(ropePrefab, ropesLayer);

        draftRope.InitializeDraft(spawnedStickers[fromId].GetComponent<RectTransform>());

    }



    public void EndRopeDrag(string toId)

    {

        if (draftRope != null) { Destroy(draftRope.gameObject); draftRope = null; }

        isDraggingRope = false;

        if (!string.IsNullOrEmpty(toId) && toId != ropeSourceId)

            InvestigationBoardManager.instance.TryAddRope(ropeSourceId, toId);

    }



    public void TryDropStickerInSlot(string stickerId, StickerTag slot)

    {

        if (!spawnedStickers.ContainsKey(stickerId))

        {

            StickerData data = InvestigationBoardManager.instance.GetStickerById(stickerId);

            if (data != null) SpawnStickerOnBoard(data);

        }

        bool ok = InvestigationBoardManager.instance.TryPlaceInSlot(stickerId, slot);

        if (!ok && spawnedStickers.TryGetValue(stickerId, out var s)) s.PlayRejectEffect();

    }



    public SlotAnchorUI FindNearestSlotInRadius(Vector2 worldPosition, float maxDistance = 200f)

    {

        SlotAnchorUI nearest = null;

        float closestDistance = maxDistance;

        foreach (var slot in spawnedSlots.Values)

        {

            if (slot.IsPositionInDropZone(worldPosition))

            {

                float distance = Vector2.Distance(worldPosition, slot.GetSlotWorldCenter());

                if (distance < closestDistance) { nearest = slot; closestDistance = distance; }

            }

        }

        return nearest;

    }



    // --- События размещения ---

    private void OnStickerPlaced(StickerTag tag, string stickerId)

    {

        if (!isOpen || !spawnedStickers.TryGetValue(stickerId, out var s)) return;

        if (!spawnedSlots.TryGetValue(tag.ToString(), out var anchor)) return;



        s.transform.SetAsLastSibling();

        anchor.RefreshCount();

        RefreshRopesForSticker(stickerId);

    }



    private void OnStickerUnplaced(StickerTag tag, string stickerId)

    {

        if (!isOpen || !spawnedStickers.TryGetValue(stickerId, out var s)) return;

        s.GetComponent<RectTransform>().anchoredPosition = InvestigationBoardManager.instance.GetPosition(stickerId);

        if (spawnedSlots.TryGetValue(tag.ToString(), out var anchor)) anchor.RefreshCount();

        RefreshRopesForSticker(stickerId);

        RefreshInventory();

    }



    private void RefreshRopesForSticker(string stickerId)

    {

        var mgr = InvestigationBoardManager.instance;

        foreach (var rope in mgr.GetRopes())

        {

            if (rope.idA == stickerId || rope.idB == stickerId)

            {

                string key = RopeKey(rope.idA, rope.idB);

                if (spawnedRopes.ContainsKey(key)) { Destroy(spawnedRopes[key].gameObject); spawnedRopes.Remove(key); }

                SpawnRope(rope);

            }

        }

    }



    // --- Инвентарь ---

    private void BuildInventoryFilterButtons()

    {

        if (filterRow == null || filterButtonPrefab == null) return;

        SpawnInventoryFilterButton("ВСЕ", null);

        foreach (StickerTag tag in System.Enum.GetValues(typeof(StickerTag)))

        {

            if (tag == StickerTag.Fact) continue;

            SpawnInventoryFilterButton(tag.ToString().ToUpper(), tag);

        }

        SpawnInventoryFilterButton("ФАКТЫ", StickerTag.Fact);

    }



    private void SpawnInventoryFilterButton(string label, StickerTag? tag)

    {

        Button btn = Instantiate(filterButtonPrefab, filterRow);

        btn.GetComponentInChildren<TextMeshProUGUI>().text = label;

        btn.onClick.AddListener(() => { activeInventoryFilter = tag; RefreshInventory(); UpdateInventoryFilterHighlight(btn); });

    }



    private void UpdateInventoryFilterHighlight(Button active)

    {

        foreach (Transform t in filterRow)

        {

            var btn = t.GetComponent<Button>();

            if (btn == null) continue;

            var colors = btn.colors;

            colors.normalColor = btn == active ? new Color(0.3f, 0.6f, 0.3f) : new Color(0.2f, 0.2f, 0.2f);

            btn.colors = colors;

        }

    }



    private void RefreshInventory()

    {

        foreach (var item in spawnedInventoryItems) Destroy(item.gameObject);

        spawnedInventoryItems.Clear();

        foreach (var id in InvestigationBoardManager.instance.GetInventory())

        {

            StickerData data = InvestigationBoardManager.instance.GetStickerById(id);

            if (data == null || (activeInventoryFilter.HasValue && data.tag != activeInventoryFilter.Value)) continue;

            StickerListItemUI item = Instantiate(inventoryItemPrefab, inventoryContentRoot);

            item.Initialize(data);

            spawnedInventoryItems.Add(item);

        }

    }



    private void ToggleInventoryPanel()

    {

        isInventoryOpen = !isInventoryOpen;

        inventoryPanel.SetActive(isInventoryOpen);

        if (isInventoryOpen) RefreshInventory();

    }



    public void ShowDetail(string id)

    {

        StickerData data = InvestigationBoardManager.instance.GetStickerById(id);

        if (data == null) return;

        detailPanel.SetActive(true);

        detailTitle.text = data.title;

        detailBody.text = data.bodyText;

        foreach (Transform t in detailCommentsRoot) Destroy(t.gameObject);

        foreach (var c in InvestigationBoardManager.instance.GetVisibleComments(id))

            Instantiate(commentPrefab, detailCommentsRoot).Initialize(c);

    }



    public void HideDetail() => detailPanel.SetActive(false);



    private void UpdateSynthesisButton()

    {

        bool ready = InvestigationBoardManager.instance.IsSynthesisReady();

        if (synthesisButton) synthesisButton.interactable = ready;

        if (synthesisReadyGlow) synthesisReadyGlow.SetActive(ready);

    }



    private void OnSynthesisReady() => UpdateSynthesisButton();

    private void OnSynthesisClick()

    {

        CaseConclusion result = InvestigationBoardManager.instance.RunSynthesis();

        if (result != null) Instantiate(synthesisPrefab, transform).Show(result);

    }


    [Title("Управление всеми уликами")]
    [Button("Вернуть всё в инвентарь", ButtonSizes.Medium)]
    public void RecallAllStickersToInventory()
    {
        // Создаем копию списка ключей, так как RemoveStickerFromBoard меняет словарь spawnedStickers
        List<string> activeStickerIds = new List<string>(spawnedStickers.Keys);

        if (activeStickerIds.Count == 0)
        {
            Debug.Log("[Board] На доске нет улик для возврата.");
            return;
        }

        foreach (string id in activeStickerIds)
        {
            RemoveStickerFromBoard(id);
        }

        // Дополнительно очищаем визуальные слоты, если там что-то осталось
        foreach (var anchor in spawnedSlots.Values)
        {
            anchor.RefreshCount();
        }

        Debug.Log($"[Board] Реколл завершен. Вернулось объектов: {activeStickerIds.Count}");
    }
    private void OnConclusionUnlocked(CaseConclusion c) { }



    private string RopeKey(string a, string b) => string.Compare(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

    public bool TryGetSlotAnchor(string slotKey, out SlotAnchorUI anchor) => spawnedSlots.TryGetValue(slotKey, out anchor);

    public bool IsDraggingRope => isDraggingRope;

    public string RopeSourceId => ropeSourceId;

    public RectTransform BoardCanvas => boardCanvas;

}