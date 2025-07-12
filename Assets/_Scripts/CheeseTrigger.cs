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
                    promptUIManager.ShowText("This cheese doubles the damage to pirates with your first bite", true);
                    break;

                case CheesePowerUpType.PoisonLeak:
                    promptUIManager.ShowPrompt(InputKeyType.ButtonWest, "Infect with pee with this button or P", true);
                    break;
            } 
        }      
    }
}

