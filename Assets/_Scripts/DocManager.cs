using UnityEngine;

public class DocManager : MonoBehaviour

{
    private enum State { Idle, LookingFor, Healing }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private UnityEngine.AI.NavMeshAgent agent;
    private GameObject currentTarget;
    private Animator pirateAnim;

    // ------------

    [Header("Idle State")]
    [SerializeField] private BoxCollider idleArea;
    [SerializeField] private float idleWalkDelay = 2f;  // tempo iniziale di attesa
    [SerializeField] private float idleWalkInterval = 4f; // tempo tra un movimento e l'altro

    // -----------------

    [Header("Heal State")]

    [SerializeField] private Transform[] healArea;
    [SerializeField] private float healRay = 10.0f;
    [SerializeField] private int recoveryPoints = 40;
    private bool hasHealed = false; // flag per evitare più cure

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
            case State.Healing: UpdateHealing(); break;

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
        
        WanderInIdleArea();

        GameObject best = FindBestPirateInHealAreas();
        if (best != null)
        {
            Debug.Log("Pirata trovato");
            currentTarget = best;
            
            EnterLookingFor();
        }
        
    }


    private void WanderInIdleArea()
    {
        
        // Se sta già andando da qualche parte, aspetta che arrivi
        if (agent.pathPending || agent.remainingDistance > 0.5f)
            return;

        // Conta il tempo d’attesa
        idleTimer -= Time.deltaTime;

        // Se ha aspettato abbastanza, scegli un nuovo punto e vai
        if (idleTimer <= 0f)
        {
            Vector3 nextPoint = GetRandomPointInIdleArea();
            agent.SetDestination(nextPoint);
            agent.isStopped = false;
            animator.SetBool("isWalking", true);
            

            idleTimer = idleWalkInterval; // reset del timer
        } else {
              animator.SetBool("isWalking", false);
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

        if (currentTarget != null)
        {
            agent.SetDestination(currentTarget.transform.position);
            agent.isStopped = false;
        }
        else
        {
            EnterIdle();
            Debug.Log("PIRATA PERSO");
        }
    }

    private void UpdateLookingFor()
    {
        if (currentTarget == null)
        {
            EnterIdle();
            return;
        }

        // Segui dinamicamente la posizione attuale del pirata
        agent.SetDestination(currentTarget.transform.position);

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= 1.5f)
        {
            EnterHealing();
            
        }
    }



    #endregion LookingFor

    #region Healing


    private void EnterHealing()
    {
        currentState = State.Healing;

        // Ferma il medico
        agent.isStopped = true;
        
        animator.SetTrigger("Heal");

        hasHealed = false;
    }

    private void UpdateHealing()
    {
        if (currentTarget != null)
        {
            PirateController pc = currentTarget.GetComponent<PirateController>();
            if (pc != null)
            {
                pc.EnterBeingHealed(transform.position, 2f);
                pc.Heal(recoveryPoints);
                Debug.Log("PirataCurato");
            }

            currentTarget = null;

        }

        // Cura fatta → reset trigger, imposta camminata
        
    

        GameObject nextTarget = FindBestPirateInHealAreas();
        if (nextTarget != null)
        {
            currentTarget = nextTarget;
            EnterLookingFor();
        }
        else
        {
            EnterIdle();
        }
    }

    #endregion Healing

    // FIND PIRATE
    
    private GameObject FindBestPirateInHealAreas()
    {
        GameObject best = null;

        float lowestHealth = float.MaxValue;

        foreach (Transform area in healArea)
        {
            Collider[] hits = Physics.OverlapSphere(area.position, healRay);

            foreach (Collider col in hits)
            {
                Debug.Log("sto cercando pirati");
                if (!col.CompareTag("Pirate")) continue;

                PirateController pc = col.GetComponent<PirateController>();
                if (pc == null || !pc.infected || pc.alreadyHealing || pc.currentHealth > pc.maxHealth * 0.5f) continue;

                if (pc.currentHealth < lowestHealth)
                {
                    lowestHealth = pc.currentHealth;
                    best = pc.gameObject;
                }
            }
        }

        return best;
    }


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
