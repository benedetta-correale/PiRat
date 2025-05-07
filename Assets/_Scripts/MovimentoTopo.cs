using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovimentoTopo : MonoBehaviour
{
    [Header("Parametri di movimento")]
    public float moveSpeed = 5f;        // Velocità di spostamento orizzontale
    public float rotationSpeed = 10f;   // Velocità di rotazione verso la direzione di marcia

    Rigidbody rb;
    Transform camTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Blocca le rotazioni sugli assi X e Z se non vuoi che il personaggio rotoli
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        camTransform = Camera.main.transform;  // Assicurati che la Main Camera sia taggata "MainCamera"
    }

    void FixedUpdate()
    {
        // 1. Input grezzo
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(inputX, 0f, inputZ);

        // 2. Normalizza (evita boost in diagonale)
        if (inputDir.magnitude > 1f)
            inputDir.Normalize();

        // 3. Calcola forward/right rispetto alla camera (proietta sul piano)
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
        Vector3 targetVel = moveDir * moveSpeed;
        rb.linearVelocity = new Vector3(targetVel.x, currentVel.y, targetVel.z);

        // 6. Rotazione graduale verso la direzione di movimento
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
        }
    }
}
