using UnityEngine;

public class piratanimation : MonoBehaviour
{
    [SerializeField]
    private float walkSpeed = 5f;
    
    private Animator animator;
    private Rigidbody rb;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    }

    void Update()
    {
        // Ottieni input WASD
        float horizontalInput = Input.GetAxis("Horizontal");  // A/D
        float verticalInput = Input.GetAxis("Vertical");      // W/S

        // Calcola la direzione del movimento
        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        // Ruota il personaggio nella direzione del movimento
        if (movement != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(movement);
        }

        // Applica il movimento usando velocity
        rb.linearVelocity = movement * walkSpeed;

        // Aggiorna l'animator usando velocity
        animator.SetFloat("Speed", rb.linearVelocity.magnitude);
    }
}