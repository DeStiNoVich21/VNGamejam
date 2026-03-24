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

        var ui = InvestigationBoardUI.instance;
        if (ui == null)
        {
            Debug.LogError("InvestigationBoardUI не найдена на сцене!");
            return;
        }

        // Закрываем и снова открываем доску, чтобы избежать дубликатов
        if (ui.isOpen) ui.Close();
        
        // Добавляем улики
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

        // Открываем заново (UI пересоздаст все визуалы корректно)
        ui.Open();

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