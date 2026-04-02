using UnityEngine;

public class TriggerAudioMuter : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Объект с AudioSource, который нужно выключить")]
    [SerializeField] private AudioSource targetAudio;

    [Tooltip("Если true, аудио выключится навсегда. Если false, включится обратно при выходе из триггера")]
    [SerializeField] private bool muteForever = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что в триггер вошел игрок (убедись, что у игрока тег "Player")
        if (other.CompareTag("Player"))
        {
            if (targetAudio != null)
            {
                targetAudio.Pause(); // Можно использовать targetAudio.Stop() или targetAudio.mute = true;
                Debug.Log($"[AudioTrigger] Аудио на объекте {targetAudio.gameObject.name} приостановлено.");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Если мы не хотим вырубать звук навсегда, включаем его обратно при выходе
        if (!muteForever && other.CompareTag("Player"))
        {
            if (targetAudio != null)
            {
                targetAudio.UnPause();
                Debug.Log("[AudioTrigger] Аудио возобновлено.");
            }
        }
    }
}