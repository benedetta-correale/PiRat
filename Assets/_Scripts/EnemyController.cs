using UnityEngine;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{

    [Header("Pirate Settings")]
    [SerializeField] private float _followSpeed = 3f;
    [SerializeField] private float _viewAngle = 90f; // Angolo del cono visivox
    [SerializeField] private float _viewDistance = 10f; // Distanza massima di vista
    [SerializeField] private float _rayAttachment = 3f; // distanza del raggio di attaccamento
    [SerializeField] private Material visionConeMaterial; // Aggiungi questo campo


    [Header("Follow Settings")]
    [SerializeField] private float _attachTime = 5f; // Tempo di attesa prima di iniziare a seguire
    public Animator animator; // Riferimento all'animatore del pirata
    public bool startFollowing; // Fixed incomplete boolean declaration
    public bool pirateIsWalking = true; // Aggiunto per gestire lo stato di camminata del pirata


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



    // Riferimento AI VARI PERSONAGGI 
    private GameObject _mainCharacter;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private RatController ratController; // Riferimento al controller del ratto
    private float _waitingTime = 0f; // Add this as a class field at the top of the class
    void Start()
    {
        _mainCharacter = GameObject.FindGameObjectWithTag("Player");
        ratController = _mainCharacter.GetComponent<RatController>();
        if (ratController == null)
        {
            Debug.LogError("RatController not found on the main character!");
            return;
        }
        

        //mi colleggo all'animatore del pirata
        animator = GetComponent<Animator>();



        //controllo l'esistenza dell'animatore e del personaggio principale
        if (animator == null)
        {
            Debug.LogError("Animator component not found on the pirate!");
            return;
        }

        if (_mainCharacter == null)
        {
            Debug.LogError("Main character not found!");
            return;
        }


        // Inizializza il cono di visione
        GameObject visionCone = new GameObject("VisionCone");
        visionCone.transform.parent = transform;
        visionCone.transform.localPosition = Vector3.zero;

        meshFilter = visionCone.AddComponent<MeshFilter>();
        meshRenderer = visionCone.AddComponent<MeshRenderer>();
        meshRenderer.material = visionConeMaterial;

        UpdateVisionCone();

        // Inizializza l'health bar
        InitializeHealthBar();
    }

    void Update()
    {
        if (_mainCharacter != null)
        {
            Vector3 direction = _mainCharacter.transform.position - transform.position;
            float distance = direction.magnitude;

            bool isInViewCone = IsInViewCone(direction, distance);

            if (isInViewCone)
            {
                startCoundown();
            }
        }

        // Fixed if statement syntax
        if (startFollowing)
        {
            StartFollowing();
        }

        UpdateVisionCone(); // Aggiorna il cono di visione ogni frame

        // Aggiorna la posizione dell'health bar solo se il pirata è infetto
        /* if (_healthSlider != null && isInfected)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + healthBarOffset);
            
            // Controlla se il pirata è davanti alla camera
            if (screenPos.z > 0)
            {
                screenPos.z = 0;
                _healthSlider.transform.position = screenPos;
            }
        }
        */
    }

    private void startCoundown()
    {
        pirateIsWalking = false; // Imposto il pirata come non in camminata



        //inizio il countdown per l'attacco 
        _waitingTime += Time.deltaTime;

        if (_waitingTime >= _attachTime)
        {
            startFollowing = true;
            _waitingTime = 0f; // Reset the timer
            Debug.Log("Il pirata ha iniziato a seguire il topo!");
        }
    }

    public void StartFollowing()

    {
        Vector3 direction = _mainCharacter.transform.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= _rayAttachment)
        {
            Debug.Log("Il pirata ha raggiunto il topo!");

            // LOGICA PER QUANDO IL PIRATA RAGGIUNGE IL TOPO
            startFollowing = false; //DA MODIFICARE PERCHE' IL TOPO POTREBBE INFETTARE ANCORA IL PIRATA
            return;
        }

        //ROTAZIONE GRADUALE
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);

        // MOVIMENTO IN AVANTI
        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * _followSpeed * Time.deltaTime;
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
}
