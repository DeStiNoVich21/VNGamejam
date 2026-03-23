using UnityEngine;
using Sirenix.OdinInspector; // Используем для удобной кнопки в инспекторе

public class InvestigationTestHelper : MonoBehaviour
{
    [Title("Тестирование")]
    [InfoBox("Нажми кнопку ниже, чтобы выдать себе все улики из списка 'All Stickers'")]

    [Button("ДОБАВИТЬ ВСЕ УЛИКИ В ИНВЕНТАРЬ", ButtonSizes.Large)]
    public void AddAllStickersToInventory()
    {
        var mgr = InvestigationBoardManager.instance;

        if (mgr == null)
        {
            Debug.LogError("InvestigationBoardManager не найден на сцене!");
            return;
        }

        // Мы не можем напрямую залезть в приватный список allStickers, 
        // но мы можем схитрить и попросить менеджер добавить их по ID.
        // Для этого нам нужно знать, какие ID там лежат.

        // В твоем случае проще всего добавить конкретные ID вручную для теста:
        mgr.AddSticker("Kyle");
        mgr.AddSticker("Homicide");
        mgr.AddSticker("404 apartment");
        mgr.AddSticker("Evening");
        mgr.AddSticker("Order");
        mgr.AddSticker("Drug");
        mgr.AddSticker("Family_Photo");
        mgr.AddSticker("Dogtag");
        mgr.AddSticker("EvictionNotice");
        mgr.AddSticker("Gun");
        mgr.AddSticker("DrugBlood");
      



        // ... и так далее по списку из твоего скриншота

        Debug.Log("Тестовые улики добавлены. Проверь панель инвентаря!");
    }

    [Button("ОЧИСТИТЬ ВСЁ", ButtonSizes.Medium)]
    public void ClearTest()
    {
        // Если нужно быстро сбросить состояние для теста
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}