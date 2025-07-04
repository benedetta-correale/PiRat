using UnityEngine;

public class DocManager : MonoBehaviour

{
    private enum State { Idle, Approccing, Healing }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;
    private GameObject currentTarget;

    [Header("Idle State")]
    [SerializeField] private BoxCollider idleArea;
    [SerializeField] private float idleWalkDelay = 2f;  // tempo iniziale di attesa
    [SerializeField] private float idleWalkInterval = 4f; // tempo tra un movimento e l'altro
    


    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    //STATI INTERNI 
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
            case State.Approccing: break;
            case State.Healing: break;

        }

    }

    #region Idle

    private void EnterIdle()
    {
        currentState = State.Idle;
        currentTarget = null;

        agent.isStopped = false;
        idleTimer = idleWalkDelay; // aspetta prima di iniziare a muoversi
        nextIdlePoint = transform.position; // resta fermo inizialmente
    }

    private void UpdateIdle()
    {
        if (agent.pathPending) return;

        idleTimer -= Time.deltaTime;

        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            if (idleTimer <= 0f)
            {
                nextIdlePoint = GetRandomPointInIdleArea();
                agent.SetDestination(nextIdlePoint);
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


}
