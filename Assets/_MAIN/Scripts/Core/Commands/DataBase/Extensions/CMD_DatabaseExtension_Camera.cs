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
            database.AddCommand("shake", new Action<string[]>(Shake));
            database.AddCommand("reset", new Action<string[]>(Reset));
            database.AddCommand("offset", new Action<string[]>(Offset));
        }

        private static void MoveTo(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-x", out float x);
            parameters.TryGetValue("-y", out float y);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 2f);

            CameraManager.Instance.MoveTo(new Vector2(x, y), speed);
        }

        private static void Follow(string[] data)
        {

        }

        private static void UnFollow(string[] data)
        {

        }

        private static void Zoom(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-size", out float size);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);

            CameraManager.Instance.Zoom(size, speed);
        }

        private static void Shake(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-intensity", out float intensity);
            parameters.TryGetValue("-duration", out float duration, defaultValue: 1f);

            CameraManager.Instance.Shake(intensity, duration);
        }

        private static void Reset(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);

            CameraManager.Instance.ResetCamera(speed);
        }

        private static void Offset(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-x", out float x);
            parameters.TryGetValue("-y", out float y);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);

            CameraManager.Instance.Offset(new Vector2(x, y), speed);
        }
    }
}