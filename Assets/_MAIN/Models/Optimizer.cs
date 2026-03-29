using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Optimizer : MonoBehaviour
{
    [Header("Настройки дистанции")]
    public Transform player;
    public float viewDistance = 50f;
    public float checkInterval = 0.3f;

    [Header("Фильтр объектов")]
    [Tooltip("Объекты с этими тегами будут оптимизироваться")]
    public List<string> tagsToOptimize = new List<string> { "Environment", "NPC" };

    private List<GameObject> _cachedObjects = new List<GameObject>();

    void Start()
    {
        if (player == null)
            player = Camera.main.transform;

        CacheObjectsByTags();
        StartCoroutine(OptimizationRoutine());
    }

    // Собираем все объекты с нужными тегами в один список
    void CacheObjectsByTags()
    {
        _cachedObjects.Clear();
        foreach (string tag in tagsToOptimize)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
            _cachedObjects.AddRange(targets);
        }

        Debug.Log($"Оптимизатор: кэшировано {_cachedObjects.Count} объектов.");
    }

    IEnumerator OptimizationRoutine()
    {
        while (true)
        {
            Vector3 playerPos = player.position;
            float sqrViewDistance = viewDistance * viewDistance; // Используем квадрат расстояния для скорости

            foreach (GameObject obj in _cachedObjects)
            {
                if (obj == null) continue;

                // Считаем квадрат расстояния (быстрее, чем Vector3.Distance)
                float sqrDistance = (obj.transform.position - playerPos).sqrMagnitude;

                // Включаем/выключаем объект
                bool shouldBeActive = sqrDistance < sqrViewDistance;

                if (obj.activeSelf != shouldBeActive)
                {
                    obj.SetActive(shouldBeActive);
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }
}
