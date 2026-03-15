using COMMANDS;
using DIALOGUE;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Регистрирует актёров в WorldObjectManager
/// и передаёт txt-файл в DialogueSystem.
/// Включает [VN Controller] при старте и выключает по окончании диалога.
/// </summary>
public class WorldSceneDirector : MonoBehaviour
{
    [Title("VN Controller")]
    [Tooltip("Перетащи сюда [VN Controller] — включится при старте, выключится после диалога")]
    [SerializeField] private GameObject vnController;
    [SerializeField] private bool autoHideWhenDone = true;

    [Title("Player")]
    [Tooltip("Перетащи сюда игрока — Movement выключится при старте, включится после диалога")]
    [SerializeField] private Movement playerMovement;

    [Title("Scene File")]
    [SerializeField] private TextAsset sceneFile;

    [Title("Actors")]
    [SerializeField] private List<WorldActor> actors = new List<WorldActor>();

    [Title("Trigger")]
    [SerializeField] private bool useTrigger = false;
    [SerializeField, ShowIf("useTrigger")] private string triggerTag = "Player";
    [SerializeField, ShowIf("useTrigger")] private bool oneShot = true;

    private bool _triggered = false;

    // --- 2D Trigger ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (oneShot && _triggered) return;
        if (!other.CompareTag(triggerTag)) return;

        _triggered = true;
        Activate();
    }

    // --- Public entry point ---

    public void Activate()
    {
        if (sceneFile == null)
        {
            Debug.LogWarning($"[WorldSceneDirector] '{name}': sceneFile is not assigned");
            return;
        }

        // Включаем VN Controller перед стартом диалога
        if (vnController != null)
            vnController.SetActive(true);

        // Отключаем управление игроком на время диалога
        if (playerMovement != null)
            playerMovement.enabled = false;

        RegisterActors();

        // ConversationManager читает файл — диалог, команды, фон, аудио
        List<string> lines = FileManager.ReadTextAsset(sceneFile, includeBlankLines: true);
        Conversation conversation = new Conversation(lines);
        DialogueSystem.instance.Say(conversation);

        // После окончания диалога — прячем VN Controller
        if (autoHideWhenDone && vnController != null)
            StartCoroutine(HideWhenDone());
    }

    private IEnumerator HideWhenDone()
    {
        // Один кадр чтобы диалог успел запуститься
        yield return null;

        // Ждём пока ConversationManager не закончит
        while (DialogueSystem.instance.conversationManager.isRunning)
            yield return null;

        vnController.SetActive(false);

        // Возвращаем управление игроку
        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    // --- Actor registration ---

    private void RegisterActors()
    {
        foreach (var actor in actors)
        {
            if (actor.go == null) continue;

            // Регистрируем в WorldObjectManager
            WorldObjectManager.instance.Register(actor.id, actor.go);

            // Регистрируем sub-database в CommandManager с именем актёра
            // Это позволяет писать в txt: 1.MoveTo(0 0 0) или Kairn.Flip()
            CommandDatabase db = CommandManager.instance.CreateSubDatabase(actor.id);
            WorldObjectCommands.RegisterTo(db, actor.id);
        }
    }

    public void UnregisterActors()
    {
        foreach (var actor in actors)
            WorldObjectManager.instance.Unregister(actor.id);
    }

    // --- Editor buttons ---

    [Button("Play Scene")]
    private void ActivateFromEditor() => Activate();

    [Button("Reset Trigger"), ShowIf("oneShot")]
    private void ResetTrigger() => _triggered = false;
}

[System.Serializable]
public class WorldActor
{
    [HorizontalGroup(150), LabelText("ID")]
    public string id;

    [HorizontalGroup, HideLabel]
    public GameObject go;
}