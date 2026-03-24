using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DIALOGUE;

/// <summary>
/// Менеджер доски расследования.
/// Хранит состояние: какие стикеры открыты, где лежат, какие связи построены.
/// Один большой кейс с зонами по расследованиям.
///
/// Команды в txt:
///   add_sticker(kyle_file)          — добавить стикер в инвентарь доски
///   add_fact(fury kyle_motive)      — добавить факт от фантома
///   remove_sticker(kyle_file)       — убрать стикер
/// </summary>
public class InvestigationBoardManager : MonoBehaviour
{
    public static InvestigationBoardManager instance { get; private set; }

    // ??? Inspector ???????????????????????????????????????????????????

    [Title("Кейс")]
    [SerializeField] private CaseData activeCase;

    [Title("Все стикеры проекта")]
    [InfoBox("Перетащи сюда все StickerData SO")]
    [SerializeField] private List<StickerData> allStickers = new List<StickerData>();

    // ??? Runtime состояние ???????????????????????????????????????????

    // Стикеры в инвентаре (добавлены но ещё не в слоте)
    private List<string> inventoryIds = new List<string>();

    // Стикеры в слотах: tag ? список id
    private Dictionary<StickerTag, List<string>> slotContents
        = new Dictionary<StickerTag, List<string>>();

    // Позиции стикеров на доске (локальные координаты канваса)
    private Dictionary<string, Vector2> stickerPositions
        = new Dictionary<string, Vector2>();

    // Верёвки
    private List<BoardRope> ropes = new List<BoardRope>();

    // Полученные выводы
    private List<string> unlockedConclusionIds = new List<string>();

    // События для UI
    public System.Action<StickerData> onStickerAdded;
    public System.Action<string> onStickerRemoved;
    public System.Action<StickerTag, string> onStickerPlaced;    // слот, id
    public System.Action<StickerTag, string> onStickerUnplaced;
    public System.Action<BoardRope> onRopeAdded;
    public System.Action<BoardRope> onRopeRemoved;
    public System.Action<CaseConclusion> onConclusionUnlocked;
    public System.Action onSynthesisReady;    // все слоты заполнены

    // ??? Lifecycle ???????????????????????????????????????????????????

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }

        // Инициализируем пустые слоты
        foreach (StickerTag tag in System.Enum.GetValues(typeof(StickerTag)))
            slotContents[tag] = new List<string>();
    }

    // ??? Стикеры ?????????????????????????????????????????????????????

    public void AddSticker(string id)
    {
        if (inventoryIds.Contains(id) || IsPlacedAnywhere(id))
        {
            Debug.Log($"[Board] Стикер '{id}' уже на доске");
            return;
        }

        StickerData sticker = GetStickerById(id);
        if (sticker == null)
        {
            Debug.LogWarning($"[Board] Стикер '{id}' не найден в allStickers");
            return;
        }

        // Проверяем синхронизацию для фактов фантомов
        if (sticker.isPhantomFact)
        {
            float sync = PhantomManager.instance?.GetSync(sticker.phantomSource) ?? 0f;
            if (sync < sticker.requiredSync)
            {
                Debug.Log($"[Board] Факт '{id}' недоступен — нужно {sticker.requiredSync}% {sticker.phantomSource}, сейчас {sync}%");
                return;
            }
        }

        inventoryIds.Add(id);

        // Начальная позиция — в зоне инвентаря
        if (!stickerPositions.ContainsKey(id))
            stickerPositions[id] = new Vector2(
                Random.Range(-800f, -400f),
                Random.Range(-200f, 200f)
            );

        Debug.Log($"[Board] Добавлен стикер: {sticker.title} [{sticker.tag}]");
        onStickerAdded?.Invoke(sticker);
    }

    public void AddFact(string phantomName, string stickerId)
    {
        // Проверяем что фантом имеет достаточно синхронизации
        if (!System.Enum.TryParse<PhantomManager.PhantomType>(
            phantomName, true, out var phantomType))
        {
            Debug.LogWarning($"[Board] Неизвестный фантом: {phantomName}");
            return;
        }

        AddSticker(stickerId);
    }

    public void RemoveSticker(string id)
    {
        inventoryIds.Remove(id);

        foreach (var list in slotContents.Values)
            list.Remove(id);

        // Удаляем верёвки этого стикера
        ropes.RemoveAll(r =>
        {
            if (r.idA == id || r.idB == id)
            {
                onRopeRemoved?.Invoke(r);
                return true;
            }
            return false;
        });

        onStickerRemoved?.Invoke(id);
    }

    // ??? Размещение в слоте ??????????????????????????????????????????

    public bool TryPlaceInSlot(string stickerId, StickerTag targetSlot)
    {
        StickerData sticker = GetStickerById(stickerId);
        if (sticker == null) return false;

        SlotConfig slotCfg = activeCase?.GetSlot(targetSlot);
        if (slotCfg == null) return false;

        // Проверяем тег — стикер должен совпадать или быть Fact в Why
        bool tagMatch = sticker.tag == targetSlot;
        bool factInWhy = sticker.tag == StickerTag.Fact
                         && targetSlot == StickerTag.Why
                         && slotCfg.acceptsFacts;

        if (!tagMatch && !factInWhy)
        {
            Debug.Log($"[Board] Нельзя положить [{sticker.tag}] в слот [{targetSlot}]");
            return false;
        }

        // Лимит слота
        if (slotCfg.maxStickers > 0 &&
            slotContents[targetSlot].Count >= slotCfg.maxStickers)
        {
            Debug.Log($"[Board] Слот [{targetSlot}] заполнен (лимит {slotCfg.maxStickers})");
            return false;
        }

        // Убираем из предыдущего слота если был там
        UnplaceFromSlots(stickerId);

        slotContents[targetSlot].Add(stickerId);
        inventoryIds.Remove(stickerId);

        onStickerPlaced?.Invoke(targetSlot, stickerId);
        Debug.Log($"[Board] {sticker.title} ? слот {targetSlot}");

        CheckSynthesisReady();
        return true;
    }

    public void UnplaceFromSlots(string stickerId)
    {
        foreach (var kvp in slotContents)
        {
            if (kvp.Value.Contains(stickerId))
            {
                kvp.Value.Remove(stickerId);
                if (!inventoryIds.Contains(stickerId))
                    inventoryIds.Add(stickerId);
                onStickerUnplaced?.Invoke(kvp.Key, stickerId);
                return;
            }
        }
    }

    // ??? Позиции ?????????????????????????????????????????????????????

    public Vector2 GetPosition(string id) =>
        stickerPositions.TryGetValue(id, out var p) ? p : Vector2.zero;

    public void SetPosition(string id, Vector2 pos) =>
        stickerPositions[id] = pos;

    // ??? Верёвки ?????????????????????????????????????????????????????

    public RopeResult TryAddRope(string idA, string idB)
    {
        if (idA == idB) return RopeResult.Invalid;
        if (HasRope(idA, idB)) return RopeResult.AlreadyExists;

        BoardRope rope = new BoardRope { idA = idA, idB = idB };
        ropes.Add(rope);
        onRopeAdded?.Invoke(rope);

        Debug.Log($"[Board] Верёвка: {idA} ? {idB}");
        return RopeResult.Ok;
    }

    public void RemoveRope(string idA, string idB)
    {
        var rope = ropes.Find(r =>
            (r.idA == idA && r.idB == idB) ||
            (r.idA == idB && r.idB == idA));

        if (rope == null) return;
        ropes.Remove(rope);
        onRopeRemoved?.Invoke(rope);
    }

    public bool HasRope(string idA, string idB) =>
        ropes.Exists(r =>
            (r.idA == idA && r.idB == idB) ||
            (r.idA == idB && r.idB == idA));

    public List<BoardRope> GetRopes() => new List<BoardRope>(ropes);

    // ??? Синтез ??????????????????????????????????????????????????????

    private void CheckSynthesisReady()
    {
        if (activeCase == null) return;

        foreach (var slot in activeCase.GetAllSlots())
        {
            if (slotContents[slot.tag].Count == 0)
                return;
        }

        Debug.Log("[Board] ? Все слоты заполнены — Синтез доступен!");
        onSynthesisReady?.Invoke();
    }

    /// <summary>
    /// Запустить синтез — определить вывод по содержимому WHY.
    /// Возвращает первый подходящий CaseConclusion или null.
    /// </summary>
    public CaseConclusion RunSynthesis()
    {
        if (activeCase == null) return null;

        List<string> whyIds = slotContents[StickerTag.Why];

        foreach (var conclusion in activeCase.conclusions)
        {
            if (unlockedConclusionIds.Contains(conclusion.conclusionId)) continue;
            if (!conclusion.IsSatisfied(whyIds)) continue;

            // Нашли вывод
            unlockedConclusionIds.Add(conclusion.conclusionId);

            // Бонусы синхронизации
            foreach (var bonus in conclusion.syncBonuses)
                PhantomManager.instance?.Raise(bonus.phantom, bonus.amount);

            // Флаг
            if (!string.IsNullOrEmpty(conclusion.setsFlag))
            {
                if (!VariableStore.HasVariable(conclusion.setsFlag))
                    VariableStore.CreateVariable(conclusion.setsFlag, true);
                else
                    VariableStore.TrySetValue(conclusion.setsFlag, true);
            }

            Debug.Log($"[Board] ?? Вывод: {conclusion.title}");
            onConclusionUnlocked?.Invoke(conclusion);
            return conclusion;
        }

        Debug.Log("[Board] Вывод не найден — проверь WHY слот");
        return null;
    }

    // ??? Геттеры для UI ??????????????????????????????????????????????

    public List<string> GetInventory() => new List<string>(inventoryIds);
    public List<string> GetSlotContents(StickerTag tag) =>
        slotContents.TryGetValue(tag, out var list)
            ? new List<string>(list)
            : new List<string>();

    public bool IsPlacedAnywhere(string id)
    {
        foreach (var list in slotContents.Values)
            if (list.Contains(id)) return true;
        return false;
    }

    public bool IsSynthesisReady()
    {
        if (activeCase == null) return false;
        foreach (var slot in activeCase.GetAllSlots())
            if (slotContents[slot.tag].Count == 0) return false;
        return true;
    }

    public StickerData GetStickerById(string id)
    {
        foreach (var s in allStickers)
            if (s != null && s.stickerId == id) return s;
        return null;
    }

    public CaseData GetActiveCase() => activeCase;

    // Видимые комментарии фантомов к стикеру
    public List<PhantomComment> GetVisibleComments(string stickerId)
    {
        StickerData s = GetStickerById(stickerId);
        if (s == null) return new List<PhantomComment>();

        var visible = new List<PhantomComment>();
        foreach (var c in s.phantomComments)
        {
            float sync = PhantomManager.instance?.GetSync(c.phantom) ?? 0f;
            if (sync >= c.requiredSync) visible.Add(c);
        }
        return visible;
    }

    // ??? Editor ??????????????????????????????????????????????????????

    [Button("Состояние доски")]
    private void EditorPrint()
    {
        Debug.Log($"[Board] Инвентарь: {string.Join(", ", inventoryIds)}");
        foreach (var kvp in slotContents)
            if (kvp.Value.Count > 0)
                Debug.Log($"[Board] {kvp.Key}: {string.Join(", ", kvp.Value)}");
        Debug.Log($"[Board] Верёвок: {ropes.Count}");
        Debug.Log($"[Board] Синтез готов: {IsSynthesisReady()}");
    }

    // Добавь этот метод в InvestigationBoardManager:
    public StickerTag? GetStickerSlot(string stickerId)
    {
        foreach (StickerTag tag in System.Enum.GetValues(typeof(StickerTag)))
        {
            if (GetSlotContents(tag).Contains(stickerId))
                return tag;
        }
        return null;
    }
}

// ??? Вспомогательные типы ????????????????????????????????????????????

[System.Serializable]
public class BoardRope
{
    public string idA;
    public string idB;
}

public enum RopeResult { Ok, AlreadyExists, Invalid }