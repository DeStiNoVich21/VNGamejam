using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CHARACTERS;
using System.Linq;
using System.Security.Cryptography;

namespace COMMANDS
{
    public class CMD_DatabaseExtension_Camera : CMD_DatabaseExtension
    {
        new public static void Extend(CommandDatabase database)
        {
            database.AddCommand("moveto", new Action<string[]>(MoveTo));
            database.AddCommand("follow", new Action<string[]>(Follow));
            database.AddCommand("unfollow", new Action<string[]>(UnFollow));
            database.AddCommand("zoom", new Action<string[]>(Zoom));
            database.AddCommand("unzoom", new Action<string[]>(UnZoom));
            database.AddCommand("shake", new Action<string[]>(Shake));
        }

        private static void MoveTo(string[] data)
        {

        }

        private static void Follow(string[] data)
        {

        }

        private static void UnFollow(string[] data)
        {

        }

        private static void Zoom(string[] data)
        {

        }

        private static void UnZoom(string[] data)
        {

        }

        private static void Shake(string[] data)
        {

        }
    }
}