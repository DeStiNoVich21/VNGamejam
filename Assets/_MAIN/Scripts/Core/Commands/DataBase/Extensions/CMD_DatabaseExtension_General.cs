using DIALOGUE;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace COMMANDS
{
    public class CMD_DatabaseExtension_General : CMD_DatabaseExtension
    {
        private static string[] PARAM_SPEED = new string[] { "-spd", "-speed" };
        private static string[] PARAM_IMMEDIATE = new string[] { "-i", "-immediate" };
        private static string[] PARAM_FILEPATH = new string[] { "-f", "-file", "-filepath" };
        private static string[] PARAM_ENQUEUE = new string[] { "-e", "-enqueue" };

        new public static void Extend(CommandDatabase dataBase)
        {
            dataBase.AddCommand("wait", new Func<string, IEnumerator>(Wait));
            dataBase.AddCommand("pause", new Func<string, IEnumerator>(Wait));

            dataBase.AddCommand("showui", new Func<string[], IEnumerator>(ShowDialogueSystem));
            dataBase.AddCommand("showterminal", new Action<string[]>(ShowTerminal));
            dataBase.AddCommand("pauseconversation", new Action<string[]>(pauseConversation));
            dataBase.AddCommand("hideui", new Func<string[], IEnumerator>(HideDialogueSystem));

            dataBase.AddCommand("showdb", new Func<string[], IEnumerator>(ShowDialogueBox));
            dataBase.AddCommand("hidedb", new Func<string[], IEnumerator>(HideDialogueBox));

            dataBase.AddCommand("load", new Action<string[]>(LoadNewDialogueFile));

            dataBase.AddCommand("spawn", new Action<string[]>(Spawn));
            dataBase.AddCommand("nextday", new Action(NextDay));
        }

        public static void NextDay()
        {
            GameManager.Instance.NextDay();
        }

        public static void Spawn(string[] data)
        {
            string fileName = string.Empty;
            float x = 0, y = 0;
            var parameters = ConvertDataToParameters(data);
            parameters.TryGetValue(PARAM_FILEPATH, out fileName);
            parameters.TryGetValue("-x", out x);
            parameters.TryGetValue("-y", out y);
            string filePath = FilePaths.GetPathToResources("Prefabs/", fileName);
            GameObject prefab = Resources.Load<GameObject>(filePath);

            // Создаем объект в канвасе
            var inst = UnityEngine.Object.Instantiate(prefab, GameManager.Instance.GameCanvas.transform);

            // Получаем RectTransform
            RectTransform rectTransform = inst.GetComponent<RectTransform>();

            // Устанавливаем позицию в локальных координатах канваса
            rectTransform.anchoredPosition = new Vector2(x, y);

            // Сбрасываем масштаб и поворот
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
        }

        private static void ShowTerminal(string[] data)
        {
            GameManager.Instance.terminal.gameObject.SetActive(bool.Parse(data[0]));
        }

        private static void pauseConversation(string[] data)
        {
            DialogueSystem.instance.conversationManager.isPausedConversation = true;
        }

        private static void LoadNewDialogueFile(string[] data)
        {
            string fileName = string.Empty;
            bool enqueue = false;

            var parameters = ConvertDataToParameters(data);

            parameters.TryGetValue(PARAM_FILEPATH, out fileName);
            parameters.TryGetValue(PARAM_ENQUEUE, out enqueue,defaultValue: false);

            string filePath = FilePaths.GetPathToResources(FilePaths.resources_dialogueFile, fileName);
            TextAsset file = Resources.Load<TextAsset>(filePath);

            if (file == null)
            {
                Debug.LogWarning($"File '{filePath}' could not be loaded from dialogue files. Please ensure it exists within the '{FilePaths.resources_dialogueFile} resources folder.'");
                return;
            }

            List<string> lines = FileManager.ReadTextAsset(file, includeBlankLines: true);
            Conversation newConversation = new Conversation(lines);

            if (enqueue)
                DialogueSystem.instance.conversationManager.Enqueue(newConversation);
            else
                DialogueSystem.instance.conversationManager.StartConversation(newConversation);
        }

        private static IEnumerator Wait(string data)
        {
            if (float.TryParse(data, out float time))
            {
                yield return new WaitForSeconds(time);
            }
        }

        private static IEnumerator ShowDialogueBox(string[] data)
        {
            float speed;
            bool immediate;

            var parameters = ConvertDataToParameters(data);

            parameters.TryGetValue(PARAM_SPEED, out speed, defaultValue: 1f);
            parameters.TryGetValue(PARAM_IMMEDIATE, out immediate, defaultValue: false);

            yield return DialogueSystem.instance.dialogueContainer.Show(speed,immediate);
        }

        private static IEnumerator HideDialogueBox(string[] data)
        {
            float speed;
            bool immediate;

            var parameters = ConvertDataToParameters(data);

            parameters.TryGetValue(PARAM_SPEED, out speed, defaultValue: 1f);
            parameters.TryGetValue(PARAM_IMMEDIATE, out immediate, defaultValue: false);

            yield return DialogueSystem.instance.dialogueContainer.Hide(speed, immediate);
        }

        private static IEnumerator ShowDialogueSystem(string[] data)
        {
            float speed;
            bool immediate;

            var parameters = ConvertDataToParameters(data);

            parameters.TryGetValue(PARAM_SPEED, out speed, defaultValue: 1f);
            parameters.TryGetValue(PARAM_IMMEDIATE, out immediate, defaultValue: false);

            yield return DialogueSystem.instance.Show(speed,immediate);
        }

        private static IEnumerator HideDialogueSystem(string[] data)
        {
            float speed;
            bool immediate;

            var parameters = ConvertDataToParameters(data);

            parameters.TryGetValue(PARAM_SPEED, out speed, defaultValue: 1f);
            parameters.TryGetValue(PARAM_IMMEDIATE, out immediate, defaultValue: false);

            yield return DialogueSystem.instance.Hide(speed, immediate);
        }
    }
}