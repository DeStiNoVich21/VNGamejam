using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// UI доски расследования + интегрированный инвентарь.
/// Одна большая доска — навигация зумом и перетаскиванием как в Miro/Канве.
/// Якоря слотов 5W+H фиксированы на доске.
/// Список инвентаря справа — стикеры только там, перетаскиваются на слоты.
/// Верёвки тянутся ПКМ между стикерами на доске.
/// </summary>
public class InvestigationBoardUI : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IScrollHandler
{
    public static InvestigationBoardUI instance { get; private set; }

    // ??? Inspector — Доска ???????????????????????????????????????????

    [Title("Корень")]
    [SerializeField] private GameObject boardRoot;
    [SerializeField] private CanvasGroup boardCG;
    [SerializeField] private RectTransform boardCanvas;
    [SerializeField] private RectTransform stickersLayer;
    [SerializeField] private RectTransform ropesLayer;
    [SerializeField] private RectTransform slotsLayer;

    [Title("Префабы")]
    [SerializeField] private StickerUI stickerPrefab;
    [SerializeField] private SlotAnchorUI slotAnchorPrefab;
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

    // ??? Inspector — Инвентарь ???????????????????????????????????????????

    [Title("Инвентарь (интегрированный)")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform inventoryContentRoot;
    [SerializeField] private StickerListItemUI inventoryItemPrefab;
    [SerializeField] private Button inventoryToggleButton;
    [SerializeField] private TextMeshProUGUI inventoryToggleButtonText;

    [Title("Фильтры инвентаря")]
    [SerializeField] private Transform filterRow;
    [SerializeField] private Button filterButtonPrefab;

    // ??? Runtime — Доска ???????????????????????????????????????????

    public bool isOpen = false;
    private float currentZoom = 1f;
    private Vector2 dragStart;
    private bool isPanningBoard = false;

    private Dictionary<string, StickerUI> spawnedStickers = new();
    private Dictionary<string, SlotAnchorUI> spawnedSlots = new();
    private Dictionary<string, BoardRopeUI> spawnedRopes = new();

    private bool isDraggingRope = false;
    private string ropeSourceId = "";
    private BoardRopeUI draftRope = null;

    private Coroutine co_fade;

    // ??? Runtime — Инвентарь ???????????????????????????????????????????

    private bool isInventoryOpen = true;
    private StickerTag? activeInventoryFilter = null;
    private List<StickerListItemUI> spawnedInventoryItems = new();

    // ??? Lifecycle ???????????????????????????????????????????????????

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        boardRoot.SetActive(false);
        boardCG.alpha = 0f;
        detailPanel.SetActive(false);
        
        if (synthesisButton) synthesisButton.onClick.AddListener(OnSynthesisClick);
        if (synthesisReadyGlow) synthesisReadyGlow.SetActive(false);

        // Инвентарь
        if (inventoryToggleButton)
            inventoryToggleButton.onClick.AddListener(ToggleInventoryPanel);

        BuildInventoryFilterButtons();
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
        mgr.onConclusionUnlocked += OnConclusionUnlocked;
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
        mgr.onConclusionUnlocked -= OnConclusionUnlocked;
        mgr.onStickerPlaced -= OnStickerPlaced;
        mgr.onStickerUnplaced -= OnStickerUnplaced;
    }

    // ??? Открыть / Закрыть ???????????????????????????????????????????

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        boardRoot.SetActive(true);
        BuildSlotAnchors();
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

    // ??? Навигация (Pan + Zoom) ???????????????????????????????????????

    public void OnPointerDown(PointerEventData e)
    {
        // Средняя кнопка или пустое место — панорамирование
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
            boardCanvas.anchoredPosition +=
                (e.position - dragStart) / currentZoom;
            dragStart = e.position;
        }

        if (isDraggingRope && draftRope != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                boardCanvas, e.position, null, out Vector2 localPos);
            draftRope.SetEnd(localPos);
        }
    }

    public void OnScroll(PointerEventData e)
    {
        float delta = e.scrollDelta.y > 0 ? zoomStep : -zoomStep;
        currentZoom = Mathf.Clamp(currentZoom + delta, zoomMin, zoomMax);
        boardCanvas.localScale = Vector3.one * currentZoom;
    }

    // ??? Якоря слотов 5W+H ???????????????????????????????????????????

    private void BuildSlotAnchors()
    {
        if (spawnedSlots.Count > 0) return; // уже построены

        CaseData c = InvestigationBoardManager.instance.GetActiveCase();
        if (c == null) return;

        foreach (var slot in c.GetAllSlots())
        {
            SlotAnchorUI anchor = Instantiate(slotAnchorPrefab, slotsLayer);
            anchor.Initialize(slot, this);
            anchor.GetComponent<RectTransform>().anchoredPosition = slot.boardPosition;
            spawnedSlots[slot.tag.ToString()] = anchor;
        }
    }

    // ??? Стикеры НА ДОСКЕ (только размещённые) ??????????????????????????

    public void SpawnStickerOnBoard(StickerData data)
    {
        if (data == null || spawnedStickers.ContainsKey(data.stickerId)) return;

        StickerUI sticker = Instantiate(stickerPrefab, stickersLayer);
        sticker.Initialize(data, this);

        Vector2 pos = InvestigationBoardManager.instance.GetPosition(data.stickerId);
        sticker.GetComponent<RectTransform>().anchoredPosition = pos;

        spawnedStickers[data.stickerId] = sticker;
    }

    private void OnStickerAdded(StickerData data)
    {
        // Обновляем инвентарь при добавлении
        RefreshInventory();
    }

    private void OnStickerRemoved(string id)
    {
        if (spawnedStickers.TryGetValue(id, out var s))
        {
            Destroy(s.gameObject);
            spawnedStickers.Remove(id);
        }
        
        RefreshInventory();
    }

    // ??? Верёвки ????????????????????????????????????????????????????

    private void RefreshAllRopes()
    {
        foreach (var rope in InvestigationBoardManager.instance.GetRopes())
            SpawnRope(rope);
    }

    // --- НОВОЕ: Определяет где прикрепить нить —
    // Если стикер в слоте, нить идёт ОТ слота К стикеру,
    // иначе — между самими стикерами ---
    private RectTransform GetRopeStartpoint(string stickerId)
    {
        // Проверяем, в каком слоте находится стикер
        var mgr = InvestigationBoardManager.instance;
        var slot = mgr.GetStickerSlot(stickerId);
        if (slot.HasValue)
        {
            string slotKey = slot.Value.ToString();
            if (spawnedSlots.TryGetValue(slotKey, out var anchor))
            {
                // Нить НАЧИНАЕТСЯ от слота
                Debug.Log($"[Board] Нить НАЧИНАЕТСЯ от слота {slotKey}");
                return anchor.GetComponent<RectTransform>();
            }
        }

        // Если не в слоте — начинается от самого стикера
        if (spawnedStickers.TryGetValue(stickerId, out var sticker))
        {
            Debug.Log($"[Board] Нить НАЧИНАЕТСЯ от стикера {stickerId}");
            return sticker.GetComponent<RectTransform>();
        }

        return null;
    }

    private RectTransform GetRopeEndpoint(string stickerId)
    {
        // Если стикер не спавнен — возвращаем null
        if (!spawnedStickers.TryGetValue(stickerId, out var sticker))
        {
            Debug.LogWarning($"[Board] Стикер '{stickerId}' не спавнен на доске");
            return null;
        }

        // Нить ВСЕГДА заканчивается на самом стикере
        Debug.Log($"[Board] Нить ЗАКАНЧИВАЕТСЯ на стикере {stickerId}");
        return sticker.GetComponent<RectTransform>();
    }

    private void SpawnRope(BoardRope rope)
    {
        string key = RopeKey(rope.idA, rope.idB);
        if (spawnedRopes.ContainsKey(key)) return;

        var mgr = InvestigationBoardManager.instance;
        
        // Определяем направление нити: от слота (если есть) к улике
        RectTransform pointA = GetRopeStartpoint(rope.idA);
        RectTransform pointB = GetRopeEndpoint(rope.idB);
        
        // Если одна из точек не найдена, пробуем в обратном направлении
        if (pointA == null || pointB == null)
        {
            pointA = GetRopeStartpoint(rope.idB);
            pointB = GetRopeEndpoint(rope.idA);
        }
        
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning($"[Board] Не удалось создать нить: pointA={pointA}, pointB={pointB}");
            return;
        }

        BoardRopeUI ui = Instantiate(ropePrefab, ropesLayer);
        ui.Initialize(pointA, pointB);
        spawnedRopes[key] = ui;
        
        Debug.Log($"[Board] Создана нить: {rope.idA} -> {rope.idB}, key={key}");
    }

    // --- НОВОЕ: Обработчики событий верёвок ---
    private void OnRopeAdded(BoardRope rope)
    {
        if (isOpen) SpawnRope(rope);
    }

    private void OnRopeRemoved(BoardRope rope)
    {
        string key = RopeKey(rope.idA, rope.idB);
        if (spawnedRopes.TryGetValue(key, out var ui))
        {
            Destroy(ui.gameObject);
            spawnedRopes.Remove(key);
        }
    }

    // --- НОВОЕ: Методы управления верёвкой ---
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

        if (string.IsNullOrEmpty(toId) || toId == ropeSourceId) return;
        InvestigationBoardManager.instance.TryAddRope(ropeSourceId, toId);
    }

    // ??? Слот — принять стикер ??????????????????????????????????????

    public void TryDropStickerInSlot(string stickerId, StickerTag slot)
    {
        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;

        // Если стикер ещё не спавнен на доске — спавним его
        if (!spawnedStickers.ContainsKey(stickerId))
        {
            StickerData data = mgr.GetStickerById(stickerId);
            if (data != null)
                SpawnStickerOnBoard(data);
        }

        // Теперь пытаемся разместить в слот
        bool ok = mgr.TryPlaceInSlot(stickerId, slot);

        if (!ok && spawnedStickers.TryGetValue(stickerId, out var s))
            s.PlayRejectEffect();
    }

    // --- НОВОЕ: Получить ближайший слот в радиусе текущей позиции ---
    public SlotAnchorUI FindNearestSlotInRadius(Vector2 worldPosition, float maxDistance = 200f)
    {
        SlotAnchorUI nearest = null;
        float closestDistance = maxDistance;

        foreach (var slot in spawnedSlots.Values)
        {
            if (slot.IsPositionInDropZone(worldPosition))
            {
                
                float distance = Vector2.Distance(worldPosition, slot.GetSlotWorldCenter());
                if (distance < closestDistance)
                {
                    nearest = slot;
                    closestDistance = distance;
                    nearest.gameObject.GetComponent<UIMultilineConnector>().AddTarget(gameObject.GetComponent<RectTransform>());
                }
            }
        }

        return nearest;
    }

    // ??? Детальная панель ????????????????????????????????????????????

    public void ShowDetail(string id)
    {
        var mgr = InvestigationBoardManager.instance;
        StickerData data = mgr.GetStickerById(id);
        if (data == null) return;

        detailPanel.SetActive(true);
        detailTitle.text = data.title;
        detailBody.text = data.bodyText;

        foreach (Transform t in detailCommentsRoot) Destroy(t.gameObject);

        foreach (var c in mgr.GetVisibleComments(id))
        {
            var entry = Instantiate(commentPrefab, detailCommentsRoot);
            entry.Initialize(c);
        }
    }

    public void HideDetail() => detailPanel.SetActive(false);

    // ??? Синтез ??????????????????????????????????????????????????????

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
        if (result == null) return;

        SynthesisPopupUI popup = Instantiate(synthesisPrefab, transform);
        popup.Show(result);
    }

    private void OnConclusionUnlocked(CaseConclusion c) { }

    // ??? События размещения/снятия ????????????????????????????????????

    private void OnStickerPlaced(StickerTag tag, string stickerId)
    {
        if (!isOpen) return;
        if (!spawnedStickers.TryGetValue(stickerId, out var s)) return;
        string key = tag.ToString();
        if (!spawnedSlots.TryGetValue(key, out var anchor)) return;

        var rt = s.GetComponent<RectTransform>();
        //FUCK this magnetism
        //rt.anchoredPosition = anchor.GetComponent<RectTransform>().anchoredPosition;
        s.transform.SetAsLastSibling();

        anchor.RefreshCount();
        
        // --- НОВОЕ: Пересчитываем нити — от стикера к центру слота ---
        RefreshRopesForSticker(stickerId);
    }

    private void OnStickerUnplaced(StickerTag tag, string stickerId)
    {
        if (!isOpen) return;
        if (!spawnedStickers.TryGetValue(stickerId, out var s)) return;

        var pos = InvestigationBoardManager.instance.GetPosition(stickerId);
        s.GetComponent<RectTransform>().anchoredPosition = pos;

        string key = tag.ToString();
        if (spawnedSlots.TryGetValue(key, out var anchor))
            anchor.RefreshCount();
        
        // --- НОВОЕ: Пересчитываем нити после снятия ---
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
                if (spawnedRopes.ContainsKey(key))
                {
                    Destroy(spawnedRopes[key].gameObject);
                    spawnedRopes.Remove(key);
                }
                
                // Пересоздаём нить — она автоматически подключится к центру слота если нужно
                SpawnRope(rope);
            }
        }
    }

    // ??? ИНВЕНТАРЬ — Фильтры ??????????????????????????????????????????

    private void BuildInventoryFilterButtons()
    {
        if (filterRow == null || filterButtonPrefab == null) return;

        SpawnInventoryFilterButton("ВСЕ", null);

        foreach (StickerTag tag in System.Enum.GetValues(typeof(StickerTag)))
        {
            if (tag == StickerTag.Fact) continue;
            StickerTag captured = tag;
            SpawnInventoryFilterButton(tag.ToString().ToUpper(), captured);
        }

        SpawnInventoryFilterButton("ФАКТЫ", StickerTag.Fact);
    }

    private void SpawnInventoryFilterButton(string label, StickerTag? tag)
    {
        Button btn = Instantiate(filterButtonPrefab, filterRow);
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp) tmp.text = label;

        StickerTag? captured = tag;
        btn.onClick.AddListener(() =>
        {
            activeInventoryFilter = captured;
            RefreshInventory();
            UpdateInventoryFilterHighlight(btn);
        });
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

    // ??? ИНВЕНТАРЬ — Обновление списка ???????????????????????????????????

    private void RefreshInventory()
    {
        foreach (var item in spawnedInventoryItems) Destroy(item.gameObject);
        spawnedInventoryItems.Clear();

        var mgr = InvestigationBoardManager.instance;
        if (mgr == null) return;

        // ТОЛЬКО инвентарь, НЕ слоты
        List<string> ids = mgr.GetInventory();

        foreach (var id in ids)
        {
            StickerData data = mgr.GetStickerById(id);
            if (data == null) continue;

            if (activeInventoryFilter.HasValue && data.tag != activeInventoryFilter.Value) continue;

            StickerListItemUI item = Instantiate(inventoryItemPrefab, inventoryContentRoot);
            item.Initialize(data);
            spawnedInventoryItems.Add(item);
        }
    }

    // ??? ИНВЕНТАРЬ — Открыть/Закрыть панель ????????????????????????
    public void RemoveStickerFromBoard(string stickerId)
    {
        if (spawnedStickers.TryGetValue(stickerId, out var s))
        {
            Destroy(s.gameObject);
            spawnedStickers.Remove(stickerId);
            Debug.Log($"[Board] Визуал стикера '{stickerId}' удалён с доски");
        }

        // Также удаляем все нити, связанные с этим стикером
        var ropesToRemove = new List<string>();
        foreach (var kvp in spawnedRopes)
        {
            var allRopes = InvestigationBoardManager.instance.GetRopes();
            var rope = allRopes.Find(r => RopeKey(r.idA, r.idB) == kvp.Key);

            if (rope != null && (rope.idA == stickerId || rope.idB == stickerId))
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

        // --- НОВОЕ: пересчитываем инвентарь ---
        RefreshInventory();
    }
    private void ToggleInventoryPanel()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryPanel) inventoryPanel.SetActive(isInventoryOpen);
        if (isInventoryOpen) RefreshInventory();

        if (inventoryToggleButtonText)
            inventoryToggleButtonText.text = isInventoryOpen ? "? УЛИКИ" : "? УЛИКИ";
    }

    // ??? Утилиты ????????????????????????????????????????????????????

    private string RopeKey(string a, string b) =>
        string.Compare(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

    // --- НОВОЕ: публичные методы для StickerUI ---
    public bool TryGetSlotAnchor(string slotKey, out SlotAnchorUI anchor)
    {
        return spawnedSlots.TryGetValue(slotKey, out anchor);
    }

    public bool IsDraggingRope => isDraggingRope;
    public string RopeSourceId => ropeSourceId;
    public RectTransform BoardCanvas => boardCanvas;
}