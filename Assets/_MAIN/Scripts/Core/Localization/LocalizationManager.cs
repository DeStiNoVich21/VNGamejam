using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager instance { get; private set; }

    private Dictionary<string, string> strings = new();
    private string currentLanguage = "ru";

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        // Читаем сохранённый язык (по умолчанию ru)
        currentLanguage = PlayerPrefs.GetString("language", "ru");
        Load(currentLanguage);
    }

    public void SetLanguage(string lang)
    {
        currentLanguage = lang;
        PlayerPrefs.SetString("language", lang);
        Load(lang);

        // Говорим всем UI-элементам обновиться
        foreach (var elem in FindObjectsByType<LocalizedText>(FindObjectsSortMode.None))
            elem.Refresh();
    }

    private void Load(string lang)
    {
        strings.Clear();
        TextAsset file = Resources.Load<TextAsset>($"Localization/{lang}");
        if (file == null) { Debug.LogError($"Localization file not found: {lang}"); return; }

        // JsonUtility не поддерживает Dictionary напрямую, используем простой парсер:
        ParseJSON(file.text);
    }

    // Простой парсер — не нужен Newtonsoft
    private void ParseJSON(string json)
    {
        json = json.Trim().Trim('{', '}');
        foreach (var line in json.Split(','))
        {
            var parts = line.Split(new[] { ':' }, 2);
            if (parts.Length != 2) continue;
            string key = parts[0].Trim().Trim('"');
            string val = parts[1].Trim().Trim('"');
            // Обрабатываем переносы строк в JSON
            val = val.Replace("\\n", "\n");
            strings[key] = val;
        }
    }

    public string Get(string key)
    {
        if (strings.TryGetValue(key, out string val)) return val;
        Debug.LogWarning($"[Localization] Key not found: {key}");
        return key; // Возвращаем сам ключ — сразу видно что не переведено
    }
}