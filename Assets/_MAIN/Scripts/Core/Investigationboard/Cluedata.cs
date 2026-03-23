using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// ScriptableObject Ч одна улика.
/// —оздать: ѕ ћ ? Create ? World Begone ? Clue
/// </summary>
[CreateAssetMenu(fileName = "Clue_", menuName = "World Begone/Clue")]
public class ClueData : ScriptableObject
{
    [Title("ќсновное")]
    public string id;
    public string title;
    [PreviewField(80)] public Sprite image;

    [TextArea(2, 5)]
    public string description;

    public ClueTier tier = ClueTier.Lead;

    [Title(" омментарии фантомов")]
    [InfoBox("ќставь пустым если фантом молчит об этой улике")]
    public List<PhantomComment> phantomComments = new List<PhantomComment>();

    [Title(" омбинации")]
    [InfoBox("«аполн€етс€ в ClueCombination SO Ч здесь только дл€ справки")]
    [ReadOnly] public List<string> partOfCombinations = new List<string>();
}

public enum ClueTier
{
    Lead,        // «ацепка    Ч сера€ карточка
    Connection,  // —в€зь      Ч син€€
    Clue,        // ”лика      Ч жЄлта€
    Truth        // »стина     Ч красна€
}

// PhantomComment определЄн в SharedTypes.cs