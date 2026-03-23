using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// UI доски расследования.
/// Одна большая доска — навигация зумом и перетаскиванием как в Miro/Канве.
/// Якоря слотов 5W+H фиксированы на доске.
/// Стикеры перетаскиваются свободно.
/// Верёвки тянутся ПКМ.
/// </summary>
public class InvestigationBoardUI : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IScrollHandler
{
    public static InvestigationBoardUI instance { get; private set; }

    // ??? Inspector ???????????????????????????????????????????????????

    [Title("Корень")]
    [SerializeField] private GameObject boardRoot;
    [SerializeField] private CanvasGroup boardCG;
    [SerializeField] private RectTransform boardCanvas;   // двигается и масштабируется
    [SerializeField] private RectTransform stickersLayer; // стикеры
    [SerializeField] private RectTransform ropesLayer;    // верёвки (под стикерами)
    [SerializeField] private RectTransform slotsLayer;    // якоря слотов

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

    // ??? Runtime ?????????????????????????????????????????????????????

    private bool isOpen = false;
    private float currentZoom = 1f;
    private Vector2 dragStart;
    private bool isPanningBoard = false;

    private Dictionary<string, StickerUI> spawnedStickers = new();
    private Dictionary<string, SlotAnchorUI> spawnedSlots = new();
    private Dictionary<string, BoardRopeUI> spawnedRopes = new();

    // Состояние верёвки
    private bool isDraggingRope = false;
    private string ropeSourceId = "";
    private BoardRopeUI draftRope = null;

    private Coroutine co_fade;

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
    }

    // ??? Открыть / Закрыть ???????????????????????????????????????????

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        boardRoot.SetActive(true);
        BuildSlotAnchors();
        RefreshAllStickers();
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

    // ??? Стикеры ?????????????????????????????????????????????????????

    private void RefreshAllStickers()
    {
        var mgr = InvestigationBoardManager.instance;
        foreach (var id in mgr.GetInventory())
            if (!spawnedStickers.ContainsKey(id))
                SpawnSticker(mgr.GetStickerById(id));

        // Также стикеры которые уже в слотах
        foreach (StickerTag tag in System.Enum.GetValues(typeof(StickerTag)))
            foreach (var id in mgr.GetSlotContents(tag))
                if (!spawnedStickers.ContainsKey(id))
                    SpawnSticker(mgr.GetStickerById(id));
    }

    private void SpawnSticker(StickerData data)
    {
        if (data == null || spawnedStickers.ContainsKey(data.stickerId)) return;

        StickerUI sticker = Instantiate(stickerPrefab, stickersLayer);
        sticker.Initialize(data, this);

        Vector2 pos = InvestigationBoardManager.instance.GetPosition(data.stickerId);
        sticker.GetComponent<RectTransform>().anchoredPosition = pos;

        spawnedStickers[data.stickerId] = sticker;
    }

    private void OnStickerAdded(StickerData data) { if (isOpen) SpawnSticker(data); }

    private void OnStickerRemoved(string id)
    {
        if (!spawnedStickers.TryGetValue(id, out var s)) return;
        Destroy(s.gameObject);
        spawnedStickers.Remove(id);
    }

    // ??? Верёвки ?????????????????????????????????????????????????????

    private void RefreshAllRopes()
    {
        foreach (var rope in InvestigationBoardManager.instance.GetRopes())
            SpawnRope(rope);
    }

    private void SpawnRope(BoardRope rope)
    {
        string key = RopeKey(rope.idA, rope.idB);
        if (spawnedRopes.ContainsKey(key)) return;
        if (!spawnedStickers.ContainsKey(rope.idA)) return;
        if (!spawnedStickers.ContainsKey(rope.idB)) return;

        BoardRopeUI ui = Instantiate(ropePrefab, ropesLayer);
        ui.Initialize(
            spawnedStickers[rope.idA].GetComponent<RectTransform>(),
            spawnedStickers[rope.idB].GetComponent<RectTransform>()
        );
        spawnedRopes[key] = ui;
    }

    private void OnRopeAdded(BoardRope rope) { if (isOpen) SpawnRope(rope); }
    private void OnRopeRemoved(BoardRope rope)
    {
        string key = RopeKey(rope.idA, rope.idB);
        if (!spawnedRopes.TryGetValue(key, out var ui)) return;
        Destroy(ui.gameObject);
        spawnedRopes.Remove(key);
    }

    // ??? Верёвка — перетаскивание (вызывается из StickerUI) ??????????

    public void BeginRopeDrag(string fromId)
    {
        isDraggingRope = true;
        ropeSourceId = fromId;

        draftRope = Instantiate(ropePrefab, ropesLayer);
        draftRope.InitializeDraft(
            spawnedStickers[fromId].GetComponent<RectTransform>()
        );
    }

    public void EndRopeDrag(string toId)
    {
        if (draftRope != null) { Destroy(draftRope.gameObject); draftRope = null; }
        isDraggingRope = false;

        if (string.IsNullOrEmpty(toId) || toId == ropeSourceId) return;

        InvestigationBoardManager.instance.TryAddRope(ropeSourceId, toId);
    }

    // ??? Слот — принять стикер (вызывается из SlotAnchorUI) ??????????

    public void TryDropStickerInSlot(string stickerId, StickerTag slot)
    {
        bool ok = InvestigationBoardManager.instance.TryPlaceInSlot(stickerId, slot);

        if (!ok && spawnedStickers.TryGetValue(stickerId, out var s))
            s.PlayRejectEffect(); // дрожание — нельзя положить
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

    private void OnConclusionUnlocked(CaseConclusion c) { /* обрабатывается попапом */ }

    // ??? Утилиты ?????????????????????????????????????????????????????

    private string RopeKey(string a, string b) =>
        string.Compare(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";

    public bool IsDraggingRope => isDraggingRope;
    public string RopeSourceId => ropeSourceId;
    public RectTransform BoardCanvas => boardCanvas;
}