using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI; // Add this for NavMeshAgent

public class EnemyController : MonoBehaviour
{
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public Animator animator;
    public float waitTimeAtPoint = 2f; // Tempo di attesa in secondi al punto di pattuglia

    [Header("Pirate Settings")]
    [SerializeField] private float _followSpeed = 3f;
    [SerializeField] private float _viewAngle = 90f; // Angolo del cono visivox
    [SerializeField] private float _viewDistance = 10f; // Distanza massima di vista
    [SerializeField] private float _rayAttachment = 3f; // distanza del raggio di attaccamento
    [SerializeField] private Material visionConeMaterial; // Aggiungi questo campo


    [Header("Follow Settings")]
    [SerializeField] private float _attachTime = 5f; // Tempo di attesa prima di iniziare a seguire
    private bool _startFollowing; // Fixed incomplete boolean declaration
    private bool _pirateIsWalking = true; // Aggiunto per gestire lo stato di camminata del pirata
    private bool _hasSpottedRat = false; // Bool che mi aiuta a capire quando ha visto il topo


    [Header("Vita del pirata")]
    public bool isInfected = false; // Aggiunto per gestire lo stato di infezione del pirata
    public float health = 100f; // Vita del pirata, puoi modificarla in base alle tue necessità
    public bool isPossessed = false; // Aggiunto per gestire lo stato di possesso del pirata
    private bool _isDead = false; // Aggiunto per gestire lo stato di morte del pirata


    [Header("UI Settings")]
    [SerializeField] private GameObject healthBarPrefab; // Prefab dell'health bar
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2f, 0); // Offset sopra il pirata
    private Slider _healthSlider; // Reference allo slider
    private Canvas _worldSpaceCanvas; // Canvas principale in World Space


    [Header("Camera Settings")]
    [SerializeField] private CameraManager cameraManager;

    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private bool waiting = false;


    // Riferimento AI VARI PERSONAGGI 
    private GameObject _mainCharacter;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private RatController ratController; // Riferimento al controller del ratto
    private float _waitingTime = 0f; // Add this as a class field at the top of the class
    void Start()

    {
        _mainCharacter = GameObject.FindGameObjectWithTag("Player");

        if (_mainCharacter == null)
        {
            Debug.LogError("Main character not found!");
            return;
        }


        // Trova il controller del ratto
        ratController = _mainCharacter.GetComponent<RatController>();
        if (ratController == null)
        {
            Debug.LogError("RatController not found on the main character!");
            return;
        }


        //mi colleggo all'animatore del pirata
        animator = GetComponent<Animator>();

        animator.SetBool("isWalking", true);
        agent = GetComponent<NavMeshAgent>();

        if (cameraManager == null)
        {
            cameraManager = FindObjectOfType<CameraManager>();
        }

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

        StartCoroutine(WaitAndGoToNextPoint()); 
        InitializeVisionCone();
        UpdateVisionCone();
        InitializeHealthBar();
        
    }

    // 3. Update the Update method to handle state changes
    void Update()
    {
        if (_mainCharacter != null)
        {
            Vector3 direction = _mainCharacter.transform.position - transform.position;
            float distance = direction.magnitude;

            bool isInViewCone = IsInViewCone(direction, distance);

            if (isInViewCone && !_hasSpottedRat)
            {
                Debug.Log("Il pirata ha avvistato il topo, comincia il countdown!");
                _hasSpottedRat = true;
                _pirateIsWalking = false;
                _waitingTime = 0f;
                _startFollowing = false;
                animator.SetBool("isWalking", false);
                agent.isStopped = true;  // Stop NavMeshAgent movement
            }

            if (_hasSpottedRat)
            {
                StartCountdown();  // Use corrected method name
            }
        }

        if (_startFollowing)
        {
            StartFollowing();
        }

        UpdateVisionCone();
    }
    

    // inizializzo il cono visivo
    private void InitializeVisionCone() {
    // Inizializza il cono di visione
    GameObject visionCone = new GameObject("VisionCone");
    visionCone.transform.parent = transform;
    visionCone.transform.localPosition = Vector3.zero;

    meshFilter = visionCone.AddComponent<MeshFilter>();
    meshRenderer = visionCone.AddComponent<MeshRenderer>();
    meshRenderer.material = visionConeMaterial;

}

    // 1. Fix the method name and comparison in startCountdown
    private void StartCountdown()
{
    _waitingTime += Time.deltaTime;

    if (_waitingTime >= _attachTime)  // Changed from == to >=
    {
        _startFollowing = true;
        _pirateIsWalking = true;
        animator.SetBool("isWalking", true);
        agent.isStopped = false;
        Debug.Log("Il pirata ha iniziato a seguire il topo!");
    }
}

    // 2. Fix the StartFollowing method to use NavMeshAgent
    public void StartFollowing()
{
    if (_mainCharacter == null || agent == null) return;

    Vector3 direction = _mainCharacter.transform.position - transform.position;
    float distance = direction.magnitude;

    if (distance <= _rayAttachment)
    {
        Debug.Log("Il pirata ha raggiunto il topo!");
        return;
    }

    // Use NavMeshAgent for movement
    agent.SetDestination(_mainCharacter.transform.position);
    agent.speed = _followSpeed;

    // Keep rotation logic for smooth turning
    Quaternion lookRotation = Quaternion.LookRotation(direction);
    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
}


    public bool IsInViewCone(Vector3 directionToTarget, float distance)
    {

        if (distance > _viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= _viewAngle * 0.5f;
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
    
    

    //metodo per disegnare il cono visivo
private void UpdateVisionCone()
{
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

    private void InitializeHealthBar()
{
    // Istanzia il prefab come figlio del pirata
    GameObject healthBar = Instantiate(healthBarPrefab, this.transform);
    healthBar.transform.localPosition = healthBarOffset;

    // Ottieni lo Slider
    _healthSlider = healthBar.GetComponentInChildren<Slider>();
    if (_healthSlider == null)
    {
        Debug.LogError("Slider non trovato nel prefab dell'health bar!");
        return;
    }

    // Imposta i valori iniziali
    _healthSlider.maxValue = health;
    _healthSlider.value = health;

    // Mostra subito la barra
    _healthSlider.gameObject.SetActive(true);
}


    public void TakeDamage()
    {
        if (!ratController.biting) return;

        if (health > 30f)
        {
            health -= 30f;
            isInfected = true; // Imposta il pirata come infetto
            if (_healthSlider != null && !_healthSlider.gameObject.activeSelf)
            {
                _healthSlider.gameObject.SetActive(true); // Mostra la barra vita al primo morso
            }
            UpdateHealthUI();
        }
        else
        {
            health = 0f;
            UpdateHealthUI();
            HandlePirateDeath();
        }
    }

    private void UpdateHealthUI()
    {
        if (_healthSlider != null)
        {
            _healthSlider.value = health;
        }
    }

        private void HandlePirateDeath()
    {
        _isDead = true;
        Debug.Log("Il pirata è morto!");

        // Hide health UI
        if (_healthSlider != null)
        {
            _healthSlider.gameObject.SetActive(false);
        }

        // TODO: Trigger death animation here
        // animator.SetTrigger("Death");
    }
    
    System.Collections.IEnumerator WaitAndGoToNextPoint()
    {
        waiting = true;
        animator.SetBool("isWalking", false);

        if (_pirateIsWalking)
        {
            waiting = false;
            yield break;
        }

        yield return new WaitForSeconds(waitTimeAtPoint);

        currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
        agent.SetDestination(patrolPoints[currentPointIndex].position);

        waiting = false;
        animator.SetBool("isWalking", true);
    }
}
