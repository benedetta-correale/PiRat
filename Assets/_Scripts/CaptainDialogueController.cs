using UnityEngine;
using UnityEngine.AI;

public class CaptainDialogueController : MonoBehaviour
{
    [Header("Dipendenze")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence fullDialogueSequence; // combinata
    [SerializeField] private Transform exitPoint;

    private NavMeshAgent agent;
    private Animator animator;

    private bool hasStartedDialogue = false;
    private bool hasWalkedAway = false;
    [SerializeField] private PrisonerDialogueTrigger prisonerTrigger;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.isStopped = true;
        SetWalking(false);

        // Collegamento alla fine del dialogo
        dialogueManager.OnDialogueEnded += HandleDialogueEnded;
    }

    void OnDestroy()
    {
        dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
    }

    void HandleDialogueEnded()
    {
        if (!hasWalkedAway)
        {
            WalkAway();
        }
    }


    void WalkAway()
    {
        hasWalkedAway = true;
        agent.isStopped = false;
        SetWalking(true);
        if (exitPoint != null)
            agent.SetDestination(exitPoint.position);
    }

    void SetWalking(bool isWalking)
    {
        if (animator != null)
            animator.SetBool("isWalking", isWalking);
    }
}