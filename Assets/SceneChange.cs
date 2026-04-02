using UnityEngine;
using UnityEngine.SceneManagement; // Обязательно для работы со сценами
public class SceneChange : MonoBehaviour
{
    [Header("Настройки перехода")]
    [Tooltip("Точное название сцены в окне Build Settings")]
    [SerializeField] private string sceneName;

    // Метод для вызова (например, через OnClick у кнопки)
    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Имя сцены не указано в инспекторе!");
        }
    }

    // Метод для загрузки по индексу (если так удобнее)
    public void LoadSceneByIndex(int index)
    {
        SceneManager.LoadScene(index);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player")) // Убедитесь, что у игрока есть тег "Player"
        {
            LoadTargetScene();
        }
    }
}
