using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;

public class PirateController : MonoBehaviour
{
    private enum State { Patrol, Suspicious, Chasing, Attacking, BeingHealed, Dead}

    public string CurrentState => state.ToString();


    public event Action<PirateController> OnPirateDeath;
    public bool isPossessed { get; set; }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform ratTransform;
    [SerializeField] private RatInteractionManager ratManager;
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
    [SerializeField] private int biteTickDamage;
    [SerializeField] private float biteTickInterval;
    [SerializeField] private float biteDuration;
     public bool infected = false;

    [Header("HEAL STATUS")]

    [SerializeField] private float healingCooldown = 5f; // tempo in secondi dopo la guarigione
    private float lastHealedTime = -Mathf.Infinity;
    public bool alreadyHealing = false;
    public float healingEndTime = 3.0f; // usata per fermare il movimento di camminata del pirata per un certo tempo 


    // STATI INTERNI 
    private Coroutine infectionCoroutine;
    private State state = State.Patrol;
    private int patrolIdx;
    private float suspicionTimer;
    private Vector3 suspicionTarget;
    private bool hasStartedInvestigating;
    private bool hasDealtDamageThisAttack = false;

    private bool ratWasRecentlyInvincible = false;
    private float retryAttackTime = 0f;

    public float currentHealth;
   


    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        healthFill.fillAmount = 1f;
        ResetAlert();
        ratManager = ratTransform.GetComponent<RatInteractionManager>();

        ratHealt = ratTransform.GetComponent<BonusMalus>();

        if (ratHealt == null)
            Debug.LogError($"{name} → ratHealt è NULL");
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

        if (state == State.Dead) return;

        
        switch (state)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Suspicious: SuspiciousUpdate(); break;
            case State.Chasing: ChasingUpdate(); break;
            case State.Attacking: UpdateAttacking(); break;
            case State.BeingHealed: UpdateBeingHealed(); break;
        }
    }

    #region Patrol

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

    private void PatrolUpdate()
    {
        if (CanSeeRat())
        {
            EnterSuspicious();
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name} NON è su una NavMesh! (posizione: {transform.position})");
            return;
        }

        

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

    #endregion
    

    #region Chasing
    private void EnterChasing()
    {
        state = State.Chasing;
        alertIndicator.SetActive(false);
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        animator.SetBool("isWalking", true);
        SendMessage("CancelAttractionFromPuddle", this, SendMessageOptions.DontRequireReceiver);
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

    #endregion

    #region Attack

    private void EnterAttacking()
    {
        if (Time.time < retryAttackTime)
        {
            //Debug.Log("⏳ Attesa prima di riprovare l’attacco");
            return;
        }

        if (ratManager != null && ratManager.invincible)
        {
            //Debug.Log("🚫 Ratto invincibile → stop attacco per 10s");
            ratWasRecentlyInvincible = true;
            retryAttackTime = Time.time + 10f; // Blocca per 10s, anche se invincibilità finisce prima
            EnterChasing(); // torna a inseguire
            return;
        }

        //Debug.Log("⚔️ STATO DI ATTACCO (ratto vulnerabile)");
        state = State.Attacking;
    }



    private void UpdateAttacking()
    {
        if (ratTransform == null) return;

        if (ratWasRecentlyInvincible && !ratManager.invincible)
        {
            ratWasRecentlyInvincible = false;
            Debug.Log("✅ Il ratto non è più invincibile");
        }


        bool isAttackingState = animator.GetCurrentAnimatorStateInfo(0).tagHash == Animator.StringToHash("Attack");

        // Dopo aver attaccato → resta fermo per il cooldown
        if (Time.time < lastAttackTime + attackCooldown)
        {
            agent.isStopped = true;
            animator.SetBool("isWalking", false);
            return; // NON fare nulla per il tempo del cooldown
        }

        // Se il cooldown è finito
        // Valuta se tornare a inseguire o tornare in Patrol
        if (!CanSeeRat())
        {
            EnterPatrol();
            return;
        }

        float distance = Vector3.Distance(transform.position, ratTransform.position);
        if (distance > attackRange)
        {
            EnterChasing();
            return;
        }

        // Ruota verso il topo
        Vector3 dir = (ratTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

        // Stato attuale
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.tagHash == Animator.StringToHash("Attack"))
        {
            Debug.Log($"Animazione ATTACK in corso. NormalizedTime = {stateInfo.normalizedTime}");
            if (!hasDealtDamageThisAttack)
            {
                if (ratHealt != null && distance <= attackRange)
                {
                    ratHealt.TakeDamage(attackDamage);
                    Debug.Log("ratto danneggiato");
                }

                hasDealtDamageThisAttack = true;
            }


            if (stateInfo.normalizedTime >= 1f)
            {
                hasDealtDamageThisAttack = false;
                // Niente EnterChasing qui! Lo decidi sopra solo dopo cooldown
            }
        }
        else
        {
            if (ratManager != null && ratManager.invincible)
            {
                Debug.Log("❌ ATTACCO NON PARTITO: ratto invincibile");
                return;
            }

            Debug.Log("🎯 ATTEMPT ATTACK: Trigger attacco chiamato");
            animator.SetTrigger("AttackTrigger");
            lastAttackTime = Time.time;
            hasDealtDamageThisAttack = false;
        }


    }



    public void OnAttackAnimationEnd()
    {
        lastAttackTime = Time.time;
        canAttack = true;
        EnterChasing(); // forza il ritorno alla camminata
    }

    #endregion

    #region BeingHealed

    public void EnterBeingHealed(Vector3 medicPosition, float duration)
    {
        state = State.BeingHealed;

        agent.isStopped = true;
        animator.SetBool("isWalking", false);

        // Ruota verso il medico
        Vector3 dir = (medicPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        transform.rotation = lookRotation;

        healingEndTime = Time.time + duration;
    }

    private void
    UpdateBeingHealed()
    {
        if (Time.time >= healingEndTime)
        {
            animator.SetBool("isWalking", true);
            agent.isStopped = false;
            EnterPatrol(); // oppure EnterChasing(), in base al contesto
        }
    }

    #endregion
    





    private void ResetAlert()
    {
        alertIndicator.SetActive(false);
        alertFillImage.fillAmount = 0f;
        suspicionTimer = 0f;
        hasStartedInvestigating = false;
    }

    // ------ VISION

    private Vector3 GetEyeOrigin()
    {
        return transform.position - transform.forward * viewOriginBackOffset + Vector3.up * eyeHeight;
    }

    private bool CanSeeRat()
    {
        Vector3 origin = GetEyeOrigin();
        Vector3 directionToRat = (ratTransform.position - origin).normalized;
        float distance = Vector3.Distance(origin, ratTransform.position);

        // ✅ Controlla se il topo è davanti
        float angle = Vector3.Angle(transform.forward, directionToRat);
        if (angle > viewAngle * 0.5f) return false;

        // ✅ Raycast per occlusione
        for (float yOffset = 0f; yOffset <= 1f; yOffset += 0.25f)
        {
            Vector3 target = ratTransform.position + Vector3.up * yOffset;
            Vector3 dir = target - origin;

            if (!Physics.Raycast(origin, dir.normalized, out RaycastHit hit, distance, LayerMask.GetMask("Wall")))
            {
                Debug.DrawRay(origin, dir.normalized * distance, Color.green, 1.5f);
                return true;
            }
            else
            {
                Debug.DrawRay(origin, dir.normalized * distance, Color.red, 1.5f);
            }
        }

        return false;
    }





    // ---- DAMAGE 

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

        // Lancia l'infezione al primo danno, se non è già partita
        if (!infected)
        {
            Debug.Log("infettato");
            infected = true;

            if (infectionCoroutine != null)
                StopCoroutine(infectionCoroutine);

            infectionCoroutine = StartCoroutine(
                InfectionDamageRoutine(biteTickDamage, biteTickInterval, biteDuration)
            );
        }

    }

    private IEnumerator InfectionDamageRoutine(int biteTickDamage, float biteTickInterval, float biteDuration)
    {
        Debug.Log("Courutine cominciata");
        float elapsed = 0f;

        while (elapsed < biteDuration)
        {
            yield return new WaitForSeconds(biteTickInterval);

            TakeDamage((int)biteTickDamage);
            Debug.Log($"[Infezione] Vita attuale del pirata: {currentHealth}");

            elapsed += biteTickInterval;

            if (currentHealth <= 0f) Die();
        }
    }

    private void Die()
    {
        if (state == State.Dead) return; // prevenzione doppia morte

        infected = false;
        OnPirateDeath?.Invoke(this);
        agent.isStopped = true;
        animator.SetTrigger("Die");
        state = State.Dead;
    }

    //GUARIGIONE

    public void Heal(int recoveryPoints)
    {
        currentHealth = Mathf.Min(currentHealth + recoveryPoints, maxHealth);
        healthFill.fillAmount = currentHealth / maxHealth;

        alreadyHealing = true;
        lastHealedTime = Time.time;

        StartHealingCooldown();
    }


    private Coroutine healingCooldownCoroutine;

    private IEnumerator HealingCooldownRoutine()
    {
        yield return new WaitForSeconds(healingCooldown);
        alreadyHealing = false;
    }

    private void StartHealingCooldown()
    {
        if (healingCooldownCoroutine != null)
            StopCoroutine(healingCooldownCoroutine);

        healingCooldownCoroutine = StartCoroutine(HealingCooldownRoutine());
    }



    // ------ GIZMOS

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
