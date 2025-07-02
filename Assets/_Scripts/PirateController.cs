using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System;

public class PirateController : MonoBehaviour
{
    // ──────────────────────── ENUM & EVENTS ────────────────────────
    private enum State { Patrol, Suspicious, Chasing, Attacking }

    public event Action<PirateController> OnPirateDeath;
    public bool isPossessed { get; set; }

    // ──────────────────────── INSPECTOR ────────────────────────────
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform ratTransform;

    [Header("Sight")]
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float viewOriginBackOffset = 0.5f;
    

    [Header("Alert UI")]
    [SerializeField] private GameObject alertIndicator;     // contorno + fill
    [SerializeField] private Image alertFillImage;          // solo fill

    [Header("Alert Timings")]
    [SerializeField] private float attachTime = 5f;         // tempo per 100 %
    [SerializeField, Range(0f,1f)] private float moveThreshold = 0.7f; // 70 %
    [SerializeField] private float baseFillSpeed = 1f;      // 1 = tempo lineare

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3.0f;

    [Header("Attacking")]

    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 2.0f;

    private float lastAttackTime;
    

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Image healthFill;
    // ----------------------------------------------------------------

    // Stato interno
    private State state = State.Patrol;
    private int patrolIdx;
    private float suspicionTimer;                 // valore 0‒attachTime
    private Vector3 suspicionTarget;              // dove andare a controllare
    private bool hasStartedInvestigating;

    private float currentHealth;

    // ──────────────────────── UNITY ────────────────────────────────
    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        healthFill.fillAmount = 1f;
        ResetAlert();
    }

    private void Start()
    {
        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
            animator.SetBool("isWalking", true);
        }
    }

    private void Update()
    {
        switch (state)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Suspicious: SuspiciousUpdate(); break;
            case State.Chasing: ChasingUpdate(); break;
            case State.Attacking: UpdateAttacking(); break;
        }
    }

    // ──────────────────────── STATES ───────────────────────────────
    #region Patrol
    private void PatrolUpdate()
    {
        // transizione a suspicious se vede il topo
        if (CanSeeRat())
        {
            EnterSuspicious();
            return;
        }

        // normale camminata di pattuglia
        if (!agent.pathPending && agent.remainingDistance < 0.3f && patrolPoints.Length > 0)
        {
            patrolIdx = (patrolIdx + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIdx].position);
        }
    }
    #endregion

    #region Suspicious
    private void EnterSuspicious()
    {
        state = State.Suspicious;
        suspicionTimer = 0f;
        suspicionTarget = ratTransform.position;
        hasStartedInvestigating = false;

        agent.isStopped = true;
        animator.SetBool("isWalking", false);

        alertIndicator.SetActive(true);
        alertFillImage.fillAmount = 0f;
    }

    private void SuspiciousUpdate()
    {
        bool seesRat = CanSeeRat();

        // 1. aggiorna timer (↑ se vede, ↓ se non vede)
        float distance = Vector3.Distance(GetEyeOrigin(), ratTransform.position);
        float proximity = 1f + ((viewDistance - distance) / viewDistance); // 1→2
        float delta = Time.deltaTime * baseFillSpeed * (seesRat ? 1f : -1f);

        suspicionTimer = Mathf.Clamp(suspicionTimer + delta * (seesRat ? proximity : 1f), 0f, attachTime);
        alertFillImage.fillAmount = suspicionTimer / attachTime;

        // 2. raggiunto il 70 % → inizia a muoversi verso ultimo avvistamento 
        if (!hasStartedInvestigating && suspicionTimer >= attachTime * moveThreshold)
        {
            hasStartedInvestigating = true;
            agent.isStopped = false;
            agent.SetDestination(suspicionTarget);
            animator.SetBool("isWalking", true);
        }

        // 3. aggiorna destinazione se continua a vedere il topo
        if (seesRat)
        {
            suspicionTarget = ratTransform.position;
            if (hasStartedInvestigating)
                agent.SetDestination(suspicionTarget);
        }

        // 4. transizioni
        if (suspicionTimer >= attachTime)
        {
            EnterChasing();
        }
        else if (suspicionTimer <= 0f && !seesRat)
        {
            EnterPatrol();
        }
    }
    #endregion

    #region Chasing
    private void EnterChasing()
    {
        state = State.Chasing;
        alertIndicator.SetActive(false);

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        animator.SetBool("isWalking", true); // ← AGGIUNGI QUESTO
        
        
    }

    private void ChasingUpdate()
    {

         float distance = Vector3.Distance(transform.position, ratTransform.position);

        if (CanSeeRat())
        {
            agent.isStopped = false;
            agent.SetDestination(ratTransform.position);

            if (distance <= attackRange)
            {
                EnterAttacking();
            }

        }

        else
        {
            EnterSuspicious(); // torna a suspicious per scalare il timer
        }
    }
    #endregion

    #region Attacking
    private void EnterAttacking()

    {
        state = State.Attacking;
        agent.isStopped = true;

        animator.SetBool("isWalking", false); 
        animator.SetTrigger("AttackTrigger");


    }

    private void UpdateAttacking()
    {
       

        if (ratTransform == null) return;

        float distance = Vector3.Distance(transform.position, ratTransform.position);

        // Se il topo è scappato, smetti di attaccare
        if (distance > attackRange)
        {
            EnterChasing();
            return;
        }

        // Gira verso il topo
        Vector3 dir = (ratTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        

        // Se è passato abbastanza tempo, attacca di nuovo
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            animator.SetTrigger("AttackTrigger");
            lastAttackTime = Time.time;
        }
    }
    


    #endregion

    private void EnterPatrol()
    {
        state = State.Patrol;
        ResetAlert();

        if (patrolPoints.Length > 0)
        {
            agent.isStopped = false;
            agent.SetDestination(patrolPoints[patrolIdx].position);
            animator.SetBool("isWalking", true);
        }
    }

    private void ResetAlert()
    {
        alertIndicator.SetActive(false);
        alertFillImage.fillAmount = 0f;
        suspicionTimer = 0f;
        hasStartedInvestigating = false;
    }
    
   
    // ──────────────────────── VISION ───────────────────────────────
    private Vector3 GetEyeOrigin()
    {
        return transform.position
             - transform.forward * viewOriginBackOffset
             + Vector3.up * eyeHeight;
    }

    private bool CanSeeRat()
    {
        Vector3 origin = GetEyeOrigin();
        Vector3 dir = (ratTransform.position + Vector3.up * 0.4f) - origin;
        float dist = dir.magnitude;

        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

        return !Physics.Raycast(origin, dir.normalized, dist, LayerMask.GetMask("Default")) 
               || Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist) 
               && hit.transform.root == ratTransform.root;
    }

    // ──────────────────────── HEALTH ───────────────────────────────
    public void TakeDamage(int dmg)
    {
        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        healthFill.fillAmount = currentHealth / maxHealth;
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        OnPirateDeath?.Invoke(this);
        agent.isStopped = true;
        animator.SetTrigger("Die");
    }

    // ───────────────────────── GIZMO ───────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? GetEyeOrigin()
                                               : transform.position + Vector3.up * eyeHeight;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, viewDistance);

        int steps = 12;
        float half = viewAngle * 0.5f;
        for (int i = 0; i <= steps; i++)
        {
            float angle = -half + (viewAngle / steps) * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.DrawRay(origin, dir * viewDistance);
        }
    }
}