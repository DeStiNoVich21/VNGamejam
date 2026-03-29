using COMMANDS;
using DIALOGUE;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
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

    private string edgeThresholdProperty = "_edge_threshold";
    private float glowValue = 0.5f;
    private float noGlowValue = 5f;

    private bool hasBeenClicked = false;
    private Material myMaterial;

    private void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Берем материал у объекта
            myMaterial = renderer.material;
            // Изначально выключаем свечение
            myMaterial.SetFloat(edgeThresholdProperty, noGlowValue);
        }
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (!hasBeenClicked && myMaterial != null)
        {
            myMaterial.SetFloat(edgeThresholdProperty, glowValue);
        }
    }

    private void OnMouseExit()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (myMaterial != null)
        {
            myMaterial.SetFloat(edgeThresholdProperty, noGlowValue);
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (hasBeenClicked) return;

        hasBeenClicked = true;

        if (myMaterial != null)
        {
            myMaterial.SetFloat(edgeThresholdProperty, noGlowValue);
        }

        if (!string.IsNullOrEmpty(sticker_id) && InvestigationBoardManager.instance != null)
        {
            InvestigationBoardManager.instance.AddSticker(sticker_id);
        }

        WorldSceneManager.instance.Activate(sceneFile, autoHideWhenDone);
    }
}
