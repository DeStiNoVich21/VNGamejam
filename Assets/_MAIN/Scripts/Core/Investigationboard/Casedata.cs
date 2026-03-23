using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// ScriptableObject — один кейс расследования.
/// Содержит все улики, слоты 5W+H, комбинации выводов.
/// Создать: ПКМ ? Create ? World Begone ? Case
/// </summary>
[CreateAssetMenu(fileName = "Case_", menuName = "World Begone/Case")]
public class CaseData : ScriptableObject
{
    [Title("Кейс")]
    public string caseId;
    public string caseTitle;
    [TextArea(2, 4)] public string caseDescription;

    [Title("Слоты 5W+H")]
    public SlotConfig slotWho = new SlotConfig { question = "КТО?", tag = StickerTag.Who };
    public SlotConfig slotWhat = new SlotConfig { question = "ЧТО?", tag = StickerTag.What };
    public SlotConfig slotWhere = new SlotConfig { question = "ГДЕ?", tag = StickerTag.Where };
    public SlotConfig slotWhen = new SlotConfig { question = "КОГДА?", tag = StickerTag.When };
    public SlotConfig slotWhy = new SlotConfig
    {
        question = "ПОЧЕМУ?",
        tag = StickerTag.Why,
        acceptsFacts = true,
        maxStickers = 8
    };
    public SlotConfig slotHow = new SlotConfig { question = "КАК?", tag = StickerTag.How };

    [Title("Возможные выводы (до 4)")]
    [InfoBox("Каждый вывод = комбинация улик в WHY. Заполни requiredStickerIds.")]
    public List<CaseConclusion> conclusions = new List<CaseConclusion>();

    public SlotConfig GetSlot(StickerTag tag)
    {
        switch (tag)
        {
            case StickerTag.Who: return slotWho;
            case StickerTag.What: return slotWhat;
            case StickerTag.Where: return slotWhere;
            case StickerTag.When: return slotWhen;
            case StickerTag.Why: return slotWhy;
            case StickerTag.How: return slotHow;
            default: return null;
        }
    }

    public List<SlotConfig> GetAllSlots() => new List<SlotConfig>
        { slotWho, slotWhat, slotWhere, slotWhen, slotWhy, slotHow };
}

// ??? Тег улики ???????????????????????????????????????????????????????

public enum StickerTag
{
    Who, What, Where, When, Why, How,
    Fact   // факты от фантомов — идут только в Why
}

// ??? Конфиг одного слота ?????????????????????????????????????????????

[System.Serializable]
public class SlotConfig
{
    public string question;
    public StickerTag tag;

    [Tooltip("Максимум стикеров в слоте. 0 = без лимита")]
    public int maxStickers = 3;

    [Tooltip("Принимает ли факты фантомов (только Why)")]
    public bool acceptsFacts = false;

    [Tooltip("Позиция якоря на доске в локальных координатах")]
    public Vector2 boardPosition;
}

// ??? Вывод по кейсу ??????????????????????????????????????????????????

[System.Serializable]
public class CaseConclusion
{
    public string conclusionId;
    public string title;

    [TextArea(3, 8)]
    [Tooltip("Текст Summary — заполни вручную. Используй {who} {what} {where} {when} {why} {how} как плейсхолдеры")]
    public string summaryText;

    [Tooltip("ID стикеров которые должны лежать в WHY для этого вывода")]
    public List<string> requiredStickerIds = new List<string>();

    [Tooltip("Бонусы синхронизации при достижении этого вывода")]
    public List<SyncBonus> syncBonuses = new List<SyncBonus>();

    [Tooltip("Флаг который выставляется в VariableStore")]
    public string setsFlag = "";

    public bool IsSatisfied(List<string> whyContents)
    {
        foreach (var id in requiredStickerIds)
            if (!whyContents.Contains(id)) return false;
        return true;
    }
}