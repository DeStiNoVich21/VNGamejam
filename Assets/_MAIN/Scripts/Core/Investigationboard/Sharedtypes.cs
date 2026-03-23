using UnityEngine;

/// <summary>
/// Общие типы используемые в нескольких системах.
/// PhantomComment — комментарий фантома к улике/стикеру.
/// SyncBonus — бонус синхронизации при открытии вывода/мысли.
/// </summary>

[System.Serializable]
public class PhantomComment
{
    [UnityEngine.Tooltip("Какой фантом даёт этот комментарий")]
    public PhantomManager.PhantomType phantom;

    [UnityEngine.Range(0, 100)]
    [UnityEngine.Tooltip("Минимальная синхронизация чтобы увидеть комментарий")]
    public float requiredSync = 0f;

    [UnityEngine.TextArea(2, 4)]
    public string comment;
}

[System.Serializable]
public class SyncBonus
{
    public PhantomManager.PhantomType phantom;

    [UnityEngine.Range(0, 30)]
    public float amount = 5f;
}