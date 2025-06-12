using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RatInputHandler : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Tooltip("Gradi al secondo per ruotare verso la direzione di movimento")]
    public float rotationSpeed = 360f;

    [Header("Animation")]
    [Tooltip("Parametro Animator per gestire la velocità dell'animazione")]
    public string animSpeedParam = "SpeedMultiplier";
    [Tooltip("Moltiplicatore minimo della velocità dell'animazione quando l'input è minimo")]
    public float minAnimSpeed = 0.5f;
    [Tooltip("Moltiplicatore massimo della velocità dell'animazione quando l'input è a intensità massima")]
    public float maxAnimSpeed = 1.6f;

    private Rigidbody rb;
    public Vector2 moveInput { get; private set; }
    private bool isSprinting;
    private Animator _ratAnimator;

    void Awake() => rb = GetComponent<Rigidbody>();

    private void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratAnimator.SetBool("isWalking", false);
        _ratAnimator.SetFloat(animSpeedParam, minAnimSpeed);
    }

    // Invocato da PlayerInput
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
        // Calcola direzione di movimento in world space
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        Vector3 desiredMove = forward * moveInput.y + right * moveInput.x;
        float currentSpeed = walkSpeed * (isSprinting ? sprintMultiplier : 1f);

        // Rotazione in loco verso desiredMove
        if (desiredMove.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredMove, Vector3.up);
            // usa Rigidbody per ruotare, mantenendo la sim physics
            Quaternion newRot = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.fixedDeltaTime
            );
            rb.MoveRotation(newRot);
        }

        // Movimento
        rb.MovePosition(rb.position + desiredMove * currentSpeed * Time.fixedDeltaTime);

        // Animazioni
        UpdateWalkingAnimation(desiredMove.magnitude);
    }

    private void UpdateWalkingAnimation(float inputMagnitude)
    {
        bool walking = inputMagnitude > 0.001f;
        _ratAnimator.SetBool("isWalking", walking);

        var state = _ratAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("WalkRatAnimation"))
        {
            float t = Mathf.Clamp01(inputMagnitude);
            float animSpeed = Mathf.Lerp(minAnimSpeed, maxAnimSpeed, t);
            _ratAnimator.SetFloat(animSpeedParam, animSpeed);
        }
        else
        {
            _ratAnimator.SetFloat(animSpeedParam, 1f);
        }
    }

    public Vector2 GetMoveInputRaw() => moveInput;
}
