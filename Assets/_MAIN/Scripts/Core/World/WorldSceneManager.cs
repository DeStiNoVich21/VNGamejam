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
        // --- ИСПРАВЛЕНИЕ: Правильная логика singleton ---
        if (instance == null)
        {
            instance = this;
            // НЕ добавляем DontDestroyOnLoad здесь! SceneTransitionManager уже это контролирует
            Debug.Log($"[WorldSceneManager] Инициализирован: {gameObject.name}");
        }
        else
        {
            // Если уже есть экземпляр
            if (instance.gameObject != gameObject)
            {
                Debug.LogWarning($"[WorldSceneManager] Обнаружен дубликат '{gameObject.name}', удаляю...");
                Destroy(gameObject);
            }
        }
    }

    private void Start()
    {
        // --- ИСПРАВЛЕНИЕ: Проверяем что instance это именно мы ---
        if (instance != this) return;

        if (WorldObjectManager.instance != null && CommandManager.instance != null)
        {
            RegisterActors();
            Debug.Log($"[WorldSceneManager] Актёры зарегистрированы ({actors.Count} шт.)");
        }
        else
        {
            Debug.LogWarning($"[WorldSceneManager] WorldObjectManager или CommandManager ещё не инициализированы!");
        }
    }

    public void Activate(TextAsset sceneFile, bool autoHideWhenDone = true)
    {
        if (sceneFile == null)
        {
            Debug.LogWarning($"[WorldSceneManager] '{name}': sceneFile is not assigned");
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
            if (actor.go == null)
            {
                Debug.LogWarning($"[WorldSceneManager] Actor '{actor.id}' имеет null GameObject!");
                continue;
            }

            WorldObjectManager.instance.Register(actor.id, actor.go);

            CommandDatabase db = CommandManager.instance.CreateSubDatabase(actor.id);
            WorldObjectCommands.RegisterTo(db, actor.id);

            Debug.Log($"[WorldSceneManager] Зарегистрирован актёр: {actor.id}");
        }
    }

    public void UnregisterActors()
    {
        foreach (var actor in actors)
        {
            if (string.IsNullOrEmpty(actor.id)) continue;
            WorldObjectManager.instance.Unregister(actor.id);
        }
    }

    // --- НОВОЕ: Очистка при выгрузке сцены ---
    private void OnDestroy()
    {
        if (instance == this)
        {
            Debug.Log($"[WorldSceneManager] Уничтожен, очищаю регистрацию актёров");
            UnregisterActors();
            instance = null;
        }
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