using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; // Обязательно для Debug
using static DIALOGUE.LogicalLines.LogicalLineUtils.Encapsulation;

namespace DIALOGUE.LogicalLines
{
    public class LL_Choice : ILogicalLine
    {
        public string keyword => "choice";
        private const char CHOICE_IDENTIFIER = '-';

        public IEnumerator Execute(DIALOGUE_LINE line)
        {
            // Отключаем AutoReader, чтобы он не конфликтовал с выборами
            if (DialogueSystem.instance.TryGetComponent<AutoReader>(out var autoReader))
            {
                if (autoReader.isOn) autoReader.Disable();
            }

            var currentConversation = DialogueSystem.instance.conversationManager.conversation;
            var progress = DialogueSystem.instance.conversationManager.conversationProgress;
            EncapsulatedData data = RipEncapsulationData(currentConversation, progress, ripHeaderAndEncapsulators: true);

            List<Choice> choices = GetChoicesFromData(data);

            if (choices.Count == 0)
            {
                Debug.LogError($"[LL_Choice] Ошибка! Парсер не нашел вариантов ответа в блоке: {line.dialogueData.rawData}");
                yield break;
            }

            string title = line.dialogueData.rawData;
            ChoicePanel panel = ChoicePanel.instance;
            string[] choiceTitles = choices.Select(c => c.title).ToArray();

            panel.Show(title, choiceTitles);

            while (panel.isWaitingOnUserChoice)
                yield return null;

            int selectedIndex = panel.lastDecision.answerIndex;

            // Защита от кривого индекса (краша игры)
            if (selectedIndex < 0 || selectedIndex >= choices.Count)
            {
                Debug.LogError($"[LL_Choice] Критическая ошибка! Получен индекс {selectedIndex}, но вариантов ответа всего {choices.Count}.");
                yield break; // Останавливаем выполнение, чтобы игра не упала
            }

            Choice selectionChoice = choices[selectedIndex];

            Conversation newConversation = new Conversation(selectionChoice.resultLines);
            DialogueSystem.instance.conversationManager.conversation.SetProgress(data.endingIndex);
            DialogueSystem.instance.conversationManager.EnqueuePriority(newConversation);
        }

        public bool Matches(DIALOGUE_LINE line)
        {
            return (line.hasSpeaker && line.speakerData.name.ToLower() == keyword);
        }

        // Полностью переработанный, пуленепробиваемый парсер
        private List<Choice> GetChoicesFromData(EncapsulatedData data)
        {
            List<Choice> choices = new List<Choice>();
            int encapsulationDepth = 0;
            bool isFirstChoice = true;

            Choice choice = new Choice
            {
                title = string.Empty,
                resultLines = new List<string>()
            };

            // Начинаем с 1, пропуская саму строку `choice "Заголовок"`
            for (int i = 1; i < data.lines.Count; i++)
            {
                string rawLine = data.lines[i];
                string trimmedLine = rawLine.Trim();

                // Пропускаем полностью пустые строки для логики (но не для текста!)
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    if (!isFirstChoice) choice.resultLines.Add(rawLine);
                    continue;
                }

                if (IsEncapsulationStart(trimmedLine))
                {
                    encapsulationDepth++;
                    if (encapsulationDepth == 1) continue; // Пропускаем главную открывающую скобку {
                }

                if (IsEncapsulationEnd(trimmedLine))
                {
                    encapsulationDepth--;
                    if (encapsulationDepth == 0) break; // Мы дошли до главной закрывающей скобки }
                }

                // Ищем варианты только на корневом уровне блока
                if (encapsulationDepth == 1 && IsChoiceStart(trimmedLine))
                {
                    if (!isFirstChoice)
                    {
                        choices.Add(choice); // Сохраняем предыдущий собранный вариант
                        choice = new Choice
                        {
                            title = string.Empty,
                            resultLines = new List<string>()
                        };
                    }

                    choice.title = trimmedLine.Substring(1).Trim();
                    isFirstChoice = false;
                    continue; // Переходим к следующей строке, не добавляя заголовок в результат
                }

                // Если это не служебная строка и мы уже нашли первый выбор, добавляем текст в вариант
                if (!isFirstChoice)
                {
                    choice.resultLines.Add(rawLine);
                }
            }

            // Не забываем добавить последний вариант в список
            if (!isFirstChoice)
            {
                choices.Add(choice);
            }

            return choices;
        }

        private bool IsChoiceStart(string line) => line.StartsWith(CHOICE_IDENTIFIER.ToString());

        private struct Choice
        {
            public string title;
            public List<string> resultLines;
        }
    }
}