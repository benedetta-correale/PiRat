using UnityEngine;

public class PirateAttackTrigger : MonoBehaviour
{
    [SerializeField] private PirateFinalMove pirateFinalMove;
    [SerializeField] private Animator pirate;


    
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (this.hasTriggered) return;

        if (other.CompareTag("Player") && RatInteractionManager.HasCompletedFirstQuickTime)
        {
            pirate.SetTrigger("AttackTrigger");
            pirateFinalMove?.MoveToFinalTarget();
            hasTriggered = true;
        }
    }
}
