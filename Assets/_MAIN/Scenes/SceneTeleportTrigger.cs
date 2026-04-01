using UnityEngine;

public class SceneTeleportTrigger : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnID;
    [SerializeField] private SceneTransitionManager.TransitionDirection direction;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            SceneTransitionManager.instance.LoadScene(targetSceneName, targetSpawnID, direction);
        }
    }
}