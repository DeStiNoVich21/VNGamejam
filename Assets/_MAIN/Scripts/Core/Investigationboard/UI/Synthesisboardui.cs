using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

/// <summary>
/// UI доски улик. Открывается/закрывается кнопкой.
/// Карточки перетаскиваются. Верёвки тянутся вручную.
/// Магнетизм подсвечивает совместимые улики при перетаскивании верёвки.
///
/// Иерархия в Canvas:
///   SynthesisBoard (этот компонент)
///     BoardRoot
///       Background (RawImage — пробковая текстура)
///       CardsContainer (пустой RectTransform)
///       LinesContainer (пустой RectTransform — под карточками)
///     DetailPanel (правая панель — описание улики)
///     ThoughtPopup (всплывает при получении мысли)
/// </summary>
public class SynthesisBoardUI : MonoBehaviour
{
    public static SynthesisBoardUI instance { get; private set; }

    // ??? Inspector ???????????????????????????????????????????????????

    [Title("Корневые объекты")]
    [SerializeField] private GameObject boardRoot;
    [SerializeField] private RectTransform cardsContainer;
    [SerializeField] private RectTransform linesContainer;
    [SerializeField] private CanvasGroup boardCanvasGroup;

    [Title("Префабы")]
    [SerializeField] private ClueCardUI clueCardPrefab;
    [SerializeField] private RopeLineUI ropeLinePrefab;
    [SerializeField] private ThoughtPopupUI thoughtPopupPrefab;

    [Title("Детальная панель")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailImage;
    [SerializeField] private TextMeshProUGUI detailTitle;
    [SerializeField] private TextMeshProUGUI detailTier;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private Transform detailCommentsContainer;
    [SerializeField] private PhantomCommentEntryUI commentEntryPrefab;

    [Title("Настройки")]
    [SerializeField] private float openSpeed = 8f;
    [SerializeField] private float magnetDistance = 120f;

    // Цвета тиров карточек
    [SerializeField] private Color colorLead = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color colorConnection = new Color(0.3f, 0.5f, 0.8f);
    [SerializeField] private Color colorClue = new Color(0.9f, 0.8f, 0.2f);
    [SerializeField] private Color colorTruth = new Color(0.9f, 0.2f, 0.2f);

    // ??? Runtime ?????????????????????????????????????????????????????

    private bool isOpen = false;
    private Dictionary<string, ClueCardUI> spawnedCards = new Dictionary<string, ClueCardUI>();
    private Dictionary<string, RopeLineUI> spawnedLines = new Dictionary<string, RopeLineUI>();

    // Состояние перетаскивания верёвки
    private bool isDraggingRope = false;
    private string ropeSourceId = "";
    private RopeLineUI activeRopeDraft = null;

    private Coroutine co_fade = null;

    // ??? Lifecycle ???????????????????????????????????????????????????

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        boardRoot.SetActive(false);
        boardCanvasGroup.alpha = 0f;
        detailPanel.SetActive(false);
    }

    private void OnEnable()
    {
        var mgr = SynthesisBoardManager.instance;
        if (mgr == null) return;

        mgr.onClueAdded += OnClueAdded;
        mgr.onClueRemoved += OnClueRemoved;
        mgr.onConnectionAdded += OnConnectionAdded;
        mgr.onConnectionRemoved += OnConnectionRemoved;
        mgr.onThoughtUnlocked += OnThoughtUnlocked;
    }

    private void OnDisable()
    {
        var mgr = SynthesisBoardManager.instance;
        if (mgr == null) return;

        mgr.onClueAdded -= OnClueAdded;
        mgr.onClueRemoved -= OnClueRemoved;
        mgr.onConnectionAdded -= OnConnectionAdded;
        mgr.onConnectionRemoved -= OnConnectionRemoved;
        mgr.onThoughtUnlocked -= OnThoughtUnlocked;
    }

    // ??? Открыть / Закрыть ???????????????????????????????????????????

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        boardRoot.SetActive(true);
        RefreshAllCards();
        RefreshAllLines();

        if (co_fade != null) StopCoroutine(co_fade);
        co_fade = StartCoroutine(FadeBoard(1f));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        detailPanel.SetActive(false);

        if (co_fade != null) StopCoroutine(co_fade);
        co_fade = StartCoroutine(FadeBoard(0f, () => boardRoot.SetActive(false)));
    }

    private IEnumerator FadeBoard(float target, System.Action onDone = null)
    {
        while (Mathf.Abs(boardCanvasGroup.alpha - target) > 0.01f)
        {
            boardCanvasGroup.alpha = Mathf.MoveTowards(
                boardCanvasGroup.alpha, target, openSpeed * Time.deltaTime);
            yield return null;
        }
        boardCanvasGroup.alpha = target;
        onDone?.Invoke();
        co_fade = null;
    }

    // ??? Карточки ????????????????????????????????????????????????????

    private void RefreshAllCards()
    {
        var mgr = SynthesisBoardManager.instance;
        foreach (var id in mgr.GetDiscoveredIds())
        {
            if (!spawnedCards.ContainsKey(id))
                SpawnCard(mgr.GetClueById(id));
        }
    }

    private void SpawnCard(ClueData clue)
    {
        if (clue == null || spawnedCards.ContainsKey(clue.id)) return;

        ClueCardUI card = Instantiate(clueCardPrefab, cardsContainer);
        card.Initialize(clue, this);

        // Восстанавливаем позицию
        Vector2 pos = SynthesisBoardManager.instance.GetCardPosition(clue.id);
        card.GetComponent<RectTransform>().anchoredPosition = pos;

        // Цвет по тиру
        card.SetTierColor(GetTierColor(clue.tier));

        spawnedCards[clue.id] = card;
    }

    private void OnClueAdded(ClueData clue)
    {
        if (isOpen) SpawnCard(clue);
    }

    private void OnClueRemoved(string id)
    {
        if (spawnedCards.TryGetValue(id, out var card))
        {
            Destroy(card.gameObject);
            spawnedCards.Remove(id);
        }
    }

    // ??? Верёвки ?????????????????????????????????????????????????????

    private void RefreshAllLines()
    {
        foreach (var conn in SynthesisBoardManager.instance.GetConnections())
            SpawnLine(conn);
    }

    private void SpawnLine(ClueConnection conn)
    {
        string key = LineKey(conn.clueIdA, conn.clueIdB);
        if (spawnedLines.ContainsKey(key)) return;
        if (!spawnedCards.ContainsKey(conn.clueIdA)) return;
        if (!spawnedCards.ContainsKey(conn.clueIdB)) return;

        RopeLineUI line = Instantiate(ropeLinePrefab, linesContainer);
        line.Initialize(
            spawnedCards[conn.clueIdA].GetComponent<RectTransform>(),
            spawnedCards[conn.clueIdB].GetComponent<RectTransform>(),
            conn.isValid
        );
        spawnedLines[key] = line;
    }

    private void OnConnectionAdded(ClueConnection conn)
    {
        if (isOpen) SpawnLine(conn);
    }

    private void OnConnectionRemoved(ClueConnection conn)
    {
        string key = LineKey(conn.clueIdA, conn.clueIdB);
        if (spawnedLines.TryGetValue(key, out var line))
        {
            Destroy(line.gameObject);
            spawnedLines.Remove(key);
        }
    }

    // ??? Перетаскивание верёвки (вызывается из ClueCardUI) ???????????

    public void BeginRopeDrag(string sourceId)
    {
        isDraggingRope = true;
        ropeSourceId = sourceId;

        // Создаём черновую верёвку
        activeRopeDraft = Instantiate(ropeLinePrefab, linesContainer);
        activeRopeDraft.InitializeDraft(
            spawnedCards[sourceId].GetComponent<RectTransform>()
        );

        // Подсвечиваем магнитные цели
        var targets = SynthesisBoardManager.instance.GetMagneticTargets(sourceId);
        foreach (var id in targets)
            if (spawnedCards.TryGetValue(id, out var card))
                card.SetMagnetHighlight(true);
    }

    public void UpdateRopeDrag(Vector2 screenPos)
    {
        if (!isDraggingRope || activeRopeDraft == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cardsContainer, screenPos, null, out Vector2 localPos);

        activeRopeDraft.UpdateDraftEnd(localPos);

        // Магнетизм — если близко к валидной цели
        var targets = SynthesisBoardManager.instance.GetMagneticTargets(ropeSourceId);
        foreach (var id in targets)
        {
            if (!spawnedCards.TryGetValue(id, out var card)) continue;
            Vector2 cardPos = card.GetComponent<RectTransform>().anchoredPosition;
            float dist = Vector2.Distance(localPos, cardPos);
            if (dist < magnetDistance)
            {
                // Притягиваем конец верёвки к карточке
                activeRopeDraft.UpdateDraftEnd(cardPos);
                break;
            }
        }
    }

    public void EndRopeDrag(string targetId)
    {
        if (!isDraggingRope) return;

        // Убираем подсветку
        var targets = SynthesisBoardManager.instance.GetMagneticTargets(ropeSourceId);
        foreach (var id in targets)
            if (spawnedCards.TryGetValue(id, out var card))
                card.SetMagnetHighlight(false);

        // Убираем черновую верёвку
        if (activeRopeDraft != null) Destroy(activeRopeDraft.gameObject);
        activeRopeDraft = null;

        isDraggingRope = false;

        if (string.IsNullOrEmpty(targetId) || targetId == ropeSourceId) return;

        // Пробуем соединить
        ConnectionResult result = SynthesisBoardManager.instance.TryConnect(ropeSourceId, targetId);
        HandleConnectionResult(result, ropeSourceId, targetId);
    }

    private void HandleConnectionResult(ConnectionResult result, string idA, string idB)
    {
        switch (result)
        {
            case ConnectionResult.ValidMatch:
                Debug.Log($"[Board UI] ? Связь {idA} ? {idB} — есть комбинация!");
                // Карточки вибрируют — притяжение
                if (spawnedCards.TryGetValue(idA, out var cA)) cA.PlayMagnetEffect(attract: true);
                if (spawnedCards.TryGetValue(idB, out var cB)) cB.PlayMagnetEffect(attract: true);
                break;

            case ConnectionResult.NoMatch:
                Debug.Log($"[Board UI] ? Связь {idA} ? {idB} — комбинации нет");
                // Карточки отталкиваются
                if (spawnedCards.TryGetValue(idA, out var rA)) rA.PlayMagnetEffect(attract: false);
                if (spawnedCards.TryGetValue(idB, out var rB)) rB.PlayMagnetEffect(attract: false);
                break;

            case ConnectionResult.AlreadyExists:
                break;
        }
    }

    // ??? Детальная панель ????????????????????????????????????????????

    public void ShowDetail(string clueId)
    {
        ClueData clue = SynthesisBoardManager.instance.GetClueById(clueId);
        if (clue == null) return;

        detailPanel.SetActive(true);
        detailImage.sprite = clue.image;
        detailTitle.text = clue.title;
        detailTier.text = clue.tier.ToString();
        detailDescription.text = clue.description;

        // Очищаем старые комментарии
        foreach (Transform child in detailCommentsContainer)
            Destroy(child.gameObject);

        // Добавляем видимые комментарии фантомов
        var comments = SynthesisBoardManager.instance.GetVisibleComments(clueId);
        foreach (var comment in comments)
        {
            var entry = Instantiate(commentEntryPrefab, detailCommentsContainer);
            entry.Initialize(comment);
        }
    }

    public void HideDetail() => detailPanel.SetActive(false);

    // ??? Мысли ???????????????????????????????????????????????????????

    private void OnThoughtUnlocked(ClueCombination combo)
    {
        ThoughtPopupUI popup = Instantiate(thoughtPopupPrefab, transform);
        popup.Show(combo);
    }

    // ??? Утилиты ?????????????????????????????????????????????????????

    public Color GetTierColor(ClueTier tier)
    {
        switch (tier)
        {
            case ClueTier.Lead: return colorLead;
            case ClueTier.Connection: return colorConnection;
            case ClueTier.Clue: return colorClue;
            case ClueTier.Truth: return colorTruth;
            default: return Color.white;
        }
    }

    private string LineKey(string a, string b)
    {
        // Нормализуем порядок чтобы "a-b" == "b-a"
        return string.Compare(a, b) < 0 ? $"{a}-{b}" : $"{b}-{a}";
    }

    public bool IsDraggingRope => isDraggingRope;
    public string RopeSourceId => ropeSourceId;

    // ??? Editor ??????????????????????????????????????????????????????

    [Button("Открыть доску")]
    private void EditorOpen() { if (Application.isPlaying) Open(); }

    [Button("Закрыть доску")]
    private void EditorClose() { if (Application.isPlaying) Close(); }
}