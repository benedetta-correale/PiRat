using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System;

public class PirateController : MonoBehaviour
{
    private enum State { Patrol, Suspicious, Chasing, Attacking }

    public event Action<PirateController> OnPirateDeath;
    public bool isPossessed { get; set; }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform ratTransform;

    [SerializeField] private BonusMalus ratHealt;

    [Header("Sight")]
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float viewOriginBackOffset = 0.5f;

    [Header("Alert UI")]
    [SerializeField] private GameObject alertIndicator;
    [SerializeField] private Image alertFillImage;

    [Header("Alert Timings")]
    [SerializeField] private float attachTime = 5f;
    [SerializeField, Range(0f,1f)] private float moveThreshold = 0.7f;
    [SerializeField] private float baseFillSpeed = 1f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3.0f;

    [Header("Attacking")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 2.0f;
    [SerializeField] private int attackDamage = 10;

    private float lastAttackTime;
    private bool canAttack = true;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Image healthFill;
    public bool infected = false;

    private State state = State.Patrol;
    private int patrolIdx;
    private float suspicionTimer;
    private Vector3 suspicionTarget;
    private bool hasStartedInvestigating;

    private float currentHealth;

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

    private void PatrolUpdate()
    {
        if (CanSeeRat())
        {
            EnterSuspicious();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.3f && patrolPoints.Length > 0)
        {
            patrolIdx = (patrolIdx + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIdx].position);
        }
    }

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

        float distance = Vector3.Distance(GetEyeOrigin(), ratTransform.position);
        float proximity = 1f + ((viewDistance - distance) / viewDistance);
        float delta = Time.deltaTime * baseFillSpeed * (seesRat ? 1f : -1f);

        suspicionTimer = Mathf.Clamp(suspicionTimer + delta * (seesRat ? proximity : 1f), 0f, attachTime);
        alertFillImage.fillAmount = suspicionTimer / attachTime;

        if (!hasStartedInvestigating && suspicionTimer >= attachTime * moveThreshold)
        {
            hasStartedInvestigating = true;
            agent.isStopped = false;
            agent.SetDestination(suspicionTarget);
            animator.SetBool("isWalking", true);
        }

        if (seesRat)
        {
            suspicionTarget = ratTransform.position;
            if (hasStartedInvestigating)
                agent.SetDestination(suspicionTarget);
        }

        if (suspicionTimer >= attachTime)
        {
            EnterChasing();
        }
        else if (suspicionTimer <= 0f && !seesRat)
        {
            EnterPatrol();
        }
    }

    private void EnterChasing()
    {
        state = State.Chasing;
        alertIndicator.SetActive(false);
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        animator.SetBool("isWalking", true);
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
            EnterSuspicious();
        }
    }

    private void EnterAttacking()
    {
        state = State.Attacking;
        agent.isStopped = true;
        animator.SetBool("isWalking", false);
        animator.SetTrigger("AttackTrigger");
        ratHealt.TakeDamage(attackDamage);
    }

    private void UpdateAttacking()
    {
        if (ratTransform == null) return;

        float distance = Vector3.Distance(transform.position, ratTransform.position);

        if (distance > attackRange)
        {
            EnterChasing();
            return;
        }

        Vector3 dir = (ratTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

        if (canAttack)
        {
            animator.SetTrigger("AttackTrigger");
            
            canAttack = false;
        }
    }

    public void OnAttackAnimationEnd()
    {
        lastAttackTime = Time.time;
        canAttack = true;
        EnterChasing(); // forza il ritorno alla camminata
    }

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

    private Vector3 GetEyeOrigin()
    {
        return transform.position - transform.forward * viewOriginBackOffset + Vector3.up * eyeHeight;
    }

    private bool CanSeeRat()
    {
        Vector3 origin = GetEyeOrigin();
        Vector3 dir = (ratTransform.position + Vector3.up * 0.4f) - origin;
        float dist = dir.magnitude;

        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle * 0.5f) return false;

        return !Physics.Raycast(origin, dir.normalized, dist, LayerMask.GetMask("Default")) 
            || (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist) && hit.transform.root == ratTransform.root);
    }

    public void TakeDamage(int dmg)
    {
        if (ratTransform != null)
        {
            Vector3 dir = (ratTransform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        }

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

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? GetEyeOrigin() : transform.position + Vector3.up * eyeHeight;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(origin, 0.1f);

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(origin, viewDistance);

        Gizmos.color = Color.yellow;
        int rays = 30;
        float halfAngle = viewAngle * 0.5f;
        for (int i = 0; i <= rays; i++)
        {
            float angle = -halfAngle + (viewAngle / rays) * i;
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 dir = rot * transform.forward;
            Gizmos.DrawRay(origin, dir * viewDistance);
        }

        if (Application.isPlaying && ratTransform != null)
        {
            Vector3 target = ratTransform.position + Vector3.up * 0.4f;
            Vector3 dirToRat = target - origin;
            float dist = dirToRat.magnitude;

            if (Physics.Raycast(origin, dirToRat.normalized, out RaycastHit hit, dist, LayerMask.GetMask("Default")))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(origin, dirToRat.normalized * hit.distance);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(origin, dirToRat.normalized * Mathf.Min(dist, viewDistance));
            }

            Gizmos.color = Color.green;
            Collider ratCollider = ratTransform.GetComponent<Collider>();
            if (ratCollider != null)
            {
                Gizmos.matrix = ratCollider.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(ratCollider.bounds.center - ratTransform.position, ratCollider.bounds.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
