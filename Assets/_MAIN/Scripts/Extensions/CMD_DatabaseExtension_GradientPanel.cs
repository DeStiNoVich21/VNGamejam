using System;
using COMMANDS;
using UnityEngine;

/// <summary>
/// Команды для управления панелью градиента фантомов.
/// 
/// Синтаксис в txt файлах:
///   show_gradient(dominion)
///   show_gradient(zenith)
///   show_gradient(stigma)
///   hide_gradient()
/// </summary>
public class CMD_DatabaseExtension_GradientPanel : CMD_DatabaseExtension
{
    new public static void Extend(CommandDatabase database)
    {
        database.AddCommand("show_gradient", new Action<string[]>(ShowGradient));
        database.AddCommand("hide_gradient", new Action(HideGradient));
    }

    private static void ShowGradient(string[] args)
    {
        if (GradientPanelManager.instance == null)
        {
            Debug.LogWarning("[GradientPanel] GradientPanelManager не найден на сцене!");
            return;
        }

        if (args.Length < 1)
        {
            Debug.LogWarning("[GradientPanel] Нужно указать имя фантома: show_gradient(dominion)");
            return;
        }

        if (!TryParsePhantom(args[0], out var type))
            return;

        GradientPanelManager.instance.Show(type);
    }

    private static void HideGradient()
    {
        if (GradientPanelManager.instance == null)
            return;

        GradientPanelManager.instance.HideImmediate();
    }

    private static bool TryParsePhantom(string name, out PhantomManager.PhantomType type)
    {
        switch (name.ToLower().Trim())
        {
            case "dominion": type = PhantomManager.PhantomType.Dominion; return true;
            case "zenith": type = PhantomManager.PhantomType.Zenith; return true;
            case "stigma": type = PhantomManager.PhantomType.Stigma; return true;
            default:
                Debug.LogWarning($"[GradientPanel] Неизвестный фантом: '{name}'");
                type = PhantomManager.PhantomType.Dominion;
                return false;
        }
    }
}