using System;
using System.Collections;
using UnityEngine;
using COMMANDS;

/// <summary>
/// Команды управления слоями VN Controller.
/// Регистрируется автоматически через рефлексию.
///
/// Синтаксис в txt:
///   layer.SetActive(dialogue false)
///   layer.SetActive(characters true)
///   layer.FadeTo(dialogue 0 -spd 1.5)
///   layer.FadeTo(background 1 -spd 1.0)
/// </summary>
public class CMD_DatabaseExtension_Layers : CMD_DatabaseExtension
{
    new public static void Extend(CommandDatabase database)
    {
        CommandDatabase layerDB = CommandManager.instance.CreateSubDatabase("layer");

        layerDB.AddCommand("setactive", new Action<string[]>(SetActive));
        layerDB.AddCommand("fadeto", new Func<string[], IEnumerator>(FadeTo));
    }

    private static VNLayerManager LM => VNLayerManager.instance;

    // layer.SetActive(dialogue false)
    private static void SetActive(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.LogWarning("[Layers] SetActive: нужно layer.SetActive(имяСлоя true/false)");
            return;
        }

        if (bool.TryParse(args[1], out bool active))
            LM.SetLayerActive(args[0], active);
    }

    // layer.FadeTo(dialogue 0 -spd 1.5)
    private static IEnumerator FadeTo(string[] args)
    {
        if (args.Length < 2) yield break;

        string layerName = args[0];

        if (!float.TryParse(args[1],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float targetAlpha))
            yield break;

        float speed = 1f;
        for (int i = 2; i < args.Length - 1; i++)
        {
            if (args[i] == "-spd" || args[i] == "-speed")
            {
                float.TryParse(args[i + 1],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out speed);
                break;
            }
        }

        Coroutine c = LM.FadeLayer(layerName, targetAlpha, speed);
        if (c != null) yield return c;
    }
}