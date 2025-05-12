using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RatController : MonoBehaviour
{
    [Header("Parametri di movimento")]
    public float moveSpeed = 5f;        // Velocità di spostamento orizzontale
    public float sprintMultiplier = 2f;    // Moltiplicatore velocità quando Shift è premuto
    public float rotationSpeed = 10f;   // Velocità di rotazione verso la direzione di marcia
    private Animator _ratAnimator; // Riferimento all'animatore
    [SerializeField] private float _ratRay = 5f;  // Added default value

    Rigidbody rb;
    Transform camTransform;

    
    [Header("Script attacco")]
    [SerializeField] private SkillCheck _skillCheck;
    [SerializeField] private EnemyController _enemyController;

    void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratAnimator.SetBool("isWalking", false); // Imposta l'animazione di camminata
        rb = GetComponent<Rigidbody>();
        // Blocca le rotazioni sugli assi X e Z se non vuoi che il personaggio rotoli
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        camTransform = Camera.main.transform;  // Assicurati che la Main Camera sia taggata "MainCamera"

        // Add this line to get the EnemyController reference
        if (_enemyController == null)
        {
            _enemyController = GameObject.FindGameObjectWithTag("Pirate").GetComponent<EnemyController>();
            if (_enemyController == null)
            {
                Debug.LogError("EnemyController not found on Pirate!");
            }
        }
    }

    void FixedUpdate()
    {
        // Controlla se Shift è premuto
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            currentSpeed *= sprintMultiplier;
        }

        // 1. Input grezzo
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
        // 2. Normalizza (evita boost in diagonale)
        if (inputDir.magnitude > 1f)
            inputDir.Normalize();

        Vector3 camForward = transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = transform.right;
        camRight.y = 0f;
        camRight.Normalize();

        // 4. Direzione di movimento nel mondo
        Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

        // 5. Applica la velocità orizzontale mantenendo la componente Y (gravità)
        Vector3 currentVel = rb.linearVelocity;
        Vector3 targetVel = moveDir * moveSpeed;
        rb.linearVelocity = new Vector3(targetVel.x, currentVel.y, targetVel.z);

        // 6. Rotazione graduale verso la direzione di movimento
        if (moveDir.sqrMagnitude > 0.001f && inputZ >= 0)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }
    }

    private void OnDrawGizmos()
    {
        DrawCircle(_ratRay);
    }

    // Aggiungi questo nuovo metodo
    private void Update()
    {
        // Disegna la linea usando Debug.DrawLine che è visibile in Play Mode
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;
            
            Vector3 currentPoint = transform.position + (Quaternion.Euler(0, angle, 0) * Vector3.forward * _ratRay);
            Vector3 nextPoint = transform.position + (Quaternion.Euler(0, nextAngle, 0) * Vector3.forward * _ratRay);
            
            Debug.DrawLine(currentPoint, nextPoint, Color.red);
        }

        // Call infectPirate to check for and infect nearby pirates
        infectPirate();
        chekIsWalking();
    }

    

    private void chekIsWalking()
    {
        if (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
        {
            _ratAnimator.SetBool("isWalking", true);
        }
        else
        {
            _ratAnimator.SetBool("isWalking", false);
        }
    }

    private void DrawCircle(float radius)
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void infectPirate()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, _ratRay);

        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Pirate"))
            {
                if (_enemyController.startFollowing == false)
                {
                    if (Input.GetKeyDown(KeyCode.I))
                    {_skillCheck.StartSkillCheck(); 
                    Debug.Log("Pirata infettato");}
                }
                else
                {
                    Debug.Log("Il pirata NON può essere infettato (sta inseguendo)");
                }

                // esce al primo pirata trovato
                break;
            }
        }
    }
}
