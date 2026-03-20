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
            database.AddCommand("stopfollow", new Action<string[]>(StopFollow));
            database.AddCommand("lookat", new Action<string[]>(LookAt));
            database.AddCommand("zoom", new Action<string[]>(Zoom));
            database.AddCommand("unzoom", new Action<string[]>(UnZoom));
            database.AddCommand("shake", new Action<string[]>(Shake));
            database.AddCommand("reset", new Action<string[]>(Reset));
            database.AddCommand("resetpos", new Action<string[]>(ResetPos));
            database.AddCommand("offset", new Action<string[]>(Offset));
            database.AddCommand("setbounds", new Action<string[]>(SetBounds));
            database.AddCommand("removebounds", new Action<string[]>(RemoveBounds));
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
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-id", out string id);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);
            
            Debug.Log($"[Camera Follow] Считан ID: '{id}'");

            if (!string.IsNullOrEmpty(id) && WorldObjectManager.instance.TryGet(id, out GameObject go))
            {
                Debug.Log($"[Camera Follow] Объект с ID '{id}' найден: {go.name}. Назначаем плавное слежение (speed {speed}).");
                CameraManager.Instance.Follow(go.transform, speed);
            }
            else
            {
                Debug.LogWarning($"[Camera Follow] ОШИБКА: ID пуст или объект '{id}' не найден в WorldObjectManager!");
            }
        }

        private static void StopFollow(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);

            CameraManager.Instance.StopFollow(speed);
        }

        private static void LookAt(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-id", out string id);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 2f);
            parameters.TryGetValue(new string[] { "-dur", "-duration" }, out float duration, defaultValue: 2f);

            Debug.Log($"[Camera LookAt] Считан ID: '{id}', speed: {speed}, duration: {duration}");

            if (!string.IsNullOrEmpty(id) && WorldObjectManager.instance.TryGet(id, out GameObject go))
            {
                Debug.Log($"[Camera LookAt] Объект с ID '{id}' найден. Запускаем наведение.");
                CameraManager.Instance.LookAt(go.transform, speed, duration);
            }
            else
            {
                Debug.LogWarning($"[Camera LookAt] ОШИБКА: Объект '{id}' не найден!");
            }
        }

        private static void Zoom(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-size", out float size);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);

            CameraManager.Instance.Zoom(size, speed);
        }

        private static void UnZoom(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);
            CameraManager.Instance.ResetZoom(speed);
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

        private static void ResetPos(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);
            CameraManager.Instance.ResetPos(speed);
        }

        private static void Offset(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-x", out float x);
            parameters.TryGetValue("-y", out float y);
            parameters.TryGetValue(new string[] { "-spd", "-speed" }, out float speed, defaultValue: 1f);

            CameraManager.Instance.Offset(new Vector2(x, y), speed);
        }

        private static void SetBounds(string[] data)
        {
            CommandParameters parameters = ConvertDataToParameters(data);
            parameters.TryGetValue("-minX", out float minX);
            parameters.TryGetValue("-maxX", out float maxX);
            parameters.TryGetValue("-minY", out float minY);
            parameters.TryGetValue("-maxY", out float maxY);

            if (WorldObjectManager.instance != null && WorldObjectManager.instance.TryGet("1", out GameObject player))
            {
                Vector3 pPos = player.transform.position;
                CameraManager.Instance.SetBounds(pPos.x + minX, pPos.x + maxX, pPos.y + minY, pPos.y + maxY);
                Debug.Log($"[Camera Bounds] Границы установлены относительно игрока: X({pPos.x + minX} до {pPos.x + maxX}), Y({pPos.y + minY} до {pPos.y + maxY})");
            }
            else
            {
                CameraManager.Instance.SetBounds(minX, maxX, minY, maxY);
                Debug.Log($"[Camera Bounds] Игрок 1 не найден. Установлены абсолютные границы: X({minX} до {maxX}), Y({minY} до {maxY})");
            }
        }

        private static void RemoveBounds(string[] data)
        {
            CameraManager.Instance.RemoveBounds();
        }
    }
}