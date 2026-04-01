using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class WorldSceneAutoLinker : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string playerActorID = "Player"; // ID дл€ системы диалогов
    [SerializeField] private Movement playerMovement;
    [SerializeField] private GameObject vnController;

    private void Start()
    {
        LinkAndRegister();
    }

    public void LinkAndRegister()
    {
        WorldSceneManager manager = WorldSceneManager.instance;

        if (manager == null)
        {
            Debug.LogWarning("[AutoLinker] WorldSceneManager не найден!");
            return;
        }

        // 1. ѕоиск компонентов, если они не заданы
        if (playerMovement == null)
            playerMovement = GetComponent<Movement>();

        if (vnController == null)
            vnController = GameObject.Find("[VN Controller]");

        // 2. ¬недрение ссылок (через рефлексию, чтобы не мен€ть ваш оригинальный класс)
        System.Type type = manager.GetType();

        FieldInfo moveField = type.GetField("playerMovement", BindingFlags.NonPublic | BindingFlags.Instance);
        moveField?.SetValue(manager, playerMovement);

        FieldInfo vnField = type.GetField("vnController", BindingFlags.NonPublic | BindingFlags.Instance);
        vnField?.SetValue(manager, vnController);

        // 3. ƒобавление игрока в список actors
        FieldInfo actorsField = type.GetField("actors", BindingFlags.NonPublic | BindingFlags.Instance);
        if (actorsField != null)
        {
            List<WorldActor> actorsList = (List<WorldActor>)actorsField.GetValue(manager);

            // ѕровер€ем, нет ли уже игрока в списке, чтобы не дублировать при перезагрузках
            if (!actorsList.Exists(a => a.id == playerActorID))
            {
                WorldActor playerAsActor = new WorldActor
                {
                    id = playerActorID,
                    go = this.gameObject
                };

                actorsList.Add(playerAsActor);

                // 4. ¬ажно: принудительно регистрируем игрока в WorldObjectManager, 
                // так как Start в WorldSceneManager мог уже отработать
                RegisterPlayerInSystems(manager, playerAsActor);

                Debug.Log($"[AutoLinker] »грок '{playerActorID}' добавлен в список актеров.");
            }
        }
    }

    private void RegisterPlayerInSystems(WorldSceneManager manager, WorldActor playerActor)
    {
        // »спользуем метод рефлексии дл€ вызова приватного метода регистрации, 
        // чтобы игрок сразу стал доступен дл€ команд диалоговой системы
        MethodInfo registerMethod = manager.GetType().GetMethod("RegisterActors", BindingFlags.NonPublic | BindingFlags.Instance);

        // ≈сли мы просто вызовем RegisterActors(), он перерегистрирует всех. 
        // Ёто безопасно, так как WorldObjectManager обычно перезаписывает ключи.
        registerMethod?.Invoke(manager, null);
    }
}