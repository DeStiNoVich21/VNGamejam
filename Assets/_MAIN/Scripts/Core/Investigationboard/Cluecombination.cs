using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// ScriptableObject — одна комбинация улик = одна мысль.
/// Создать: ПКМ ? Create ? World Begone ? Clue Combination
/// </summary>
[CreateAssetMenu(fileName = "Combination_", menuName = "World Begone/Clue Combination")]
public class ClueCombination : ScriptableObject
{
    [Title("Комбинация")]
    [InfoBox("Перетащи сюда ClueData которые нужно соединить верёвкой")]
    public List<ClueData> requiredClues = new List<ClueData>();

    [Title("Результат — Мысль")]
    public string thoughtId;
    public string thoughtTitle;

    [TextArea(3, 8)]
    public string thoughtText;

    [PreviewField(80)] public Sprite thoughtImage;

    [Title("Бонусы при открытии")]
    public List<SyncBonus> syncBonuses = new List<SyncBonus>();

    [Title("Разблокирует")]
    [Tooltip("ID диалогового файла который становится доступен")]
    public string unlocksDialogueFile = "";

    [Tooltip("ID флага который выставляется в VariableStore")]
    public string setsFlag = "";

    // Проверить есть ли все нужные улики в списке открытых
    public bool IsSatisfied(List<string> discoveredIds)
    {
        foreach (var clue in requiredClues)
        {
            if (clue == null) continue;
            if (!discoveredIds.Contains(clue.id))
                return false;
        }
        return true;
    }

    // Проверить что именно эти две улики входят в комбинацию
    public bool ContainsBoth(string idA, string idB)
    {
        bool hasA = false, hasB = false;
        foreach (var c in requiredClues)
        {
            if (c == null) continue;
            if (c.id == idA) hasA = true;
            if (c.id == idB) hasB = true;
        }
        return hasA && hasB;
    }
}

// SyncBonus определён в SharedTypes.cs