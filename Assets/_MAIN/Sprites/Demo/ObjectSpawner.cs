using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class SmartMultiSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        [HorizontalGroup("Split", 0.7f), HideLabel]
        public GameObject prefab;
        [HorizontalGroup("Split", 0.3f), LabelWidth(50)]
        public float weight = 1f;
    }

    [Title("Pool of Objects")]
    [TableList(AlwaysExpanded = true)]
    public List<SpawnEntry> spawnList = new List<SpawnEntry>();

    [Title("Spawn Logic")]
    [SceneObjectsOnly]
    public Transform targetParent;

    [Tooltip("Кол-во объектов в секунду (как Rate over Time)")]
    public float rateOverTime = 2f;

    [Range(0, 10)]
    public int burstCount = 1;

    [Title("Geometry")]
    public float roadWidth = 10f;
    public float travelDistance = 30f;

    [MinMaxSlider(0f, 20f, true)]
    public Vector2 speedRange = new Vector2(5f, 8f);

    private float _spawnTimer;
    [Title("Visuals")]
    [Tooltip("Развернуть спрайт по горизонтали при спавне?")]
    public bool reverseSprites = false;
    private void Update()
    {
        // Логика частоты как в Particle System
        float spawnThreshold = 1f / rateOverTime;
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= spawnThreshold)
        {
            for (int i = 0; i < burstCount; i++)
            {
                SpawnRandomObject();
            }
            _spawnTimer = 0;
        }
    }

    private void SpawnRandomObject()
    {
        if (spawnList == null || spawnList.Count == 0) return;

        GameObject prefab = GetWeightedRandomPrefab();
        if (prefab == null) return;

        // Расчет позиции
        float randomX = Random.Range(-roadWidth / 2f, roadWidth / 2f);
        Vector3 spawnPos = transform.position + transform.right * randomX;

        // Создаем с принудительной ротацией 0,0,0
        GameObject instance = Instantiate(prefab, spawnPos, Quaternion.identity, targetParent);

        // --- ДОБАВЬ ЭТОТ БЛОК ---
        if (reverseSprites)
        {
            Vector3 scale = instance.transform.localScale;
            scale.x *= -1;
            instance.transform.localScale = scale;
        }
        // ------------------------

        // Параметры движения
        float speed = Random.Range(speedRange.x, speedRange.y);
        StartCoroutine(MovementRoutine(instance.transform, speed));
    }

    private GameObject GetWeightedRandomPrefab()
    {
        float totalWeight = 0;
        foreach (var entry in spawnList) totalWeight += entry.weight;

        float randomValue = Random.Range(0, totalWeight);
        float cumulativeWeight = 0;

        foreach (var entry in spawnList)
        {
            cumulativeWeight += entry.weight;
            if (randomValue <= cumulativeWeight) return entry.prefab;
        }
        return null;
    }

    private IEnumerator MovementRoutine(Transform tr, float speed)
    {
        Vector3 startPos = tr.position;
        float distanceTravelled = 0;

        while (tr != null && distanceTravelled < travelDistance)
        {
            float step = speed * Time.deltaTime;
            // Двигаем "вперед" относительно направления спавнера, но не меняя ротацию самого объекта
            tr.position += transform.up * step;
            distanceTravelled += step;
            yield return null;
        }

        if (tr != null) Destroy(tr.gameObject);
    }

    private void OnDrawGizmos()
    {
        // Рисуем линию спавна (Ширина дороги)
        Gizmos.color = Color.cyan;
        Vector3 left = transform.position - transform.right * (roadWidth / 2f);
        Vector3 right = transform.position + transform.right * (roadWidth / 2f);
        Gizmos.DrawLine(left, right);

        // Рисуем границы пути
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Vector3 endLeft = left + transform.up * travelDistance;
        Vector3 endRight = right + transform.up * travelDistance;
        Gizmos.DrawLine(left, endLeft);
        Gizmos.DrawLine(right, endRight);
        Gizmos.DrawLine(endLeft, endRight);
    }
}