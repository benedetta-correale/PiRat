using UnityEngine;

public class CheeseTrigger : MonoBehaviour
{
    public PromptUIManager promptUIManager;
    private bool hasTriggered = false;
    public enum CheesePowerUpType { Heal, SpeedBoost, DamageBoost, PoisonLeak }
    public CheesePowerUpType powerUpType;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            switch (powerUpType)
            {
                case CheesePowerUpType.Heal:
                    promptUIManager.ShowPrompt(InputKeyType.RightTrigger, "Bite this cheese to heal with right trigger or mouse click", true);
                    break;

                case CheesePowerUpType.SpeedBoost:
                    promptUIManager.ShowPrompt(InputKeyType.RightTrigger, "Bite this cheese to gain a speed boost with right trigger or mouse click", true);
                    break;
                
                case CheesePowerUpType.DamageBoost:
                    promptUIManager.ShowPrompt(InputKeyType.RightTrigger, "Bite this cheese to double the damage to the pirates with right trigger or mouse click", true);
                    break;

                case CheesePowerUpType.PoisonLeak:
                    promptUIManager.ShowPrompt(InputKeyType.RightTrigger, "Bite this cheese to infect with pee with right trigger or mouse click", true);
                    break;
            } 
        }      
    }
}

