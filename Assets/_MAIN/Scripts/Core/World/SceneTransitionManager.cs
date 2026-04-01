using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// Управляет переходами между сценами с wipe-эффектом (как в Katana Zero).
/// Поддерживает DontDestroyOnLoad для игрока и UI.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance { get; private set; }

    [Title("Transition Panel")]
    [SerializeField, Required]
    private RectTransform transitionPanel;

    [SerializeField, Required]
    private Image transitionImage;

    [Title("Animation Settings")]
    [SerializeField, Range(0.1f, 3f)]
    private float wipeDuration = 0.5f;

    [SerializeField]
    private AnimationCurve wipeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField]
    private TransitionDirection defaultDirection = TransitionDirection.RightToLeft;

    [Title("Persistent Objects")]
    [InfoBox("Объекты, которые должны сохраняться между сценами")]
    [SerializeField]
    private GameObject player;

    [SerializeField]
    private GameObject persistentUI;

    [SerializeField]
    private GameObject vnController;

    private bool isTransitioning = false;
    private Canvas canvas;

    // Кешируем ссылки, чтобы они не терялись
    private Transform originalParent;

    public enum TransitionDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // КРИТИЧЕСКИ ВАЖНО: сначала делаем DontDestroyOnLoad самого менеджера
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Потом настраиваем остальные объекты
            SetupPersistentObjects();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        // Сохраняем оригинального родителя панели
        if (transitionPanel != null)
        {
            originalParent = transitionPanel.parent;
            transitionPanel.gameObject.SetActive(false);
        }
    }

    private void SetupPersistentObjects()
    {
        // Player
        if (player != null)
        {
            player.transform.SetParent(null);
            DontDestroyOnLoad(player);
            Debug.Log($"[SceneTransition] Player '{player.name}' marked as DontDestroyOnLoad");
        }

        // Persistent UI
        if (persistentUI != null)
        {
            persistentUI.transform.SetParent(null);
            DontDestroyOnLoad(persistentUI);
            Debug.Log($"[SceneTransition] PersistentUI '{persistentUI.name}' marked as DontDestroyOnLoad");
        }

        // VN Controller
        if (vnController != null)
        {
            vnController.transform.SetParent(null);
            DontDestroyOnLoad(vnController);
            Debug.Log($"[SceneTransition] VN Controller '{vnController.name}' marked as DontDestroyOnLoad");
        }

        // Canvas самого менеджера тоже должен быть persistent
        if (canvas != null && canvas.gameObject != gameObject)
        {
            Canvas rootCanvas = canvas.rootCanvas;
            if (rootCanvas != null && rootCanvas.gameObject != gameObject)
            {
                rootCanvas.transform.SetParent(null);
                DontDestroyOnLoad(rootCanvas.gameObject);
                Debug.Log($"[SceneTransition] Canvas '{rootCanvas.name}' marked as DontDestroyOnLoad");
            }
        }
    }

    /// <summary>
    /// Загружает сцену с wipe-эффектом
    /// </summary>
    public void LoadScene(string sceneName, TransitionDirection? direction = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransition] Переход уже в процессе!");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName, direction ?? defaultDirection));
    }

    /// <summary>
    /// Загружает сцену по индексу
    /// </summary>
    public void LoadScene(int sceneIndex, TransitionDirection? direction = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransition] Переход уже в процессе!");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneIndex, direction ?? defaultDirection));
    }

    private IEnumerator TransitionCoroutine(object sceneIdentifier, TransitionDirection direction)
    {
        isTransitioning = true;

        // Отключаем движение игрока
        DisablePlayerMovement();

        // Wipe IN (панель закрывает экран)
        yield return StartCoroutine(WipeIn(direction));

        // Загружаем новую сцену
        if (sceneIdentifier is string sceneName)
        {
            Debug.Log($"[SceneTransition] Loading scene: {sceneName}");
            yield return SceneManager.LoadSceneAsync(sceneName);
        }
        else if (sceneIdentifier is int sceneIndex)
        {
            Debug.Log($"[SceneTransition] Loading scene index: {sceneIndex}");
            yield return SceneManager.LoadSceneAsync(sceneIndex);
        }

        // Даем время Unity обработать загрузку
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.15f);

        // Проверяем что панель все еще существует после загрузки
        if (transitionPanel == null || transitionImage == null)
        {
            Debug.LogError("[SceneTransition] Transition panel was destroyed! Scene transition cancelled.");
            isTransitioning = false;
            yield break;
        }
        // --- НОВЫЙ БЛОК: ОЧИСТКА ---
        // Ждем один кадр, чтобы объекты в новой сцене успели проснуться (Awake)
        yield return new WaitForEndOfFrame();

        RemoveDuplicatesInNewScene();
        // ---------------------------

        yield return new WaitForSeconds(0.15f);

        if (transitionPanel != null)
        {

        }
            yield return StartCoroutine(WipeOut(direction));
        // Wipe OUT (панель открывает экран)
        yield return StartCoroutine(WipeOut(direction));

        // Включаем движение игрока
        EnablePlayerMovement();

        isTransitioning = false;

        Debug.Log($"[SceneTransition] Transition completed successfully");
    }

    private IEnumerator WipeIn(TransitionDirection direction)
    {
        // Проверяем что объекты существуют
        if (transitionPanel == null)
        {
            Debug.LogError("[SceneTransition] TransitionPanel is null!");
            yield break;
        }

        transitionPanel.gameObject.SetActive(true);

        Vector2 startPos = GetStartPosition(direction, isWipeIn: true);
        Vector2 endPos = Vector2.zero;

        transitionPanel.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < wipeDuration)
        {
            // Проверка на случай если объект был уничтожен во время анимации
            if (transitionPanel == null)
            {
                Debug.LogError("[SceneTransition] TransitionPanel destroyed during WipeIn!");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = wipeCurve.Evaluate(elapsed / wipeDuration);
            transitionPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        if (transitionPanel != null)
            transitionPanel.anchoredPosition = endPos;
    }

    private IEnumerator WipeOut(TransitionDirection direction)
    {
        // Проверяем что объекты существуют
        if (transitionPanel == null)
        {
            Debug.LogError("[SceneTransition] TransitionPanel is null!");
            yield break;
        }

        Vector2 startPos = Vector2.zero;
        Vector2 endPos = GetStartPosition(direction, isWipeIn: false);

        transitionPanel.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < wipeDuration)
        {
            // Проверка на случай если объект был уничтожен во время анимации
            if (transitionPanel == null)
            {
                Debug.LogError("[SceneTransition] TransitionPanel destroyed during WipeOut!");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = wipeCurve.Evaluate(elapsed / wipeDuration);
            transitionPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        if (transitionPanel != null)
        {
            transitionPanel.anchoredPosition = endPos;
            transitionPanel.gameObject.SetActive(false);
        }
    }

    private Vector2 GetStartPosition(TransitionDirection direction, bool isWipeIn)
    {
        if (canvas == null)
        {
            Debug.LogWarning("[SceneTransition] Canvas is null, using default screen size");
            return GetStartPositionFallback(direction, isWipeIn);
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogWarning("[SceneTransition] Canvas RectTransform is null");
            return GetStartPositionFallback(direction, isWipeIn);
        }

        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        switch (direction)
        {
            case TransitionDirection.RightToLeft:
                return isWipeIn ? new Vector2(width, 0) : new Vector2(-width, 0);

            case TransitionDirection.LeftToRight:
                return isWipeIn ? new Vector2(-width, 0) : new Vector2(width, 0);

            case TransitionDirection.TopToBottom:
                return isWipeIn ? new Vector2(0, height) : new Vector2(0, -height);

            case TransitionDirection.BottomToTop:
                return isWipeIn ? new Vector2(0, -height) : new Vector2(0, height);

            default:
                return Vector2.zero;
        }
    }

    private Vector2 GetStartPositionFallback(TransitionDirection direction, bool isWipeIn)
    {
        float width = Screen.width;
        float height = Screen.height;

        switch (direction)
        {
            case TransitionDirection.RightToLeft:
                return isWipeIn ? new Vector2(width, 0) : new Vector2(-width, 0);
            case TransitionDirection.LeftToRight:
                return isWipeIn ? new Vector2(-width, 0) : new Vector2(width, 0);
            case TransitionDirection.TopToBottom:
                return isWipeIn ? new Vector2(0, height) : new Vector2(0, -height);
            case TransitionDirection.BottomToTop:
                return isWipeIn ? new Vector2(0, -height) : new Vector2(0, height);
            default:
                return Vector2.zero;
        }
    }

    private void DisablePlayerMovement()
    {
        if (player == null) return;

        Movement movement = player.GetComponent<Movement>();
        if (movement != null)
            movement.enabled = false;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void RemoveDuplicatesInNewScene()
    {
        // 1. Очистка Игрока
        if (player != null)
        {
            // Ищем всех, у кого есть скрипт Movement
            Movement[] allPlayers = GameObject.FindObjectsByType<Movement>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
            {
                // Если этот объект НЕ в сцене DontDestroyOnLoad — значит он новый и его надо убить
                if (p.gameObject.scene.name != "DontDestroyOnLoad")
                {
                    Debug.Log($"[SceneTransition] Удален дубликат Игрока из новой сцены: {p.gameObject.name}");
                    Destroy(p.gameObject);
                }
            }
        }

        // 2. Очистка VN Controller
        if (vnController != null)
        {
            // Ищем по тегу или по имени (лучше заранее назначить тег "VNController")
            GameObject[] controllers = GameObject.FindGameObjectsWithTag("VNController");
            foreach (var c in controllers)
            {
                if (c.scene.name != "DontDestroyOnLoad")
                {
                    Debug.Log($"[SceneTransition] Удален дубликат VN Controller: {c.name}");
                    Destroy(c);
                }
            }
        }

        // 3. Очистка Persistent UI
        if (persistentUI != null)
        {
            // Аналогично ищем по тегу "PersistentUI"
            GameObject[] uiRoots = GameObject.FindGameObjectsWithTag("PersistentUI");
            foreach (var ui in uiRoots)
            {
                if (ui.scene.name != "DontDestroyOnLoad")
                {
                    Debug.Log($"[SceneTransition] Удален дубликат Persistent UI: {ui.name}");
                    Destroy(ui);
                }
            }
        }
    }
    private void EnablePlayerMovement()
    {
        if (player == null) return;

        Movement movement = player.GetComponent<Movement>();
        if (movement != null)
            movement.enabled = true;
    }

    // --- Editor Buttons ---
    [Button("Test Transition Right?Left"), ButtonGroup("Test")]
    private void TestRightToLeft()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(TransitionCoroutine(SceneManager.GetActiveScene().buildIndex, TransitionDirection.RightToLeft));
    }

    [Button("Test Transition Left?Right"), ButtonGroup("Test")]
    private void TestLeftToRight()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(TransitionCoroutine(SceneManager.GetActiveScene().buildIndex, TransitionDirection.LeftToRight));
    }
}