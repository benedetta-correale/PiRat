using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using System.Collections;

public class PirateController : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    public Animator animator;
    public float waitTimeAtPoint = 2f;
    private int _originalAreaMask;

    [Header("Pirate Settings")]
    [SerializeField] private float _followSpeed = 3f;
    [SerializeField] private float _viewAngle = 90f;
    [SerializeField] private float _viewDistance = 4f;
    [SerializeField] private float _rayAttachment = 3f;
    [SerializeField] private Material visionConeMaterial;
    public int attackDamage = 20;

    [Header("Follow Settings")]
    [SerializeField] private float _attachTime = 5f;
    [SerializeField] private float _stopAttachTime = 5f;
    private bool _startFollowing;
    private bool _pirateIsWalking = true;
    private bool _hasSpottedRat = false;
    private bool _hitRats = false;

    [Header("Vita del pirata")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    [SerializeField] private float damagePerHit = 20f;
    public bool isInfected = false;
    private bool _isDead;
    public bool isPossessed = false;
    public System.Action<PirateController> OnPirateDeath;

    [Header("UI Settings")]
    [SerializeField] private Image healthForegroundImage;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2f, 0);

    //[SerializeField] private GameObject deathEffect, hitEffect;
    [Header("Alert Settings")]
    [SerializeField] private GameObject alertIndicator;   // l'intero oggetto
    [SerializeField] private Image alertFillImage;        // foreground (fill)
    private bool alertFinished = false;



    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private bool waiting = false;
    private bool isAlive = true;

    private GameObject _mainCharacter;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private RatInteractionManager ratController;

    private float _waitingTime = 0f;
    private float _lostSightTimer = 0f;

    void Start()
    {
        _isDead = false;
        _mainCharacter = GameObject.FindGameObjectWithTag("Player");
        
        if (healthForegroundImage != null)
            healthForegroundImage.fillAmount = 1f;

        if (healthForegroundImage != null)
            healthForegroundImage.transform.parent.gameObject.SetActive(false); // nasconde la barra

        if (_mainCharacter == null)
        {
            Debug.LogError("Main character not found!");
            return;
        }

        ratController = _mainCharacter.GetComponent<RatInteractionManager>();
        if (ratController == null)
        {
            Debug.LogError("RatController not found on the main character!");
            return;
        }

        animator = GetComponent<Animator>();
        animator.SetBool("isWalking", true);

        agent = GetComponent<NavMeshAgent>();
        _originalAreaMask = agent.areaMask;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("No patrol points assigned! Please set patrol points in the Inspector.");
            return;
        }

        agent.SetDestination(patrolPoints[currentPointIndex].position);

        StartCoroutine(PatrolRoutine());
        InitializeVisionCone();
        UpdateVisionCone();

        currentHealth = maxHealth;

        if (healthForegroundImage != null)
            healthForegroundImage.fillAmount = 1f; // 100% iniziale

        if (agent.isOnNavMesh)
        {
            agent.SetDestination(patrolPoints[currentPointIndex].position);
        }
        else
        {
            Debug.LogWarning($"Pirate {name} non è sul NavMesh all’avvio!");
        }

        if (patrolPoints != null)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
            Debug.Log($"Patrol Point {i}: {patrolPoints[i].position}");
            }
        }
        else
        {
            Debug.Log("Patrol Points array is null");
        }

    }

    void Update()
    {
        if (_mainCharacter != null)
        {
            Vector3 direction = _mainCharacter.transform.position - transform.position;
            float distance = direction.magnitude;

            bool isInViewCone = IsInViewCone(direction, distance);

            if (isInViewCone && !_hasSpottedRat && _hitRats)
            {
                _hasSpottedRat = true;
                _pirateIsWalking = false;
                _waitingTime = 0f;
                _startFollowing = false;
                animator.SetBool("isWalking", false);
                agent.isStopped = true;

                StartCoroutine(RotateTowardsTarget(direction));
            }

            if (_hasSpottedRat)
            {
                StartCountdown();
            }
        }

        CheckHitRat();

        if (_startFollowing)
        {
            StartFollowing();
            StopFollowingIfLostSight();
        }

        UpdateVisionCone();
    }

    private IEnumerator RotateTowardsTarget(Vector3 direction)
    {
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        float rotationSpeed = 2.0f;

        while (Quaternion.Angle(transform.rotation, lookRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void InitializeVisionCone()
    {
        Transform existingCone = transform.Find("VisionCone");
        if (existingCone != null)
            Destroy(existingCone.gameObject);

        GameObject visionCone = new GameObject("VisionCone");
        visionCone.transform.SetParent(transform, false);
        visionCone.transform.localPosition = Vector3.zero;
        visionCone.transform.localRotation = Quaternion.identity;

        meshFilter = visionCone.AddComponent<MeshFilter>();
        meshRenderer = visionCone.AddComponent<MeshRenderer>();

        if (visionConeMaterial == null)
        {
            visionConeMaterial = new Material(Shader.Find("Standard"));
            visionConeMaterial.color = new Color(1f, 1f, 0f, 0.3f);
        }
        meshRenderer.material = visionConeMaterial;
    }

    private void StartCountdown()
    {
        if (alertFinished) return;  // 👈 blocca se già completato

        _waitingTime += Time.deltaTime;

        if (_mainCharacter != null)
        {
            Vector3 direction = _mainCharacter.transform.position - transform.position;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
        }

        // attiva indicator se serve
        if (alertIndicator != null && !alertIndicator.activeSelf)
        {
            alertIndicator.SetActive(true);
        }

        if (alertFillImage != null)
        {
            float fill = Mathf.Clamp01(_waitingTime / _attachTime);
            alertFillImage.fillAmount = fill;
        }

        if (_waitingTime >= _attachTime)
        {
            if (!_hitRats)
            {
                // topo sparito
                _hasSpottedRat = false;
                _startFollowing = false;

                ResetAlert();
                agent.isStopped = false;
                agent.areaMask = _originalAreaMask;
                agent.SetDestination(patrolPoints[currentPointIndex].position);
                animator.SetBool("isWalking", true);
            }
            else
            {
                // trovato topo
                _startFollowing = true;
                _pirateIsWalking = true;
                agent.isStopped = false;
                animator.SetBool("isWalking", true);
                agent.areaMask = NavMesh.AllAreas;

                // blocca alert fino a nuovo reset
                alertFinished = true;
                if (alertIndicator != null)
                    alertIndicator.SetActive(false);
                if (alertFillImage != null)
                    alertFillImage.fillAmount = 0f;
            }

            _waitingTime = 0f;
        }
    }

    private void ResetAlert()
    {
        alertFinished = false;

        if (alertIndicator != null)
            alertIndicator.SetActive(false);
        if (alertFillImage != null)
            alertFillImage.fillAmount = 0f;
    }

    public void StartFollowing()
    {
        if (_mainCharacter == null || agent == null) return;

        Vector3 direction = _mainCharacter.transform.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= _rayAttachment)
            return;

        agent.isStopped = false;
        agent.areaMask = NavMesh.AllAreas;
        agent.SetDestination(_mainCharacter.transform.position);
        agent.speed = _followSpeed;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);

        _pirateIsWalking = true;
        animator.SetBool("isWalking", true);
    }

    private void StopFollowingIfLostSight()
    {
        if (!_hitRats)
        {
            _lostSightTimer += Time.deltaTime;

            if (_lostSightTimer >= _stopAttachTime)
            {
                Debug.Log("Il pirata ha perso il topo per troppo tempo. Torna in pattuglia.");

                _startFollowing = false;
                _hasSpottedRat = false;
                _lostSightTimer = 0f;

                // resetta il punto interrogativo
                ResetAlert();

                agent.isStopped = false;
                agent.areaMask = _originalAreaMask;
                agent.speed = agent.speed / _followSpeed;
                agent.SetDestination(patrolPoints[currentPointIndex].position);
                animator.SetBool("isWalking", true);
            }
        }
        else
        {
            _lostSightTimer = 0f;
        }
    }

    public bool IsInViewCone(Vector3 directionToTarget, float distance)
    {
        if (distance > _viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= _viewAngle * 0.5f;
    }

    private void OnTriggerEnter(Collider other)
    {
        OnAttack(other);
    }

    private void OnAttack(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 directionToRat = other.transform.position - transform.position;
            float distanceToRat = directionToRat.magnitude;

            if (IsInViewCone(directionToRat, distanceToRat) && distanceToRat <= _rayAttachment)
            {
                animator.SetBool("isWalking", false);
                animator.SetBool("isAttacking", true);
                StartCoroutine(ResetAttackTrigger());

                BonusMalus bonusMalus = other.GetComponent<BonusMalus>();
                if (bonusMalus != null)
                {
                    bonusMalus.TakeDamage(attackDamage);
                }
            }
        }
        if (other.CompareTag("Player"))
        {
            BonusMalus bonusMalus = other.GetComponent<BonusMalus>();
            if (bonusMalus != null)
            {
                bonusMalus.TakeDamage(attackDamage);
            }
        }

    }

    private IEnumerator ResetAttackTrigger()
    {
        yield return new WaitForSeconds(1.2f);
        animator.SetBool("isAttacking", false);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log("Pirata ha preso danno! Vita attuale: " + currentHealth); // 👈 nuovo log

        isInfected = true;
        UpdateHealthUI();

        if (currentHealth <= 0f)
        {
            HandlePirateDeath();
        }

        if (healthForegroundImage != null && !healthForegroundImage.transform.parent.gameObject.activeSelf)
        {
            healthForegroundImage.transform.parent.gameObject.SetActive(true);
        }
    }


    private void UpdateHealthUI()
    {
        if (healthForegroundImage != null)
        {
            float percent = currentHealth / maxHealth;
            healthForegroundImage.fillAmount = percent;
        }
    }

    private void HandlePirateDeath()
    {
        _isDead = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false; // blocca in modo definitivo
        }

        animator.SetBool("isWalking", false);
        animator.SetTrigger("Die"); // attiva l'animazione di morte

        if (healthForegroundImage != null)
            healthForegroundImage.fillAmount = 0f;

        OnPirateDeath?.Invoke(this);
        
    }

    private IEnumerator PatrolRoutine()
    {
        Debug.Log("isWalking: " + animator.GetBool("isWalking"));

        float stuckTimer = 0f;

        while (true)
        {
            if (_isDead) yield break;

            if (_startFollowing || _hasSpottedRat)
            {
                stuckTimer = 0f;
                yield return null;
                continue;
            }

            if (!agent.enabled || !agent.isOnNavMesh)
            {
                stuckTimer = 0f;
                yield return null;
                continue;
            }

            if (_pirateIsWalking && !waiting)
            {
                Debug.Log("RemainingDistance: " + agent.remainingDistance);

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    Debug.Log("Capitano arrivato al punto " + currentPointIndex);

                    waiting = true;
                    animator.SetBool("isWalking", false);

                    yield return new WaitForSeconds(waitTimeAtPoint);

                    // Cambia punto: 0 → 1, 1 → 0

                    currentPointIndex = (currentPointIndex == 0) ? 1 : 0;
                    Debug.Log("Prossimo punto: " + currentPointIndex + " → " + patrolPoints[currentPointIndex].position);

                    // Ruota verso il nuovo punto (opzionale ma utile)
                    yield return StartCoroutine(RotateTowards(patrolPoints[currentPointIndex].position));


                    //vado al nuovo punto 
                    animator.SetBool("isWalking", true);
                    waiting = false;
                    agent.SetDestination(patrolPoints[currentPointIndex].position);

                    stuckTimer = 0f;
                }
            }

            yield return null;
        }
    }

    private IEnumerator RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction == Vector3.zero)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float rotationSpeed = 5f; // puoi aumentare o diminuire per più lentezza o velocità

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            yield return null;
        }

        // correzione finale precisa
        transform.rotation = targetRotation;
    }




    private void CheckHitRat()
    {
        if (_mainCharacter == null) return;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Vector3 targetCenter = _mainCharacter.transform.position + Vector3.up * 0.5f;
        Vector3 directionToTarget = (targetCenter - origin).normalized;
        float distance = Vector3.Distance(origin, targetCenter);

        if (Physics.SphereCast(origin, 0.3f, directionToTarget, out hit, distance))
        {
            if (hit.collider.transform.root.gameObject == _mainCharacter)
                _hitRats = true;
            else
                _hitRats = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftDirection = Quaternion.Euler(0, -_viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0, _viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDirection * _viewDistance);
        Gizmos.DrawRay(transform.position, rightDirection * _viewDistance);
        Gizmos.DrawWireSphere(transform.position, _viewDistance);

        int numLines = 10;
        for (int i = 0; i < numLines; i++)
        {
            float angle = (-_viewAngle * 0.5f) + ((_viewAngle / (numLines - 1)) * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, direction * _viewDistance);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _rayAttachment);
    }

    public void DebugKillAfterSeconds(float seconds)
    {
        StartCoroutine(KillRoutine(seconds));
    }

    private IEnumerator KillRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HandlePirateDeath();
    }
    private void UpdateVisionCone()
    {
        if (meshFilter == null)
        {
            InitializeVisionCone();
            if (meshFilter == null)
            {
                Debug.LogError($"Failed to initialize MeshFilter on {gameObject.name}");
                return;
            }
        }

        int segments = 32;
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        float angleStep = _viewAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (-_viewAngle / 2) + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            vertices[i + 1] = direction * _viewDistance;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }
}
