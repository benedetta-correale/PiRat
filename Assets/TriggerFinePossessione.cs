using UnityEngine;

public class TriggerFinePossessione : MonoBehaviour
{
    private bool hasTriggered = true;
    public DialogueManager dialogueManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered) return;
        if (other.CompareTag("PirateTutorial"))
        {
            hasTriggered = false;
            dialogueManager.PromptUIExitPossession();
        }
    }
}
