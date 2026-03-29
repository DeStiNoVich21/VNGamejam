using COMMANDS;
using DIALOGUE;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class WorldSceneDirector : MonoBehaviour
{
    [Title("VN Controller")]
    [SerializeField] private bool autoHideWhenDone = true;

    [Title("Scene File")]
    [SerializeField] private TextAsset sceneFile;

    [Title("Trigger")]
    [SerializeField] private bool useTrigger = false;
    [SerializeField, ShowIf("useTrigger")] private string triggerTag = "Player";
    [SerializeField, ShowIf("useTrigger")] private bool oneShot = true;

    private bool _triggered = false;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (oneShot && _triggered) return;
        if (!other.CompareTag(triggerTag)) return;

        _triggered = true;

        WorldSceneManager.instance.Activate(sceneFile, autoHideWhenDone);
    }
}