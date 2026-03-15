using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Управляет слоями Canvas-Main.
/// Вешается на [Manager] рядом с WorldObjectManager.
///
/// Команды в txt:
///   layer.SetActive(dialogue false)
///   layer.SetActive(characters true)
///   layer.FadeTo(dialogue 0 -spd 1.5)
///   layer.FadeTo(background 1 -spd 1.0)
/// </summary>
public class VNLayerManager : MonoBehaviour
{
    public static VNLayerManager instance { get; private set; }

    [Title("Layers")]
    [SerializeField] private GameObject underlayBackground;   // 0 - Underlay Background
    [SerializeField] private GameObject background;           // 1 - Background
    [SerializeField] private GameObject characters;           // 2 - Characters
    [SerializeField] private GameObject cinematic;            // 3 - Cinematic
    [SerializeField] private GameObject dialogue;             // 4 - Dialogue
    [SerializeField] private GameObject foreground;           // 5 - Foreground
    [SerializeField] private GameObject playerInteraction;    // 6 - Player Interaction

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // ??? Получить слой по имени ??????????????????????????????????????

    public GameObject GetLayer(string name)
    {
        switch (name.ToLower().Replace(" ", "").Replace("-", ""))
        {
            case "underlaybackground":
            case "underlay":
            case "0": return underlayBackground;
            case "background":
            case "bg":
            case "1": return background;
            case "characters":
            case "character":
            case "chars":
            case "2": return characters;
            case "cinematic":
            case "3": return cinematic;
            case "dialogue":
            case "dialog":
            case "4": return dialogue;
            case "foreground":
            case "fg":
            case "5": return foreground;
            case "playerinteraction":
            case "player":
            case "interaction":
            case "6": return playerInteraction;
            default:
                Debug.LogWarning($"[VNLayerManager] Слой '{name}' не найден");
                return null;
        }
    }

    // ??? SetActive ???????????????????????????????????????????????????

    public void SetLayerActive(string layerName, bool active)
    {
        GameObject layer = GetLayer(layerName);
        if (layer != null) layer.SetActive(active);
    }

    // ??? Fade ????????????????????????????????????????????????????????

    public Coroutine FadeLayer(string layerName, float targetAlpha, float speed)
    {
        GameObject layer = GetLayer(layerName);
        if (layer == null) return null;

        CanvasGroup cg = layer.GetComponent<CanvasGroup>();
        if (cg == null) cg = layer.AddComponent<CanvasGroup>();

        return StartCoroutine(Fading(cg, targetAlpha, speed));
    }

    private IEnumerator Fading(CanvasGroup cg, float target, float speed)
    {
        while (Mathf.Abs(cg.alpha - target) > 0.01f)
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, target, speed * Time.deltaTime);
            yield return null;
        }
        cg.alpha = target;

        // Если полностью прозрачный — отключаем raycast и interactable
        cg.interactable = target > 0f;
        cg.blocksRaycasts = target > 0f;
    }

    // ??? Editor кнопки для теста ?????????????????????????????????????

    [Button("Show All Layers")]
    private void ShowAll()
    {
        foreach (var name in new[] { "0", "1", "2", "3", "4", "5", "6" })
            SetLayerActive(name, true);
    }

    [Button("Hide All Layers")]
    private void HideAll()
    {
        foreach (var name in new[] { "0", "1", "2", "3", "4", "5", "6" })
            SetLayerActive(name, false);
    }
}