using UnityEngine;
using UnityEngine.SceneManagement;
[DefaultExecutionOrder(-1000)]
public class Bootstrap : MonoBehaviour
{
    [Scene]
    public string VnScene;
    private void Awake()
    {
        SceneManager.LoadSceneAsync(VnScene,LoadSceneMode.Additive);
    }
}
