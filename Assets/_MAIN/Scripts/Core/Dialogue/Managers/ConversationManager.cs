using CHARACTERS;
using COMMANDS;
using DIALOGUE.LogicalLines;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

namespace DIALOGUE
{
    public class ConversationManager
    {
        private DialogueSystem dialogueSystem => DialogueSystem.instance;

        private Coroutine process = null;
        public bool isRunning => process != null;

        public TextArchitect architect = null;
        private bool userPromt = false;

        private TextMeshProUGUI nameText;

        private LogicalLineManager logicalLineManager;

        public Conversation conversation => (conversationQueue.IsEmpty() ? null : conversationQueue.top);
        public int conversationProgress => (conversationQueue.IsEmpty() ? -1 : conversationQueue.top.GetProgress());

        private ConversationQueue conversationQueue;

        public struct DialogueSegment
        {
            public string text;
            public float pause;
            public float speed;
        }

        public ConversationManager(TextArchitect architect, TextMeshProUGUI nameText)
        {
            this.architect = architect;
            this.nameText = nameText;

            dialogueSystem.onUserPromt_Next += OnUserPromt_Next;
            logicalLineManager = new LogicalLineManager();
            conversationQueue = new ConversationQueue();
        }

        private List<DialogueSegment> DeconstructDialogue(string dialogue)
        {
            List<DialogueSegment> segments = new List<DialogueSegment>();

            string pattern = @"\{\s*(pause|spd)\s*=\s*([^}]+)\s*\}|\{\s*/(spd)\s*\}";
            var matches = Regex.Matches(dialogue, pattern);

            int lastIndex = 0;
            float currentSpeed = 1f;
            const float defaultSpeed = 1f;

            foreach (Match match in matches)
            {
                string subText = dialogue.Substring(lastIndex, match.Index - lastIndex);
                if (!string.IsNullOrEmpty(subText))
                {
                    segments.Add(new DialogueSegment { text = subText, speed = currentSpeed });
                }

                if (match.Groups[3].Success)
                {
                    currentSpeed = defaultSpeed;
                }
                else 
                {
                    string tagType = match.Groups[1].Value;
                    float val = float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);

                    if (tagType == "pause")
                    {
                        segments.Add(new DialogueSegment { pause = val });
                    }
                    else if (tagType == "spd")
                    {
                        currentSpeed = val;
                    }
                }

                lastIndex = match.Index + match.Length;
            }

            string remaining = dialogue.Substring(lastIndex);
            if (!string.IsNullOrEmpty(remaining))
            {
                segments.Add(new DialogueSegment { text = remaining, speed = currentSpeed });
            }

            return segments;
        }

        public void SetCharacterName(string rawName)
        {
            if (nameText == null) return;

            string cleanName = TextEffectParser.ParseCustomTags(rawName, out var nameEffects);

            nameText.text = cleanName;

            var animator = nameText.GetComponent<TextVertexAnimator>();
            if (animator != null)
            {
                animator.SetEffects(nameEffects);
            }
        }

        public void Enqueue(Conversation conversation) => conversationQueue.Enqueue(conversation);
        public void EnqueuePriority(Conversation conversation) => conversationQueue.EnqueuePriotity(conversation);

        private void OnUserPromt_Next()
        {
            userPromt = true;
        }

        public Coroutine StartConversation(Conversation conversation)
        {
            StopConversation();
            conversationQueue.Clear();
            Enqueue(conversation);
            process = dialogueSystem.StartCoroutine(RunningConversation());
            return process;
        }

        public void StopConversation()
        {
            if (!isRunning) return;
            dialogueSystem.StopCoroutine(process);
            process = null;
        }

        IEnumerator RunningConversation()
        {
            while (!conversationQueue.IsEmpty())
            {
                Conversation currentConversation = conversation;

                if (currentConversation.HasReachedEnd())
                {
                    conversationQueue.Dequeue();
                    continue;
                }

                string rawLin = currentConversation.CurrentLine();

                if (string.IsNullOrWhiteSpace(rawLin))
                {
                    TryAdvanceConversation(currentConversation);
                    continue;
                }

                DIALOGUE_LINE line = DialogueParser.Parse(rawLin);

                if (logicalLineManager.TryGetLogic(line, out Coroutine logic))
                {
                    yield return logic;
                }
                else
                {
                    if (line.hasDialogue)
                        yield return Line_RunDialogue(line);

                    if (line.hasCommands)
                        yield return Line_RunCommands(line);

                    yield return PauseConversation();

                    if (line.hasDialogue)
                    {
                        yield return WaitForUserInput();
                        CommandManager.instance.StopAllProcesses();
                    }
                }
                TryAdvanceConversation(currentConversation);
            }
            process = null;
        }

        public bool isPausedConversation;

        public IEnumerator PauseConversation()
        {
            while (isPausedConversation)
            {
                yield return null;
            }
        }

        private void TryAdvanceConversation(Conversation conversation)
        {
            conversation.IncrementProgress();
            if (conversation != conversationQueue.top) return;
            if (conversation.HasReachedEnd()) conversationQueue.Dequeue();
        }

        IEnumerator Line_RunDialogue(DIALOGUE_LINE line)
        {
            if (line.hasSpeaker)
                HandleSpeakerLogic(line.speakerData);
            else
                SetCharacterName(""); 

            if (!dialogueSystem.dialogueContainer.isVisible)
                dialogueSystem.dialogueContainer.Show();

            yield return BuildLineSegments(line.dialogueData);
        }

        private void HandleSpeakerLogic(DL_SPEAKER_DATA speakerData)
        {
            bool characterMustBeCreated = (speakerData.makeCharacterEnter || speakerData.isCastingPosition || speakerData.isCastingExpressions);
            Character character = CharacterManager.instance.GetCharacter(speakerData.name, createIfDoesNotExist: characterMustBeCreated);

            if (speakerData.makeCharacterEnter && (!character.isVisible && !character.isRevealing))
                character.Show();

            SetCharacterName(TagManager.Inject(speakerData.displayname));

            DialogueSystem.instance.ApplySpeakerDataToDialogueContainer(speakerData.name);

            if (speakerData.isCastingPosition)
                character.MoveToPosition(speakerData.castPosition);

            if (speakerData.isCastingExpressions)
            {
                foreach (var ce in speakerData.CastExpressions)
                    character.OnReceiveCastingExpression(ce.layer, ce.expression);
            }
        }

        IEnumerator Line_RunCommands(DIALOGUE_LINE line)
        {
            List<DL_COMMAND_DATA.Command> commands = line.commandsData.commands;
            foreach (DL_COMMAND_DATA.Command command in commands)
            {
                if (command.waitForCompletion || command.name == "wait")
                {
                    CoroutineWrapper cw = CommandManager.instance.Execute(command.name, command.arguments);
                    while (!cw.isDone)
                    {
                        if (userPromt)
                        {
                            CommandManager.instance.StopCurrentProcess();
                            userPromt = false;
                        }
                        yield return null;
                    }
                }
                else
                    CommandManager.instance.Execute(command.name, command.arguments);
            }
            yield return null;
        }

        IEnumerator BuildLineSegments(DL_DIALOGUE_DATA line)
        {
            for (int i = 0; i < line.segments.Count; i++)
            {
                DL_DIALOGUE_DATA.DIALOGUE_SEGMENT segment = line.segments[i];
                yield return WaitForDialogueSegmentSignalToBeTriggered(segment);
                yield return BuildDialogue(segment.dialogue, segment.appendText);
            }
        }

        public bool isWaitingOnAutoTimer { get; private set; } = false;

        IEnumerator WaitForDialogueSegmentSignalToBeTriggered(DL_DIALOGUE_DATA.DIALOGUE_SEGMENT segment)
        {
            switch (segment.startSignal)
            {
                case DL_DIALOGUE_DATA.DIALOGUE_SEGMENT.StartSignal.C:
                case DL_DIALOGUE_DATA.DIALOGUE_SEGMENT.StartSignal.A:
                    yield return WaitForUserInput();
                    break;
                case DL_DIALOGUE_DATA.DIALOGUE_SEGMENT.StartSignal.WC:
                case DL_DIALOGUE_DATA.DIALOGUE_SEGMENT.StartSignal.WA:
                    isWaitingOnAutoTimer = true;
                    yield return new WaitForSeconds(segment.signalDelay);
                    isWaitingOnAutoTimer = false;
                    break;
            }
        }

        IEnumerator BuildDialogue(string dialogue, bool append = false)
        {
            dialogue = TagManager.Inject(dialogue);

            List<DialogueSegment> segments = DeconstructDialogue(dialogue);

            var animator = architect.tmpro.GetComponent<TextVertexAnimator>();

            if (!append)
            {
                architect.Build("");
                yield return new WaitForEndOfFrame();
            }

            float originalSpeed = architect.speedMultiplier;

            foreach (var segment in segments)
            {
                if (segment.pause > 0)
                {
                    float timer = segment.pause;
                    while (timer > 0)
                    {
                        if (userPromt) { userPromt = false; break; }
                        timer -= Time.deltaTime;
                        yield return null;
                    }
                }

                if (!string.IsNullOrEmpty(segment.text))
                {
                    string textWithEffects = TextEffectParser.ParseCustomTags(segment.text, out var effects);
                    if (animator != null) animator.SetEffects(effects);

                    architect.speedMultiplier = originalSpeed * segment.speed;
                    yield return architect.Append(textWithEffects);

                    while (architect.isBuilding)
                    {
                        if (userPromt)
                        {
                            if (!architect.hurryUp) architect.hurryUp = true;
                            else architect.ForceComplete();
                            userPromt = false;
                        }
                        yield return null;
                    }
                }
            }

            architect.speedMultiplier = originalSpeed;
        }

        IEnumerator WaitForUserInput()
        {
            dialogueSystem.prompt.Show();
            while (!userPromt) yield return null;
            dialogueSystem.prompt.Hide();
            userPromt = false;
        }
    }
}