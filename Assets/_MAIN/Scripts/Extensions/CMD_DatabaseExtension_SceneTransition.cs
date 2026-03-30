using System;
using COMMANDS;
using UnityEngine;

/// <summary>
/// Команды для переходов между сценами.
/// 
/// Синтаксис в txt файлах:
///   load_scene(SceneName)
///   load_scene(SceneName -dir RightToLeft)
///   load_scene(2)                           // по индексу
///   load_scene(NextLevel -dir TopToBottom)
/// 
/// Направления:
///   RightToLeft (по умолчанию)
///   LeftToRight
///   TopToBottom
///   BottomToTop
/// </summary>
public class CMD_DatabaseExtension_SceneTransition : CMD_DatabaseExtension
{
    new public static void Extend(CommandDatabase database)
    {
        database.AddCommand("load_scene", new Action<string[]>(LoadScene));
    }

    private static string[] PARAM_DIRECTION = new string[] { "-dir", "-direction" };

    private static void LoadScene(string[] args)
    {
        if (SceneTransitionManager.instance == null)
        {
            Debug.LogError("[SceneTransition] SceneTransitionManager не найден на сцене!");
            return;
        }

        if (args.Length < 1)
        {
            Debug.LogWarning("[SceneTransition] Нужно указать имя сцены: load_scene(SceneName)");
            return;
        }

        var parameters = ConvertDataToParameters(args);

        string sceneIdentifier = args[0];
        SceneTransitionManager.TransitionDirection direction = SceneTransitionManager.TransitionDirection.RightToLeft;

        // Проверяем параметр направления
        if (parameters.TryGetValue(PARAM_DIRECTION, out string directionString))
        {
            if (!TryParseDirection(directionString, out direction))
            {
                Debug.LogWarning($"[SceneTransition] Неизвестное направление '{directionString}', использую RightToLeft");
            }
        }

        // Загружаем сцену
        if (int.TryParse(sceneIdentifier, out int sceneIndex))
        {
            SceneTransitionManager.instance.LoadScene(sceneIndex, direction);
        }
        else
        {
            SceneTransitionManager.instance.LoadScene(sceneIdentifier, direction);
        }
    }

    private static bool TryParseDirection(string directionString, out SceneTransitionManager.TransitionDirection direction)
    {
        switch (directionString.ToLower().Replace("_", "").Replace("-", ""))
        {
            case "righttoleft":
            case "rtl":
                direction = SceneTransitionManager.TransitionDirection.RightToLeft;
                return true;

            case "lefttoright":
            case "ltr":
                direction = SceneTransitionManager.TransitionDirection.LeftToRight;
                return true;

            case "toptobottom":
            case "ttb":
                direction = SceneTransitionManager.TransitionDirection.TopToBottom;
                return true;

            case "bottomtotop":
            case "btt":
                direction = SceneTransitionManager.TransitionDirection.BottomToTop;
                return true;

            default:
                direction = SceneTransitionManager.TransitionDirection.RightToLeft;
                return false;
        }
    }
}