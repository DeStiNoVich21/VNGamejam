using COMMANDS;
using DIALOGUE;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

public class Evidence : MonoBehaviour
{
    [Title("VN Controller")]
    [SerializeField] private bool autoHideWhenDone = true;

    [Title("Scene File")]
    [SerializeField] private TextAsset sceneFile;

    [Title("Investigation Board")]
    [SerializeField] private string sticker_id;

    [Title("Visual Settings")]
    [Tooltip("Использовать ли эффект белого цвета вместо изменения edge_threshold?")]
    [SerializeField] private bool useFlashEffect = true;

    [ShowIf("useFlashEffect")]
    [SerializeField] private Color hoverColor = Color.white;

    [HideIf("useFlashEffect")]
    [SerializeField] private string edgeThresholdProperty = "_edge_threshold";
    [HideIf("useFlashEffect")]
    [SerializeField] private float glowValue = 0.5f;
    [HideIf("useFlashEffect")]
    [SerializeField] private float noGlowValue = 5f;

    private bool hasBeenClicked = false;
    private Material myMaterial;
    private Color originalColor;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color"); // Оптимизация обращения к шейдеру

    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            myMaterial = renderer.material;

            if (useFlashEffect)
                originalColor = myMaterial.HasProperty(ColorProperty) ? myMaterial.GetColor(ColorProperty) : Color.white;
            else
                myMaterial.SetFloat(edgeThresholdProperty, noGlowValue);
        }
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (!hasBeenClicked && myMaterial != null)
        {
            if (useFlashEffect)
                myMaterial.SetColor(ColorProperty, hoverColor);
            else
                myMaterial.SetFloat(edgeThresholdProperty, glowValue);
        }
    }

    private void OnMouseExit()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        ResetVisuals();
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (hasBeenClicked) return;

        hasBeenClicked = true;
        ResetVisuals();

        if (!string.IsNullOrEmpty(sticker_id) && InvestigationBoardManager.instance != null)
        {
            InvestigationBoardManager.instance.AddSticker(sticker_id);
        }

        if (sceneFile != null) WorldSceneManager.instance.Activate(sceneFile, autoHideWhenDone);
    }

    private void ResetVisuals()
    {
        if (myMaterial == null) return;

        if (useFlashEffect)
            myMaterial.SetColor(ColorProperty, originalColor);
        else
            myMaterial.SetFloat(edgeThresholdProperty, noGlowValue);
    }
}