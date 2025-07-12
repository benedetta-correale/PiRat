using UnityEngine;

public class TrapTrigger : MonoBehaviour
{
    public PromptUIManager promptUIManager;
    private bool hasTriggered = false;
    public TrapTypeTutorial trapType;
    public enum TrapTypeTutorial { Spring, Glue, Slide }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            switch (trapType)
            {
                case TrapTypeTutorial.Spring:
                    promptUIManager.ShowText("Attention! This trap deals damage to you", true);
                    break;

                case TrapTypeTutorial.Glue:
                    promptUIManager.ShowPrompt(InputKeyType.LeftStick, "Attention! This trap holds you, move to break here", true);
                    break;

                case TrapTypeTutorial.Slide:
                    promptUIManager.ShowText("Attention! This trap makes you slip", true);
                    break;
            }
        }
    }
}
