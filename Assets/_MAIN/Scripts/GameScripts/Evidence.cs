using COMMANDS;
using DIALOGUE;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Evidence : MonoBehaviour
{
    [Title("VN Controller")]
    [SerializeField] private bool autoHideWhenDone = true;

    [Title("Scene File")]
    [SerializeField] private TextAsset sceneFile;

    private void OnMouseDown()
    {
        WorldSceneManager.instance.Activate(sceneFile, autoHideWhenDone);
    }
}
