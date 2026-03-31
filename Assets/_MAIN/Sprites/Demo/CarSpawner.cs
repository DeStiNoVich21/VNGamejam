using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

public class LaneCarSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CarSettings
    {
        [HorizontalGroup("Ref"), HideLabel, Required]
        public GameObject prefab;
        [VerticalGroup("Stats"), LabelText("Скорость")]
        public Vector2 speedRange = new Vector2(10f, 15f);
        [VerticalGroup("Stats"), LabelText("Шанс (Вес)")]
        public float weight = 1f;
    }

    [Title("Пул машин")]
    [TableList(AlwaysExpanded = true)]
    public List<CarSettings> carPool = new List<CarSettings>();

    [Title("Геометрия дороги")]
    [OnValueChanged("UpdateLanePositions")] // Теперь метод существует ниже
    public int laneCount = 3;

    [OnValueChanged("UpdateLanePositions")]
    public float roadWidth = 12f;
    public float travelDistance = 100f;

    [Title("Интервалы спавна")]
    [InfoBox("Минимальное время между спавном машин на ОДНОЙ и той же полосе")]
    public float minTimeBetweenSameLane = 2f;
    public float globalSpawnRate = 1.5f;

    [Title("Ротация и Иерархия")]
    public Vector3 spawnRotation = Vector3.zero;
    [SceneObjectsOnly]
    public Transform container;

    private float[] _laneLastSpawnTime;
    private float _globalTimer;

    // Этот метод лечит ошибку со скриншота
    private void UpdateLanePositions()
    {
        _laneLastSpawnTime = new float[laneCount];
        for (int i = 0; i < laneCount; i++)
            _laneLastSpawnTime[i] = -minTimeBetweenSameLane;
    }

    private void Start()
    {
        UpdateLanePositions();
    }

    private void Update()
    {
        _globalTimer += Time.deltaTime;
        if (_globalTimer >= 1f / globalSpawnRate)
        {
            TrySpawnCar();
            _globalTimer = 0;
        }
    }

    private void TrySpawnCar()
    {
        if (carPool == null || carPool.Count == 0) return;

        List<int> freeLanes = new List<int>();
        for (int i = 0; i < laneCount; i++)
        {
            if (Time.time - _laneLastSpawnTime[i] >= minTimeBetweenSameLane)
                freeLanes.Add(i);
        }

        if (freeLanes.Count == 0) return;

        int selectedLane = freeLanes[Random.Range(0, freeLanes.Count)];
        CarSettings settings = GetWeightedCar();

        if (settings != null && settings.prefab != null)
        {
            float laneX = GetXPosForLane(selectedLane);
            Vector3 spawnPos = transform.position + transform.right * laneX;

            GameObject car = Instantiate(settings.prefab, spawnPos, Quaternion.Euler(spawnRotation), container);
            _laneLastSpawnTime[selectedLane] = Time.time;

            float speed = Random.Range(settings.speedRange.x, settings.speedRange.y);
            StartCoroutine(DriveRoutine(car.transform, speed));
        }
    }

    private CarSettings GetWeightedCar()
    {
        float total = 0;
        foreach (var c in carPool) total += c.weight;
        float rnd = Random.Range(0, total);
        float current = 0;
        foreach (var c in carPool)
        {
            current += c.weight;
            if (rnd <= current) return c;
        }
        return null;
    }

    private float GetXPosForLane(int index)
    {
        if (laneCount <= 1) return 0;
        float step = roadWidth / (laneCount - 1);
        return -roadWidth / 2f + (step * index);
    }

    private IEnumerator DriveRoutine(Transform tr, float speed)
    {
        float dist = 0;
        while (tr != null && dist < travelDistance)
        {
            float step = speed * Time.deltaTime;
            tr.position += transform.up * step;
            dist += step;
            yield return null;
        }
        if (tr != null) Destroy(tr.gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i < laneCount; i++)
        {
            float x = GetXPosForLane(i);
            Vector3 pos = transform.position + transform.right * x;
            Gizmos.DrawSphere(pos, 0.2f);
            Gizmos.DrawRay(pos, transform.up * travelDistance);
        }
    }
}