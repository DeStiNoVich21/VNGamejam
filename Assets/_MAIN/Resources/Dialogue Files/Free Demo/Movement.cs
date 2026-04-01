using UnityEngine;
using UnityEngine.SceneManagement;

public class Movement : MonoBehaviour
{
    public float speed = 5f;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }
    private void OnEnable()
    {
        // Подписываемся на событие загрузки сцены
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Обязательно отписываемся при уничтожении, чтобы избежать утечек памяти
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
       this.enabled = true; // Включаем скрипт при загрузке новой сцены
    }
    void Update()
    {
        // Получаем ввод (A/D или стрелки)
        moveInput = Input.GetAxisRaw("Horizontal");

        // Управление анимацией
        anim.SetBool("isIdle", moveInput == 0);

        // Разворот спрайта
        if (moveInput > 0) sr.flipX = false;
        else if (moveInput < 0) sr.flipX = true;
    }

    void FixedUpdate()
    {
        // Физическое перемещение
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
    }
}
