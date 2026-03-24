using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DIALOGUE;

/// <summary>
/// Центральный менеджер доски.
/// Хранит состояние: какие улики открыты, какие связи построены, какие мысли получены.
/// UI-доска читает данные отсюда.
///
/// Команды в txt:
///   add_clue(bloody_knife)
///   remove_clue(bloody_knife)
/// </summary>
public class SynthesisBoardManager : MonoBehaviour
{
    public static SynthesisBoardManager instance { get; private set; }

    // ??? Данные ??????????????????????????????????????????????????????

    [Title("База улик")]
    [InfoBox("Перетащи сюда все ClueData SO из проекта")]
    [SerializeField] private List<ClueData> allClues = new List<ClueData>();

    [Title("Комбинации")]
    [SerializeField] private List<ClueCombination> allCombinations = new List<ClueCombination>();

    // ??? Состояние (runtime) ?????????????????????????????????????????

    // Открытые улики — id
    private List<string> discoveredClueIds = new List<string>();

    // Позиции карточек на доске (сохраняются между открытиями)
    private Dictionary<string, Vector2> cardPositions = new Dictionary<string, Vector2>();

    // Построенные связи
    private List<ClueConnection> connections = new List<ClueConnection>();

    // Полученные мысли
    private List<string> unlockedThoughtIds = new List<string>();

    // Событие — UI подписывается
    public System.Action<ClueData> onClueAdded;
    public System.Action<string> onClueRemoved;
    public System.Action<ClueCombination> onThoughtUnlocked;
    public System.Action<ClueConnection> onConnectionAdded;
    public System.Action<ClueConnection> onConnectionRemoved;

    // ??? Lifecycle ???????????????????????????????????????????????????

    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    // ??? Улики ???????????????????????????????????????????????????????

    public void AddClue(string clueId)
    {
        if (discoveredClueIds.Contains(clueId))
        {
            Debug.Log($"[Board] Улика '{clueId}' уже на доске");
            return;
        }

        ClueData clue = GetClueById(clueId);
        if (clue == null)
        {
            Debug.LogWarning($"[Board] Улика '{clueId}' не найдена в allClues");
            return;
        }

        discoveredClueIds.Add(clueId);

        // Начальная позиция — случайная в пределах доски
        if (!cardPositions.ContainsKey(clueId))
            cardPositions[clueId] = new Vector2(
                Random.Range(-600f, 600f),
                Random.Range(-300f, 300f)
            );

        Debug.Log($"[Board] Добавлена улика: {clue.title} [{clue.tier}]");
        onClueAdded?.Invoke(clue);
    }

    public void RemoveClue(string clueId)
    {
        if (!discoveredClueIds.Contains(clueId)) return;

        discoveredClueIds.Remove(clueId);

        // Удаляем все связи этой улики
        connections.RemoveAll(c =>
        {
            if (c.clueIdA == clueId || c.clueIdB == clueId)
            {
                onConnectionRemoved?.Invoke(c);
                return true;
            }
            return false;
        });

        onClueRemoved?.Invoke(clueId);
        Debug.Log($"[Board] Улика удалена: {clueId}");
    }

    // ??? Позиции карточек ????????????????????????????????????????????

    public Vector2 GetCardPosition(string clueId)
    {
        return cardPositions.TryGetValue(clueId, out var pos) ? pos : Vector2.zero;
    }

    public void SetCardPosition(string clueId, Vector2 pos)
    {
        cardPositions[clueId] = pos;
    }

    // ??? Связи ???????????????????????????????????????????????????????

    /// <summary>
    /// Попытаться создать связь между двумя уликами.
    /// Возвращает: 0=ок, 1=уже существует, 2=нет комбинации, 3=конфликт
    /// </summary>
    public ConnectionResult TryConnect(string idA, string idB)
    {
        if (idA == idB) return ConnectionResult.Invalid;

        // Уже соединены?
        if (HasConnection(idA, idB)) return ConnectionResult.AlreadyExists;

        // Есть ли комбинация для этой пары?
        ClueCombination combo = FindCombinationForPair(idA, idB);

        ClueConnection conn = new ClueConnection
        {
            clueIdA = idA,
            clueIdB = idB,
            isValid = combo != null
        };

        connections.Add(conn);
        onConnectionAdded?.Invoke(conn);

        // Если связь валидна — проверяем всю комбинацию
        if (combo != null)
            CheckCombination(combo);

        return combo != null ? ConnectionResult.ValidMatch : ConnectionResult.NoMatch;
    }

    public void RemoveConnection(string idA, string idB)
    {
        var conn = connections.Find(c =>
            (c.clueIdA == idA && c.clueIdB == idB) ||
            (c.clueIdA == idB && c.clueIdB == idA));

        if (conn == null) return;
        connections.Remove(conn);
        onConnectionRemoved?.Invoke(conn);
    }

    public bool HasConnection(string idA, string idB)
    {
        return connections.Exists(c =>
            (c.clueIdA == idA && c.clueIdB == idB) ||
            (c.clueIdA == idB && c.clueIdB == idA));
    }

    public List<ClueConnection> GetConnections() => new List<ClueConnection>(connections);

    // ??? Проверка магнетизма (для подсветки при перетаскивании) ??????

    /// <summary>
    /// Вернуть список улик которые "притягиваются" к данной
    /// (есть потенциальная комбинация)
    /// </summary>
    public List<string> GetMagneticTargets(string clueId)
    {
        List<string> targets = new List<string>();

        foreach (var combo in allCombinations)
        {
            bool hasThis = false;
            foreach (var c in combo.requiredClues)
                if (c != null && c.id == clueId) { hasThis = true; break; }

            if (!hasThis) continue;

            foreach (var c in combo.requiredClues)
            {
                if (c == null || c.id == clueId) continue;
                if (discoveredClueIds.Contains(c.id) && !targets.Contains(c.id))
                    targets.Add(c.id);
            }
        }
        return targets;
    }

    // ??? Комбинации и мысли ??????????????????????????????????????????

    private void CheckCombination(ClueCombination combo)
    {
        if (!combo.IsSatisfied(discoveredClueIds)) return;

        // Проверяем что все связи комбинации построены
        bool allConnected = true;
        var clues = combo.requiredClues;
        for (int i = 0; i < clues.Count; i++)
        {
            for (int j = i + 1; j < clues.Count; j++)
            {
                if (clues[i] == null || clues[j] == null) continue;
                if (!HasConnection(clues[i].id, clues[j].id))
                {
                    allConnected = false;
                    break;
                }
            }
            if (!allConnected) break;
        }

        if (!allConnected) return;
        if (unlockedThoughtIds.Contains(combo.thoughtId)) return;

        UnlockThought(combo);
    }

    private void UnlockThought(ClueCombination combo)
    {
        unlockedThoughtIds.Add(combo.thoughtId);

        // Бонусы синхронизации
        foreach (var bonus in combo.syncBonuses)
            PhantomManager.instance?.Raise(bonus.phantom, bonus.amount);

        // Флаг в VariableStore
        if (!string.IsNullOrEmpty(combo.setsFlag))
        {
            if (!VariableStore.HasVariable(combo.setsFlag))
                VariableStore.CreateVariable(combo.setsFlag, true);
            else
                VariableStore.TrySetValue(combo.setsFlag, true);
        }

        Debug.Log($"[Board] ?? Мысль получена: {combo.thoughtTitle}");
        onThoughtUnlocked?.Invoke(combo);
    }

    // ??? Геттеры для UI ??????????????????????????????????????????????

    public List<string> GetDiscoveredIds() => new List<string>(discoveredClueIds);
    public List<string> GetUnlockedThoughts() => new List<string>(unlockedThoughtIds);
    public bool HasClue(string id) => discoveredClueIds.Contains(id);
    public bool HasThought(string id) => unlockedThoughtIds.Contains(id);

    public ClueData GetClueById(string id)
    {
        foreach (var c in allClues)
            if (c != null && c.id == id) return c;
        return null;
    }

    public List<ClueData> GetAllClues() => allClues;
    public List<ClueCombination> GetAllCombinations() => allCombinations;

    // ??? Подсветка — какие комментарии фантомов видны сейчас ?????????

    public List<PhantomComment> GetVisibleComments(string clueId)
    {
        ClueData clue = GetClueById(clueId);
        if (clue == null) return new List<PhantomComment>();

        var visible = new List<PhantomComment>();
        foreach (var comment in clue.phantomComments)
        {
            float sync = PhantomManager.instance?.GetSync(comment.phantom) ?? 0f;
            if (sync >= comment.requiredSync)
                visible.Add(comment);
        }
        return visible;
    }

    private ClueCombination FindCombinationForPair(string idA, string idB)
    {
        foreach (var combo in allCombinations)
            if (combo.ContainsBoth(idA, idB)) return combo;
        return null;
    }

    // ??? Editor ??????????????????????????????????????????????????????

    [Button("Показать состояние доски")]
    private void EditorPrint()
    {
        Debug.Log($"[Board] Улики: {string.Join(", ", discoveredClueIds)}");
        Debug.Log($"[Board] Связи: {connections.Count}");
        Debug.Log($"[Board] Мысли: {string.Join(", ", unlockedThoughtIds)}");
    }
}

// ??? Вспомогательные типы ????????????????????????????????????????????

[System.Serializable]
public class ClueConnection
{
    public string clueIdA;
    public string clueIdB;
    public bool isValid;  // true = есть комбинация для этой пары
}

public enum ConnectionResult
{
    ValidMatch,    // Есть комбинация — магнит притянул
    NoMatch,       // Связь создана но комбинации нет — отталкивание
    AlreadyExists, // Уже соединены
    Invalid        // Та же улика
}