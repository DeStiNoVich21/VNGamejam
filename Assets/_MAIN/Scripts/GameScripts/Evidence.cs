using COMMANDS;
using DIALOGUE;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using SpriteGlow; // Добавляем пространство имен твоего глоу-эффекта

public class Evidence : MonoBehaviour
{
    [Title("VN Controller")]
    [SerializeField] private bool autoHideWhenDone = true;

    [Title("Scene File")]
    [SerializeField] private TextAsset sceneFile;

    [Title("Investigation Board")]
    [SerializeField] private string sticker_id;

    [Title("Visual Settings")]
    [EnumToggleButtons]
    public enum HighlightType { SpriteGlow, FlashColor, ManualShader }
    public HighlightType highlightMode = HighlightType.SpriteGlow;

    [ShowIf("highlightMode", HighlightType.FlashColor)]
    [SerializeField] private Color hoverColor = Color.white;

    [ShowIf("highlightMode", HighlightType.ManualShader)]
    [SerializeField] private string edgeThresholdProperty = "_edge_threshold";

    private bool hasBeenClicked = false;
    private SpriteGlowEffect glowEffect;
    private SpriteRenderer sRenderer;
    private Material myMaterial;
    private Color originalColor;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private void Awake()
    {
        sRenderer = GetComponent<SpriteRenderer>();
        glowEffect = GetComponent<SpriteGlowEffect>();

        // Если выбран режим глоу, но компонента нет — выключаем его по умолчанию
        if (glowEffect != null && highlightMode == HighlightType.SpriteGlow)
            glowEffect.enabled = false;
    }

    private void Start()
    {
        if (sRenderer != null)
        {
            myMaterial = sRenderer.material;
            if (myMaterial.HasProperty(ColorProperty))
                originalColor = myMaterial.GetColor(ColorProperty);
        }
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (hasBeenClicked) return;

        SetHighlight(true);
    }

    private void OnMouseExit()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (hasBeenClicked) return;

        SetHighlight(false);
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (hasBeenClicked) return;

        hasBeenClicked = true;
        SetHighlight(false); // Выключаем подсветку при клике

        if (!string.IsNullOrEmpty(sticker_id) && InvestigationBoardManager.instance != null)
            InvestigationBoardManager.instance.AddSticker(sticker_id);

        if (sceneFile != null) WorldSceneManager.instance.Activate(sceneFile, autoHideWhenDone);
    }

    private void SetHighlight(bool state)
    {
        switch (highlightMode)
        {
            case HighlightType.SpriteGlow:
                if (glowEffect != null) glowEffect.enabled = state;
                break;

            case HighlightType.FlashColor:
                if (myMaterial != null)
                    myMaterial.SetColor(ColorProperty, state ? hoverColor : originalColor);
                break;

            case HighlightType.ManualShader:
                if (myMaterial != null)
                    myMaterial.SetFloat(edgeThresholdProperty, state ? 0.5f : 5f);
                break;
        }
    }
}