using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[DefaultExecutionOrder(1000)]
public class BreakerManager : SerializedMonoBehaviour
{
    public static BreakerManager Instance;

    public Dictionary<string,SlorOne> breackAbleSlot;

    public SlorOne foodMaker;
    public List<(string, bool,int,Characters)> repaired = new();

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        GameManager.Instance.OnDayChange += OnNextDay;
        OnNextDay(GameManager.Instance.currDay);
    }

    public void OnNextDay(int day)
    {
        repaired.Clear();
        foreach (var item in breackAbleSlot)
        {
            if(item.Value.transform.childCount > 0)
            {
                DragableElement child = item.Value.transform.GetComponentInChildren<DragableElement>();
                int rng = Random.Range(0, 10);
                if(child.characteristic.technique > rng)
                    item.Value.isWorking = true;
                Debug.Log(child.characteristic.technique > rng ? "Sucsecc" : "NO");
                repaired.Add((item.Key, child.characteristic.technique > rng,rng,child.characters));
            }
        }

        if(day == 0)
        {
            for (int i = 0; i < 4; i++)
                DestroyRandom();
        }

        for (int i = 0; i < 2; i++)
            DestroyRandom();

        foodMaker.isWorking = false;
    }

    public void DestroyRandom()
    {
        List<KeyValuePair<string, SlorOne>> candidates = new();

        foreach (var kv in breackAbleSlot)
        {
            if (!repaired.Any(r => r.Item1 == kv.Key))
                candidates.Add(kv);
        }

        if (candidates.Count == 0)
            return;

        var element = candidates[Random.Range(0, candidates.Count)];
        element.Value.isWorking = false;
    }


    private void OnDestroy()
    {
        GameManager.Instance.OnDayChange -= OnNextDay;
    }
}
