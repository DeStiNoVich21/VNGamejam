using UnityEngine;

public class PersistentObjectsManager : MonoBehaviour
{
    public static PersistentObjectsManager instance { get; private set; }

    [SerializeField] private GameObject[] persistentObjects;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (var obj in persistentObjects)
            {
                if (obj != null)
                {
                    obj.transform.SetParent(transform);
                    Debug.Log($"[Persistent] {obj.name} attached to DontDestroyOnLoad");
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}