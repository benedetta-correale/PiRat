using UnityEngine;
using UnityEngine.AI;

public class PirateController : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;    // Inserisci qui i tuoi punti (empty objects nella scena)
    public float waitTimeAtPoint = 2f;  // Quanto si ferma a ogni punto
    public Animator animator;            // Riferimento all'animatore del pirata

    [Header("Camera Settings")]
    [SerializeField] private CameraManager cameraManager;

    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private bool waiting = false;

    void Start()
    {
        animator.SetBool("isWalking", true);
        agent = GetComponent<NavMeshAgent>();

        // Trova il CameraManager se non è assegnato
        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
        }

        // Informa il CameraManager della transform del pirata
        if (cameraManager != null)
        {
            cameraManager.SetPirateTransform(transform);
        }

        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
        else
        {
            Debug.LogWarning("PirateNPCMovement: Nessun punto assegnato!");
        }
    }

    void Update()
    {
        if (waiting || patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartCoroutine(WaitAndGoToNextPoint());
        }
    }

    System.Collections.IEnumerator WaitAndGoToNextPoint()
    {
        waiting = true;
        animator.SetBool("isWalking", false); // Ferma l'animazione


        yield return new WaitForSeconds(waitTimeAtPoint);

        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);

        waiting = false;
        animator.SetBool("isWalking", true); // Riprende l'animazione
    }
}
