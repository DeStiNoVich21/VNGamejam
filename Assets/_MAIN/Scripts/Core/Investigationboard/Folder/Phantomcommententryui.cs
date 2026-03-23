using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ќдна строка комментари€ фантома в детальной панели улики.
/// </summary>
public class PhantomCommentEntryUI : MonoBehaviour
{
    [SerializeField] private Image phantomIcon;
    [SerializeField] private TextMeshProUGUI phantomName;
    [SerializeField] private TextMeshProUGUI commentText;

    // ÷вета фантомов дл€ иконки
    private static readonly System.Collections.Generic.Dictionary
        <PhantomManager.PhantomType, Color> PHANTOM_COLORS = new()
    {
        { PhantomManager.PhantomType.Genesis,    new Color(0.4f, 0.7f, 1.0f) },
        { PhantomManager.PhantomType.Melancholy, new Color(0.7f, 0.4f, 0.9f) },
        { PhantomManager.PhantomType.Fury,       new Color(1.0f, 0.3f, 0.3f) },
        { PhantomManager.PhantomType.Stigma,     new Color(0.3f, 0.9f, 0.5f) },
        { PhantomManager.PhantomType.Ego,        new Color(0.9f, 0.9f, 0.9f) },
    };

    public void Initialize(PhantomComment comment)
    {
        if (phantomName != null)
            phantomName.text = comment.phantom.ToString();

        if (commentText != null)
            commentText.text = comment.comment;

        if (phantomIcon != null && PHANTOM_COLORS.TryGetValue(comment.phantom, out Color c))
            phantomIcon.color = c;
    }
}