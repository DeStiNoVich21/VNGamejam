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
            DontDestroyOnLoad(gameObject);
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

        // Панель изначально невидима
        if (transitionPanel != null)
            transitionPanel.gameObject.SetActive(false);
    }

    private void SetupPersistentObjects()
    {
        // Player
        if (player != null)
        {
            if (player.transform.parent != null)
                player.transform.SetParent(null);
            DontDestroyOnLoad(player);
        }

        // Persistent UI
        if (persistentUI != null)
        {
            if (persistentUI.transform.parent != null)
                persistentUI.transform.SetParent(null);
            DontDestroyOnLoad(persistentUI);
        }

        // VN Controller
        if (vnController != null)
        {
            if (vnController.transform.parent != null)
                vnController.transform.SetParent(null);
            DontDestroyOnLoad(vnController);
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
            yield return SceneManager.LoadSceneAsync(sceneName);
        else if (sceneIdentifier is int sceneIndex)
            yield return SceneManager.LoadSceneAsync(sceneIndex);

        // Небольшая пауза
        yield return new WaitForSeconds(0.1f);

        // Wipe OUT (панель открывает экран)
        yield return StartCoroutine(WipeOut(direction));

        // Включаем движение игрока
        EnablePlayerMovement();

        isTransitioning = false;
    }

    private IEnumerator WipeIn(TransitionDirection direction)
    {
        transitionPanel.gameObject.SetActive(true);

        Vector2 startPos = GetStartPosition(direction, isWipeIn: true);
        Vector2 endPos = Vector2.zero;

        transitionPanel.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < wipeDuration)
        {
            elapsed += Time.deltaTime;
            float t = wipeCurve.Evaluate(elapsed / wipeDuration);
            transitionPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        transitionPanel.anchoredPosition = endPos;
    }

    private IEnumerator WipeOut(TransitionDirection direction)
    {
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = GetStartPosition(direction, isWipeIn: false);

        transitionPanel.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < wipeDuration)
        {
            elapsed += Time.deltaTime;
            float t = wipeCurve.Evaluate(elapsed / wipeDuration);
            transitionPanel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        transitionPanel.anchoredPosition = endPos;
        transitionPanel.gameObject.SetActive(false);
    }

    private Vector2 GetStartPosition(TransitionDirection direction, bool isWipeIn)
    {
        if (canvas == null)
            return Vector2.zero;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
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

    [Button("Test Transition Top?Bottom"), ButtonGroup("Test")]
    private void TestTopToBottom()
    {
        if (!Application.isPlaying) return;
        StartCoroutine(TransitionCoroutine(SceneManager.GetActiveScene().buildIndex, TransitionDirection.TopToBottom));
    }
}