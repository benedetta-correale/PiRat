// PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControls : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

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

        rb.MovePosition(rb.position + desiredMove * speed * Time.fixedDeltaTime);

        chekIsWalking();
    }

    private void chekIsWalking()
    {
        if (Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f)
            _ratAnimator.SetBool("isWalking", true);
        else
            _ratAnimator.SetBool("isWalking", false);
    }

}
