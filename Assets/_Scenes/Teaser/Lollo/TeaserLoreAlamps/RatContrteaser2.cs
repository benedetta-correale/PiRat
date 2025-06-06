using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RatControllerTeaser2 : MonoBehaviour
{
    [Header("Parametri di movimento")]
    public float moveSpeed = 5f;           // Velocità di spostamento orizzontale
    public float sprintMultiplier = 2f;    // Moltiplicatore velocità quando Shift è premuto
    public float rotationSpeed = 10f;      // Velocità di rotazione verso la direzione di marcia
    private Animator _ratAnimator;         // Riferimento all'animatore
    [SerializeField] private float _ratRay = 5f;

    Rigidbody rb;
    Transform camTransform;

    void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratAnimator.SetBool("isWalking", false);
        rb = GetComponent<Rigidbody>();
        // Blocca rotazioni X/Z e movimento Y
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        camTransform = Camera.main.transform;
    }

    void FixedUpdate()
    {
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            currentSpeed *= sprintMultiplier;

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        // Applica il movimento nella direzione in cui il personaggio è già rivolto
        Vector3 movement = transform.forward * inputZ + transform.right * inputX;
        if (movement.magnitude > 1f)
            movement.Normalize();

        // Applica la velocità
        rb.linearVelocity = movement * currentSpeed;

        // Rimuoviamo la rotazione automatica
        // Il personaggio mantiene la sua rotazione originale
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
}
