using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class SummaryManager : MonoBehaviour
{
    public static SummaryManager Instance;
    public TextMeshProUGUI textMeshPro;
    public CanvasGroup SummaryGroup;
    private void Awake()
    {
        if(Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        GameManager.Instance.OnDayChange += OnDayChange;
    }

    public void OnDayChange(int day)
    {
        textMeshPro.text = MakeSummary();
        SummaryGroup.alpha = 1;
        SummaryGroup.interactable = true;
        SummaryGroup.blocksRaycasts = true;
    }

    public string MakeSummary()
    {
        string summary = "";
        var bk = BreakerManager.Instance;
        summary = "Ежедневный отчет о состоянии оборудования и работах:\n";
        summary += "----------------------------------------------------\n\n";
        summary += "Сломанные структуры\n";
        foreach (var item in bk.breackAbleSlot)
        {
            if (!item.Value.isWorking)
            {
                summary += $"Был сломан {item.Key}\n";
            }
            summary += "\n";
        }
        summary += "----------------------------------------------------\n\n";
        summary += "Действия устранения\n";
        foreach (var item in bk.repaired)
        {
            summary +=
                $"{item.Item4.name} отправился(лась) работать в {item.Item1} и результатом стал " +
                (item.Item2 ? "успех" : "провал") +
                $" с шансом {item.Item4.characteristic.technique * 10}% \n";
            summary += "\n";
        }
        if(bk.repaired.Count == 0)
        {
            summary += "НИКАКИЕ \n";
        }
        return summary;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnDayChange -= OnDayChange;
    }
}
