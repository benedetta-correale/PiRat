using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RatInputHandler : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintMultiplier = 1.5f;

    [Tooltip("Velocitï¿½ con cui il personaggio ruota verso la direzione di movimento")]
    public float rotationSpeed = 10f;

    [Header("Animation")]
    [Tooltip("Parametro Animator per gestire la velocità dell'animazione")]
    public string animSpeedParam = "SpeedMultiplier";
    [Tooltip("Moltiplicatore minimo della velocità dell'animazione quando l'input è minimo")]
    public float minAnimSpeed = 0.5f;
    [Tooltip("Moltiplicatore massimo della velocità dell'animazione quando l'input è a intensità massima")]
    public float maxAnimSpeed = 1.6f;

    Rigidbody rb;
    public Vector2 moveInput { get; private set; }
    bool isSprinting;
    private Animator _ratAnimator;

    void Awake() => rb = GetComponent<Rigidbody>();

    private void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratAnimator.SetBool("isWalking", false);
        // Imposta inizialmente la velocità di animazione al valore minimo
        _ratAnimator.SetFloat(animSpeedParam, minAnimSpeed);
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

        // Aggiorna animazioni
        UpdateWalkingAnimation(desiredMove.magnitude);
    }

    /// <summary>
    /// Aggiorna i parametri Animator per camminata e velocità
    /// </summary>
    private void UpdateWalkingAnimation(float inputMagnitude)
    {
        // Mantieni il bool di walking
        bool walking = inputMagnitude > 0.001f;
        _ratAnimator.SetBool("isWalking", walking);

        // Solo se siamo nello stato di camminata, modifichiamo la velocità dell'animazione
        var state = _ratAnimator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("WalkRatAnimation"))
        {
            // Calcola velocità tra min e max in base all'intensità dell'input
            float t = Mathf.Clamp01(inputMagnitude);
            float animSpeed = Mathf.Lerp(minAnimSpeed, maxAnimSpeed, t);
            _ratAnimator.SetFloat(animSpeedParam, animSpeed);
        }
        else
        {
            // Negli altri stati, manteniamo velocità di default
            _ratAnimator.SetFloat(animSpeedParam, 1f);
        }
    }

    public Vector2 GetMoveInputRaw()
    {
        return moveInput;
    }
}
