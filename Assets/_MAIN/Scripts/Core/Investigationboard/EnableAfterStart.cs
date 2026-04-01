using UnityEngine;
using System.Collections;

public class ForceEnableUI : MonoBehaviour
{
    [SerializeField] private float delay = 5.0f; // Задержка в секундах
    public InvestigationBoardUI targetScript;

    void Awake()
    {
        // Находим целевой скрипт на этом же объекте
        targetScript = GetComponent<InvestigationBoardUI>();

        // Выключаем его сразу, если он вдруг включен
        if (targetScript != null)
        {
            targetScript.enabled = false;
            Debug.Log($"<color=yellow>[ForceEnable]</color> Скрипт {targetScript.name} временно отключен...");
        }
    }

    void Start()
    {
        if (targetScript != null)
        {
            StartCoroutine(EnableAfterDelay());
        }
    }

    IEnumerator EnableAfterDelay()
    {
        // Ждем указанное время
        yield return new WaitForSeconds(delay);

        // Включаем обратно
        targetScript.enabled = true;

        // Если в скрипте есть логика открытия (Open), можно вызвать и её
        // targetScript.Open(); 

        Debug.Log($"<color=green>[ForceEnable]</color> Скрипт {targetScript.name} активирован спустя {delay} сек.");
    }
}