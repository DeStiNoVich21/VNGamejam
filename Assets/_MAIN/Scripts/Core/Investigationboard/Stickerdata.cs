using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// ScriptableObject — один стикер (улика или факт фантома).
/// Создать: ПКМ ? Create ? World Begone ? Sticker
/// </summary>
[CreateAssetMenu(fileName = "Sticker_", menuName = "World Begone/Sticker")]
public class StickerData : ScriptableObject
{
    [Title("Стикер")]
    public string stickerId;
    public string title;
    public StickerTag tag;

    [TextArea(2, 6), LabelText("Текст на стикере")]
    public string bodyText;

    [Tooltip("Маленькая иконка сверху (опционально)")]
    [PreviewField(60)] public Sprite icon;

    [Title("Факт от фантома")]
    [Tooltip("Если это факт от фантома — выбери кого. Иначе оставь None.")]
    public bool isPhantomFact = false;

    [ShowIf("isPhantomFact")]
    public PhantomManager.PhantomType phantomSource;

    [ShowIf("isPhantomFact"), Range(0, 100)]
    [Tooltip("Минимальная синхронизация чтобы этот факт был доступен")]
    public float requiredSync = 25f;

    [Title("Комментарии фантомов к этому стикеру")]
    [InfoBox("Фантомы могут комментировать любой стикер при достаточной синхронизации")]
    public List<PhantomComment> phantomComments = new List<PhantomComment>();
}