using UnityEngine;

public class DocManager : MonoBehaviour

{
    private enum State { Idle, LookingFor, Healing }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;
    private GameObject currentTarget;

    // ------------

    [Header("Idle State")]
    [SerializeField] private BoxCollider idleArea;
    [SerializeField] private float idleWalkDelay = 2f;  // tempo iniziale di attesa
    [SerializeField] private float idleWalkInterval = 4f; // tempo tra un movimento e l'altro

    // -----------------

    [Header("Heal State")]

    [SerializeField] private Transform[] healArea;
    [SerializeField] private float healRay = 10.0f;

    // -----------------

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    // --------- STATI INTERNI 
    private State currentState = State.Idle;
    private float idleTimer;              // timer per attendere tra un punto e l'altro
    private Vector3 nextIdlePoint;



    void Awake()
    {
        if (!agent) agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        currentHealth = maxHealth;

    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.LookingFor: UpdateLookingFor(); break;
            case State.Healing: break;

        }

    }

    #region Idle

    private void EnterIdle()
    {
        currentState = State.Idle;
        currentTarget = null;

        agent.isStopped = false;
        animator.SetBool("isWalking", true);
        idleTimer = idleWalkDelay; // aspetta prima di iniziare a muoversi
        nextIdlePoint = transform.position; // resta fermo inizialmente

    }

    private void UpdateIdle()
    {
        if (agent.pathPending) return;

        idleTimer -= Time.deltaTime;

        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            animator.SetBool("isWalking", false);
            if (idleTimer <= 0f)
            {
                nextIdlePoint = GetRandomPointInIdleArea();
                agent.SetDestination(nextIdlePoint);
                animator.SetBool("isWalking", true);
                idleTimer = idleWalkInterval;
            }
        }
    }

    private Vector3 GetRandomPointInIdleArea()
    {
        if (idleArea == null)
            return transform.position;

        Bounds bounds = idleArea.bounds;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out var hit, 1.0f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        return transform.position; // se non trova nulla
    }
    #endregion Idle

    #region LookingFor

    private void EnterLookingFor()
    {
        currentState = State.LookingFor;
        currentTarget = FindBestPirateInHealAreas();

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.transform.position);
            agent.isStopped = false;
            animator.SetBool("isWalking", true);
        }
        else
        {
            EnterIdle(); // nessun bersaglio trovato
        }



    }

    private void UpdateLookingFor()
    {
        if (currentTarget == null)
        {
            EnterIdle();
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance <= 1.5f)
        {
            EnterHealing();
        }
    }


    private GameObject FindBestPirateInHealAreas()
    {
        GameObject best = null;
        float lowestHealth = float.MaxValue;

        foreach (Transform area in healArea)
        {
            Collider[] hits = Physics.OverlapSphere(area.position, healRay);

            foreach (Collider col in hits)
            {
                if (!col.CompareTag("Pirate")) continue;

                PirateController pc = col.GetComponent<PirateController>();
                if (pc == null || !pc.infected) continue;

                if (pc.currentHealth < lowestHealth)
                {
                    lowestHealth = pc.currentHealth;
                    best = pc.gameObject;
                }
            }
        }

        return best;
    }


    #endregion LookingFor

    #region Healing

    private void EnterHealing()
    {
        currentState = State.Healing;
        animator.SetTrigger("Healing");

    }

    #endregion Healing




    // GIZMOS

    private void OnDrawGizmosSelected()
    {
        if (healArea == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform point in healArea)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, healRay);
        }
    }



}
