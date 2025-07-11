using UnityEngine;
using UnityEngine.AI;

public class EmpowermentTrigger : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueSequence empowermentDialogue;

    [SerializeField] private Transform pirate;               // Il GameObject del pirata
    [SerializeField] private Transform targetPoint;          // Il punto da raggiungere
    [SerializeField] private float arrivalThreshold = 0.5f;  // Precisione del check

    private bool hasTriggered = false;
    private NavMeshAgent agent;

    void Start()
    {
        if (pirate != null)
            agent = pirate.GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (hasTriggered || pirate == null || targetPoint == null || agent == null) return;

        float distance = Vector3.Distance(pirate.position, targetPoint.position);

        if (!agent.pathPending && distance <= arrivalThreshold)
        {
            hasTriggered = true;
            dialogueManager.StartDialogue(empowermentDialogue);
        }
    }
}
