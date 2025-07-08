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

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.isStopped = true;
        SetWalking(false);
    }

    void Update()
    {
        if (!hasStartedDialogue && Input.GetKeyDown(KeyCode.D))
        {
            hasStartedDialogue = true;
            agent.isStopped = true;
            SetWalking(false);
            dialogueManager.StartDialogue(fullDialogueSequence);
        }

        if (hasStartedDialogue && !dialogueManager.IsDialogueActive() && !hasWalkedAway)
        {
            WalkAway();
        }

        if (hasWalkedAway && agent.remainingDistance <= agent.stoppingDistance)
        {
            SetWalking(false);
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