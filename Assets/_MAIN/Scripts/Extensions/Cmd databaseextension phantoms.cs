using System;
using System.Collections;
using UnityEngine;
using COMMANDS;

/// <summary>
/// Команды управления синхронизацией фантомов.
/// Регистрируется автоматически через рефлексию.
///
/// Синтаксис в txt файлах:
///
///   raise_sync(fury 10)
///   raise_sync(melancholy 5)
///   decrease_sync(ego 8)
///   check_sync(fury 50)          — печатает в лог текущее значение
///   print_sync()                  — печатает все значения в лог
///
/// Имена фантомов (регистр не важен):
///   genesis / melancholy / fury / stigma / ego
/// </summary>
public class CMD_DatabaseExtension_Phantoms : CMD_DatabaseExtension
{
    new public static void Extend(CommandDatabase database)
    {
        database.AddCommand("raise_sync", new Action<string[]>(RaiseSync));
        database.AddCommand("decrease_sync", new Action<string[]>(DecreaseSync));
        database.AddCommand("check_sync", new Action<string[]>(CheckSync));
        database.AddCommand("print_sync", new Action(PrintSync));
    }

    private static PhantomManager PM => PhantomManager.instance;

    // raise_sync(fury 10)
    private static void RaiseSync(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.LogWarning("[Phantoms] raise_sync: нужно raise_sync(имяФантома значение)");
            return;
        }

        if (!TryParsePhantom(args[0], out var type)) return;
        if (!float.TryParse(args[1],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float amount)) return;

        PM.Raise(type, amount);
    }

    // decrease_sync(fury 8)
    private static void DecreaseSync(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.LogWarning("[Phantoms] decrease_sync: нужно decrease_sync(имяФантома значение)");
            return;
        }

        if (!TryParsePhantom(args[0], out var type)) return;
        if (!float.TryParse(args[1],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float amount)) return;

        PM.Decrease(type, amount);
    }

    // check_sync(fury 50) — проверяет и печатает достигнут ли порог
    private static void CheckSync(string[] args)
    {
        if (args.Length < 1) return;

        if (!TryParsePhantom(args[0], out var type)) return;

        float current = PM.GetSync(type);

        if (args.Length >= 2 && float.TryParse(args[1],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float threshold))
        {
            bool reached = PM.HasReached(type, threshold);
            Debug.Log($"[Phantoms] {type}: {current:F0}% — порог {threshold}% {(reached ? "✓ достигнут" : "✗ не достигнут")}");
        }
        else
        {
            Debug.Log($"[Phantoms] {type}: {current:F0}%");
        }
    }

    // print_sync() — все значения в лог
    private static void PrintSync()
    {
        PM.PrintAll();
    }

    // ─── Парсер имени фантома ────────────────────────────────────────

    private static bool TryParsePhantom(string name, out PhantomManager.PhantomType type)
    {
        switch (name.ToLower().Trim())
        {
            case "genesis": type = PhantomManager.PhantomType.Genesis; return true;
            case "melancholy": type = PhantomManager.PhantomType.Melancholy; return true;
            case "fury": type = PhantomManager.PhantomType.Fury; return true;
            case "stigma": type = PhantomManager.PhantomType.Stigma; return true;
            case "ego": type = PhantomManager.PhantomType.Ego; return true;
            default:
                Debug.LogWarning($"[Phantoms] Неизвестный фантом: '{name}'. Допустимые: genesis, melancholy, fury, stigma, ego");
                type = PhantomManager.PhantomType.Ego;
                return false;
        }
    }
}