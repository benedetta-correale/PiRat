using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControlManager : MonoBehaviour
{
    public static CameraControlManager Instance { get; private set; }

    [Header("References (assign in Inspector)")]
    public RatInputHandler ratController;
    public Transform ratTransform;
    public Transform pirateTransform;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.P;

    //[Header("Camera Offset & Rotation")]
    //public Vector3 cameraOffset = new Vector3(0f, 5f, -10f);
    //public Vector3 cameraEulerAngles = new Vector3(20f, 0f, 0f);

    [Header("Transition Settings")]
    [Range(0.1f, 10f)] public float transitionSpeed = 3f;

    private Transform camTransform;
    private bool followPirate = false;
    private Transform currentTarget;

    [Header("Offset")]
    [Tooltip("Offset locale rispetto al target: X = spostamento laterale, Y = altezza, Z = distanza dietro")]
    public Vector3 offset = new Vector3(0f, 13f, -13f);

    [Header("Settings")]
    [Tooltip("Velocità di rotazione orizzontale")]
    public float sensitivity = 120f;

    float yaw;
    Vector2 lookInput;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    void Start()
    {
        camTransform = Camera.main.transform;
        pirateTransform = null;
        currentTarget = ratTransform;
        //camTransform.rotation = Quaternion.Euler(cameraEulerAngles);

        if (currentTarget == null)
        {
            Debug.LogError("CameraController: manca il riferimento a Target!");
            enabled = false;
            return;
        }
        // inizializza yaw dalla rotazione corrente
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            followPirate = !followPirate;
            ratController.enabled = !followPirate;
            if (pirateTransform != null) currentTarget = followPirate ? pirateTransform : ratTransform;
        }
    }

    // Invocato dal PlayerInput → Invoke Unity Events sulla action "Look"
    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        // aggiorna solo yaw (rotazione intorno all'asse Y)
        yaw += lookInput.x * sensitivity * Time.deltaTime;

        // costruisci la rotazione orizzontale
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        // posiziona la camera: Target + rotazione * Offset
        transform.position = currentTarget.position + rot * offset;

        // guarda sempre il target
        transform.LookAt(currentTarget.position);
    }

    public void SwitchToPirate(Transform pirate)
    {
        pirateTransform = pirate;
        followPirate = true;
        ratController.enabled = false;
        currentTarget = pirateTransform;
    }
}