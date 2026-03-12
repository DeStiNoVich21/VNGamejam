using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct ManagementResources
{
    public float energy,food,oxygen;

    public float energyDiffPerDay,foodDiffPerPerson, oxygenDiffPerPerson;
}

[DefaultExecutionOrder(-1)]
public class ResourcesManagementData : MonoBehaviour
{
    public static ResourcesManagementData Instance;
    public ManagementResources managementResources = new();

    public Slider food, oxygen, energy;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        GameManager.Instance.OnDayChange += OnNextDay;
    }

    private void Update()
    {
        food.value = managementResources.food;
        oxygen.value = managementResources.oxygen;
        energy.value = managementResources.energy;
    }

    public void OnNextDay(int currDay)
    {
        var bm = BreakerManager.Instance;

        managementResources.food += bm.foodMaker.transform.childCount > 0 ?
            Mathf.Min(managementResources.food, 30) : 0;

        EnergyLoss("Engine");

        if(bm.breackAbleSlot["Engine"].isWorking) 
            managementResources.energy = Mathf.Min(managementResources.energy + managementResources.energyDiffPerDay,100);
        managementResources.food -= managementResources.foodDiffPerPerson;
    }

    private void EnergyLoss(string name)
    {
        var bm = BreakerManager.Instance;
        if (!bm.breackAbleSlot[name].isWorking)
        {
            if (bm.breackAbleSlot[name].transform.childCount == 0)
            {
                managementResources.energy -= managementResources.energyDiffPerDay;
            }
        }
    }


    private void OnDestroy()
    {
        GameManager.Instance.OnDayChange -= OnNextDay;
    }
}
