using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RatController : MonoBehaviour
{
    [Header("Parametri di movimento")]
    public float moveSpeed = 5f;           // Velocità di spostamento orizzontale
    public float sprintMultiplier = 2f;    // Moltiplicatore velocità quando Shift è premuto
    public float rotationSpeed = 10f;      // Velocità di rotazione verso la direzione di marcia
    private Animator _ratAnimator;         // Riferimento all'animatore
    [SerializeField] private float _ratRay = 5f;

    Rigidbody rb;
    Transform camTransform;

    [Header("Effetti dell' attacco")]
    public bool biting = false; // Indica se il ratto sta mordendo
    public SkillCheck skillCheck;
    public EnemyController enemyController;



    void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratAnimator.SetBool("isWalking", false);
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        camTransform = Camera.main.transform;

    }

    void FixedUpdate()
    {
        // 1. Determina la velocità corrente (sprint)
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            currentSpeed *= sprintMultiplier;

        // 2. Input grezzo e normalizzazione
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);
        if (inputDir.magnitude > 1f)
            inputDir.Normalize();

        // 3. Calcola forward e right rispetto alla camera (proietta sul piano)
        Vector3 camForward = camTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();
        Vector3 camRight = camTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        // 4. Direzione di movimento nel mondo
        Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

        // 5. Applica la velocità orizzontale mantenendo la componente Y (gravità)
        Vector3 currentVel = rb.linearVelocity;
        Vector3 targetVel = moveDir * currentSpeed;
        rb.linearVelocity = new Vector3(targetVel.x, currentVel.y, targetVel.z);

        // 6. Rotazione graduale verso la direzione di movimento
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }
    }


    private void Update()
    {
        // Disegna il cerchio di rilevamento (solo in Play Mode)
        int segments = 32;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;
            Vector3 p1 = transform.position + Quaternion.Euler(0, angle, 0) * Vector3.forward * _ratRay;
            Vector3 p2 = transform.position + Quaternion.Euler(0, nextAngle, 0) * Vector3.forward * _ratRay;
            Debug.DrawLine(p1, p2, Color.red);
        }

        infectPirate();
        chekIsWalking();
    }

    private void chekIsWalking()
    {
        if (Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f)
            _ratAnimator.SetBool("isWalking", true);
        else
            _ratAnimator.SetBool("isWalking", false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _ratRay);
    }

    private void infectPirate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _ratRay);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Pirate"))
            {
                enemyController = hit.GetComponent<EnemyController>();  // Fixed 'get hit' syntax error

                if (enemyController.startFollowing == false)  // Changed _enemyController to enemyController
                {
                    if (Input.GetKeyDown(KeyCode.I))
                    {
                        //skillCheck.StartSkillCheck();
                        Debug.Log("Pirata infettato");

                        if (enemyController != null)  // Fixed missing opening parenthesis
                        {
                            biting = true;
                            enemyController.TakeDamage();
                        }
                    }
                }
                else
                {
                    Debug.Log("Il pirata NON può essere infettato (sta inseguendo)");
                }
                break;
            }
        }
    }
}
