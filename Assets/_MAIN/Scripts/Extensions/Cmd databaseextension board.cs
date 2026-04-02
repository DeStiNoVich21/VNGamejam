using System;
using UnityEngine;
using COMMANDS;

/// <summary>
///  оманды доски расследовани€ в txt файлах.
///
///   add_sticker(kyle_file)           Ч улика по€вл€етс€ на доске
///   add_fact(fury kyle_motive)       Ч факт от фантома (провер€ет синхронизацию)
///   remove_sticker(kyle_file)        Ч убрать стикер
/// </summary>
public class CMD_DatabaseExtension_Board : CMD_DatabaseExtension
{
    new public static void Extend(CommandDatabase database)
    {
        database.AddCommand("add_sticker", new Action<string[]>(AddSticker));
        database.AddCommand("add_fact", new Action<string[]>(AddFact));
        database.AddCommand("remove_sticker", new Action<string[]>(RemoveSticker));

        database.AddCommand("show_board", new Action(ShowBoard));
        database.AddCommand("hide_board", new Action(HideBoard));
    }

    private static InvestigationBoardManager Board => InvestigationBoardManager.instance;

    // add_sticker(kyle_file)
    private static void AddSticker(string[] args)
    {
        if (args.Length < 1)
        {
            Debug.LogWarning("[Board] add_sticker: нужно add_sticker(id)");
            return;
        }
        Board.AddSticker(args[0].Trim());
    }

    // add_fact(fury kyle_motive) Ч фантом fury даЄт факт kyle_motive
    private static void AddFact(string[] args)
    {
        if (args.Length < 2)
        {
            Debug.LogWarning("[Board] add_fact: нужно add_fact(фантом id_факта)");
            return;
        }
        Board.AddFact(args[0].Trim(), args[1].Trim());
    }

    // remove_sticker(kyle_file)
    private static void RemoveSticker(string[] args)
    {
        if (args.Length < 1) return;
        Board.RemoveSticker(args[0].Trim());
    }
    private static void ShowBoard()
    {
        if (InvestigationBoardUI.instance != null)
        {
            InvestigationBoardUI.instance.Open();
        }
        else
        {
            Debug.LogWarning("[Board] show_board: доска расследовани€ не найдена в сцене");
        }
    }

    private static void HideBoard()
    {
        if (InvestigationBoardUI.instance != null)
            InvestigationBoardUI.instance.Close();
        else
        {
            Debug.LogWarning("[Board] show_board: доска расследовани€ не найдена в сцене");
        }
    }

}