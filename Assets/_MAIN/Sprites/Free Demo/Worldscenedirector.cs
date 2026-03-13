using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// World Scene Director - manages a single world scene.
/// Holds references to scene objects, registers them in WorldObjectManager
/// and executes commands from a txt file.
/// </summary>
public class WorldSceneDirector : MonoBehaviour
{
    [Title("Scene")]
    [SerializeField] private TextAsset sceneFile;

    [Title("Actors")]
    [SerializeField] private List<WorldActor> actors = new List<WorldActor>();

    [Title("Trigger")]
    [SerializeField] private bool useTrigger = false;
    [SerializeField, ShowIf("useTrigger")] private string triggerTag = "Player";
    [SerializeField, ShowIf("useTrigger")] private bool oneShot = true;

    private bool _triggered = false;
    private WorldObjectManager manager => WorldObjectManager.instance;

    // --- 2D Trigger ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTrigger) return;
        if (oneShot && _triggered) return;
        if (!other.CompareTag(triggerTag)) return;

        _triggered = true;
        Activate();
    }

    // --- Public entry point ---

    public void Activate()
    {
        if (sceneFile == null)
        {
            Debug.LogWarning($"[WorldSceneDirector] '{name}': sceneFile is not assigned");
            return;
        }
        StartCoroutine(RunScene());
    }

    // --- Lifecycle ---

    private IEnumerator RunScene()
    {
        RegisterActors();
        yield return ProcessFile();
        UnregisterActors();
    }

    private void RegisterActors()
    {
        foreach (var actor in actors)
        {
            if (actor.go != null)
                manager.Register(actor.id, actor.go);
        }
    }

    private void UnregisterActors()
    {
        foreach (var actor in actors)
            manager.Unregister(actor.id);
    }

    // --- File processing ---

    private IEnumerator ProcessFile()
    {
        string[] lines = sceneFile.text.Split('\n');

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                continue;

            yield return ProcessLine(line);
        }
    }

    private IEnumerator ProcessLine(string line)
    {
        if (line.StartsWith("wait("))
        {
            float duration = ParseSingleFloat(line);
            yield return new WaitForSeconds(duration);
            yield break;
        }

        if (line.Contains(".") && line.Contains("("))
        {
            yield return ProcessObjectCommand(line);
            yield break;
        }

        Debug.Log($"[WorldSceneDirector] Unknown line: '{line}'");
    }

    // --- Object command parsing ---
    // Format: ObjectId.CommandName(arg0 arg1 arg2)

    private IEnumerator ProcessObjectCommand(string line)
    {
        int dotIndex = line.IndexOf('.');
        int openParen = line.IndexOf('(');
        int closeParen = line.LastIndexOf(')');

        if (dotIndex < 0 || openParen < 0 || closeParen < 0)
            yield break;

        string id = line.Substring(0, dotIndex).Trim();
        string command = line.Substring(dotIndex + 1, openParen - dotIndex - 1).Trim().ToLower();
        string argsRaw = line.Substring(openParen + 1, closeParen - openParen - 1).Trim();
        string[] args = argsRaw.Length > 0
            ? argsRaw.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
            : new string[0];

        switch (command)
        {
            case "setactive":
                if (args.Length > 0 && bool.TryParse(args[0], out bool active))
                    manager.SetActive(id, active);
                break;

            case "playanim":
                if (args.Length > 0)
                    manager.PlayAnim(id, args[0]);
                break;

            case "setanimatorbool":
                if (args.Length > 1 && bool.TryParse(args[1], out bool boolVal))
                    manager.SetAnimatorBool(id, args[0], boolVal);
                break;

            case "setposition":
                if (TryParseVector3(args, out Vector3 pos))
                    manager.SetPosition(id, pos);
                break;

            case "moveto":
                yield return ProcessMoveTo(id, args);
                break;

            case "flip":
                manager.Flip(id);
                break;

            default:
                Debug.LogWarning($"[WorldSceneDirector] Unknown command '{command}' for '{id}'");
                break;
        }
    }

    // --- MoveTo with speed ---

    private IEnumerator ProcessMoveTo(string id, string[] args)
    {
        float speed = 2f;
        List<string> positional = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-spd" && i + 1 < args.Length)
            {
                float.TryParse(args[i + 1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out speed);
                i++;
            }
            else
            {
                positional.Add(args[i]);
            }
        }

        if (!TryParseVector3(positional.ToArray(), out Vector3 target))
            yield break;

        Coroutine move = manager.MoveTo(id, target, speed);
        if (move != null)
            yield return move;
    }

    // --- Helpers ---

    private float ParseSingleFloat(string line)
    {
        int open = line.IndexOf('(');
        int close = line.IndexOf(')');
        if (open < 0 || close < 0) return 0f;

        string inner = line.Substring(open + 1, close - open - 1);
        float.TryParse(inner, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float result);
        return result;
    }

    private bool TryParseVector3(string[] args, out Vector3 result)
    {
        result = Vector3.zero;
        if (args.Length < 2) return false;

        float x = 0, y = 0, z = 0;

        bool ok = float.TryParse(args[0], System.Globalization.NumberStyles.Float,
                      System.Globalization.CultureInfo.InvariantCulture, out x)
               && float.TryParse(args[1], System.Globalization.NumberStyles.Float,
                      System.Globalization.CultureInfo.InvariantCulture, out y);

        if (!ok) return false;

        if (args.Length > 2)
            float.TryParse(args[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out z);

        result = new Vector3(x, y, z);
        return true;
    }

    // --- Editor buttons ---

    [Button("Play Scene")]
    private void ActivateFromEditor() => Activate();

    [Button("Reset Trigger"), ShowIf("oneShot")]
    private void ResetTrigger() => _triggered = false;
}

// --- Actor struct ---

[System.Serializable]
public class WorldActor
{
    [HorizontalGroup(150), LabelText("ID")]
    public string id;

    [HorizontalGroup, HideLabel]
    public GameObject go;
}