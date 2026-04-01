using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;
    private TMP_Text label;

    private void Start()
    {
        label = GetComponent<TMP_Text>();
        Refresh();
    }

    public void Refresh()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (LocalizationManager.instance != null)
            label.text = LocalizationManager.instance.Get(key);
    }
}