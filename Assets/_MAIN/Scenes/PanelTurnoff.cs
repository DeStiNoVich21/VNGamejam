using UnityEngine;
using UnityEngine.SceneManagement;

public class PanelTurnoff : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

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
        if (targetObject != null)
        {
            targetObject.SetActive(false);
            Debug.Log($"[Cleaner] Объект {targetObject.name} был отключен в сцене: {scene.name}");
        }
    }
}
