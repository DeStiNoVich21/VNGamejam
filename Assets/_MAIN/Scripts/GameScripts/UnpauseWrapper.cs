using UnityEngine;

public class UnpauseWrapper : MonoBehaviour
{
    public void UnpauseConversation()
    {
        DIALOGUE.DialogueSystem.instance.conversationManager.isPausedConversation = false;
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
