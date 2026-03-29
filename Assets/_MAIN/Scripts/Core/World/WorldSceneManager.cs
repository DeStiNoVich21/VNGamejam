using COMMANDS;
using DIALOGUE;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Регистрирует актёров в WorldObjectManager
/// и передаёт txt-файл в DialogueSystem.
/// нужен для того чтобы откуда угодно вызывать диалоги.
/// улучшенный старый Worldscenedirector который работал только с тригером для себя.
/// </summary>
public class WorldSceneManager : MonoBehaviour
{
    public static WorldSceneManager instance { get; private set; }

    [Title("VN Controller")]
    [Tooltip("Перетащи сюда [VN Controller] — включится при старте, выключится после диалога")]
    [SerializeField] private GameObject vnController;

    [Title("Player")]
    [Tooltip("Перетащи сюда игрока — Movement выключится при старте, включится после диалога")]
    [SerializeField] private Movement playerMovement;

    [Title("Actors")]
    [SerializeField] private List<WorldActor> actors = new List<WorldActor>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (instance != null && WorldObjectManager.instance != null && CommandManager.instance != null)
            RegisterActors();
    }

    public void Activate(TextAsset sceneFile, bool autoHideWhenDone = true)
    {
        if (sceneFile == null)
        {
            Debug.LogWarning($"[WorldSceneDirector] '{name}': sceneFile is not assigned");
            return;
        }

        if (vnController != null)
            vnController.SetActive(true);

        if (playerMovement != null)
            playerMovement.enabled = false;

        List<string> lines = FileManager.ReadTextAsset(sceneFile, includeBlankLines: true);
        Conversation conversation = new Conversation(lines);
        DialogueSystem.instance.Say(conversation);

        if (autoHideWhenDone && vnController != null)
            StartCoroutine(HideWhenDone());
    }

    private IEnumerator HideWhenDone()
    {
        yield return null;

        while (DialogueSystem.instance.conversationManager.isRunning)
            yield return null;

        vnController.SetActive(false);

        if (playerMovement != null)
            playerMovement.enabled = true;
    }


    private void RegisterActors()
    {
        foreach (var actor in actors)
        {
            if (actor.go == null) continue;

            WorldObjectManager.instance.Register(actor.id, actor.go);

            CommandDatabase db = CommandManager.instance.CreateSubDatabase(actor.id);
            WorldObjectCommands.RegisterTo(db, actor.id);
        }
    }

    public void UnregisterActors()
    {
        foreach (var actor in actors)
            WorldObjectManager.instance.Unregister(actor.id);
    }
}

[System.Serializable]
public class WorldActor
{
    [HorizontalGroup(150), LabelText("ID")]
    public string id;

    [HorizontalGroup, HideLabel]
    public GameObject go;
}