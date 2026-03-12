using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int currDay;
    public Action<int> OnDayChange;

    public static GameManager Instance;

    public RectTransform terminal;

    public Canvas GameCanvas;

    private void Awake()
    {
        if(Instance == null) 
            Instance = this;
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        VariableStore.CreateVariable("currDay", currDay, () => currDay, value => 
        { 
            currDay = value;
        });
    }

    [Button]
    public void NextDay()
    {
        currDay++;
        OnDayChange?.Invoke(currDay);

        VariableStore.TrySetValue("currDay", currDay);
    }
}
