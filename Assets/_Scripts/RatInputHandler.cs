// PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RatInputHandler : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Tooltip("Velocit� con cui il personaggio ruota verso la direzione di movimento")]
    public float rotationSpeed = 10f;

    Rigidbody rb;
    Vector2 moveInput;
    bool isSprinting;
    private Animator _ratAnimator;

    void Awake() => rb = GetComponent<Rigidbody>();
    private void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratAnimator.SetBool("isWalking", false);
    }

    // Questi metodi vengono invocati dal PlayerInput (Invoke Unity Events)
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        isSprinting = ctx.ReadValueAsButton();
    }

    void FixedUpdate()
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 desiredMove = forward * moveInput.y + right * moveInput.x;
        float speed = walkSpeed * (isSprinting ? sprintMultiplier : 1f);

        if (desiredMove.sqrMagnitude > 0.001f)
        {
            // calcola la rotazione target guardando nella direzione di movimento
            Quaternion targetRot = Quaternion.LookRotation(desiredMove, Vector3.up);
            // applica una Slerp per rendere la rotazione fluida
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );
        }

        rb.MovePosition(rb.position + desiredMove * speed * Time.fixedDeltaTime);

        chekIsWalking();
    }

    private void chekIsWalking()
    {
        bool walking = moveInput.sqrMagnitude > 0.001f;
        _ratAnimator.SetBool("isWalking", walking);
    }

}
