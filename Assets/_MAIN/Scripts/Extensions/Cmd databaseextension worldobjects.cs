using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using COMMANDS;

/// <summary>
/// Расширение CommandManager для WorldObject'ов.
///
/// НЕ регистрирует глобальную базу — вместо этого WorldSceneDirector
/// вызывает WorldObjectCommands.RegisterTo(db, actorId) для каждого актёра.
/// Это позволяет писать в txt:
///   1.MoveTo(2 0 0 -spd 1.5)
///   1.SetActive(false)
///   1.PlayAnim(walk)
///   1.Flip()
/// </summary>
public class CMD_DatabaseExtension_WorldObjects : CMD_DatabaseExtension
{
    new public static void Extend(CommandDatabase database)
    {
        // Ничего не регистрируем глобально.
        // Регистрация идёт через WorldObjectCommands.RegisterTo() из WorldSceneDirector.
    }
}

/// <summary>
/// Статический хелпер — регистрирует команды для конкретного актёра в его sub-database.
/// </summary>
public static class WorldObjectCommands
{
    public static void RegisterTo(CommandDatabase db, string actorId)
    {
        db.AddCommand("moveto", new Func<string[], IEnumerator>(args => MoveTo(actorId, args)));
        db.AddCommand("setactive", new Action<string[]>(args => SetActive(actorId, args)));
        db.AddCommand("playanim", new Action<string[]>(args => PlayAnim(actorId, args)));
        db.AddCommand("setanimatorbool", new Action<string[]>(args => SetAnimatorBool(actorId, args)));
        db.AddCommand("setanimatortrigger", new Action<string[]>(args => SetAnimatorTrigger(actorId, args)));
        db.AddCommand("setanimatorfloat", new Action<string[]>(args => SetAnimatorFloat(actorId, args)));
        db.AddCommand("setanimatorint", new Action<string[]>(args => SetAnimatorInt(actorId, args)));
        db.AddCommand("setposition", new Action<string[]>(args => SetPosition(actorId, args)));
        db.AddCommand("rotateto", new Func<string[], IEnumerator>(args => RotateTo(actorId, args)));
        db.AddCommand("scaleto", new Func<string[], IEnumerator>(args => ScaleTo(actorId, args)));
        db.AddCommand("flip", new Action<string[]>(args => Flip(actorId, args)));
    }

    private static WorldObjectManager WOM => WorldObjectManager.instance;

    // MoveTo(x y z -spd 1.5)
    private static IEnumerator MoveTo(string id, string[] args)
    {
        if (!TryParseXY(args, out Vector3 target)) yield break;

        float speed = 2f;
        for (int i = 0; i < args.Length - 1; i++)
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

        Coroutine move = WOM.MoveTo(id, target, speed);
        if (move != null) yield return move;
    }

    // SetActive(true/false)
    private static void SetActive(string id, string[] args)
    {
        if (args.Length < 1) return;
        if (bool.TryParse(args[0], out bool active))
            WOM.SetActive(id, active);
    }

    // PlayAnim(stateName)
    private static void PlayAnim(string id, string[] args)
    {
        if (args.Length < 1) return;
        WOM.PlayAnim(id, args[0]);
    }

    // SetAnimatorBool(param true/false)
    private static void SetAnimatorBool(string id, string[] args)
    {
        if (args.Length < 2) return;
        if (bool.TryParse(args[1], out bool value))
            WOM.SetAnimatorBool(id, args[0], value);
    }

    // SetPosition(x y z)
    private static void SetPosition(string id, string[] args)
    {
        if (!TryParseXY(args, out Vector3 pos)) return;
        WOM.SetPosition(id, pos);
    }

    // Flip()
    private static void Flip(string id, string[] args)
    {
        WOM.Flip(id);
    }

    // SetAnimatorTrigger(param)
    private static void SetAnimatorTrigger(string id, string[] args)
    {
        if (args.Length < 1) return;
        WOM.SetAnimatorTrigger(id, args[0]);
    }

    // SetAnimatorFloat(param 1.5)
    private static void SetAnimatorFloat(string id, string[] args)
    {
        if (args.Length < 2) return;
        if (float.TryParse(args[1],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float value))
            WOM.SetAnimatorFloat(id, args[0], value);
    }

    // SetAnimatorInt(param 2)
    private static void SetAnimatorInt(string id, string[] args)
    {
        if (args.Length < 2) return;
        if (int.TryParse(args[1], out int value))
            WOM.SetAnimatorInt(id, args[0], value);
    }

    // RotateTo(0 90 0 -spd 60)
    private static IEnumerator RotateTo(string id, string[] args)
    {
        if (!TryParseXY(args, out Vector3 target)) yield break;

        float speed = 90f;
        for (int i = 0; i < args.Length - 1; i++)
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

        Coroutine c = WOM.RotateTo(id, target, speed);
        if (c != null) yield return c;
    }

    // ScaleTo(1.5 1.5 1 -spd 2)
    private static IEnumerator ScaleTo(string id, string[] args)
    {
        if (!TryParseXY(args, out Vector3 target)) yield break;

        float speed = 2f;
        for (int i = 0; i < args.Length - 1; i++)
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

        Coroutine c = WOM.ScaleTo(id, target, speed);
        if (c != null) yield return c;
    }

    // Парсим x y (z опционально) из начала args
    private static bool TryParseXY(string[] args, out Vector3 result)
    {
        result = Vector3.zero;
        List<float> vals = new List<float>();

        foreach (var arg in args)
        {
            if (arg.StartsWith("-") && !IsNumeric(arg)) break;
            if (float.TryParse(arg,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float v))
                vals.Add(v);
        }

        if (vals.Count < 2) return false;
        result = new Vector3(vals[0], vals[1], vals.Count > 2 ? vals[2] : 0f);
        return true;
    }

    private static bool IsNumeric(string s) =>
        float.TryParse(s,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out _);
}