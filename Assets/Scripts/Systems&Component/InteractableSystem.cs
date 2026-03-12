using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public abstract class BaseCondition : IDialogueCondition
{
    public bool isRepeateable;
    public bool executed;

    public abstract bool Check();
}
[System.Serializable]
public class DaysCondition : BaseCondition
{
    public int ExecutionDay;
    public override bool Check()
    {
        VariableStore.TryGetValue("currDay",out var currDay);

        return ExecutionDay == (int)currDay;
    }
}
public interface IDialogueCondition
{
    public bool Check();
}

[Serializable]
public struct DialogueEntry
{
    public TextAsset textfile;

    [SerializeReference]
    public IDialogueCondition[] condition;
    public bool ConditionsTrue { get {
            foreach (IDialogueCondition condition in condition)
            {
                if(!condition.Check())
                    return false;
            }
            return true; 
        } }    
}

public class InteractableSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public DialogueEntry[] entries;
    public Tween Tween;
    private void Start()
    {
        Tween = transform.DOScale(transform.localScale * 1.05f, 0.2f).SetAutoKill(false)
            .Pause();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        foreach (var entry in entries)
        {
            if (entry.ConditionsTrue)
            {
                DIALOGUE.DialogueSystem.instance.Say(FileManager.ReadTextAsset(entry.textfile, true));
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Tween.PlayForward();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Tween.PlayBackwards();
    }
}

