using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControlManager : MonoBehaviour
{
    public static CameraControlManager Instance { get; private set; }
    private bool rotationLocked = false;
    [Header("References (assign in Inspector)")]
    public RatInputHandler ratController;
    public Transform ratTransform;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Escape;

    //[Header("Camera Offset & Rotation")]
    //public Vector3 cameraOffset = new Vector3(0f, 5f, -10f);
    //public Vector3 cameraEulerAngles = new Vector3(20f, 0f, 0f);

    [Header("Transition Settings")]
    [Range(0.1f, 10f)] public float transitionSpeed = 3f;

    //private Transform camTransform;
    private bool followPirate = false;
    private Transform currentTarget;
    private Transform pirateTransform;

    [Header("Offset")]
    [Tooltip("Offset locale rispetto al target: X = spostamento laterale, Y = altezza, Z = distanza dietro")]
    public Vector3 offset = new Vector3(0f, 13f, -13f);

    [Header("Selection Zoom")]
    [Tooltip("Offset della camera in modalità selezione del topo")]
    public Vector3 selectionOffset = new Vector3(0f, 20f, -20f);

    // campo privato per salvare l’offset di default
    private Vector3 defaultOffset;
    private Vector3 currentOffset;
    private Vector3 targetOffset;

    public Vector3 cameraInitialPosition;
    public Vector3 cameraInitialRotation;


    [Header("Settings")]
    [Tooltip("Velocità di rotazione orizzontale")]
    public float sensitivity = 120f;

    [Header("Collisione Camera")]
    public LayerMask cameraCollisionMask;
    public float cameraMinDistance = 1f; // distanza minima di sicurezza dal topo


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
        pirateTransform = null;
        currentTarget = ratTransform;

        if (currentTarget == null)
        {
            Debug.LogError("CameraController: manca il riferimento a Target!");
            enabled = false;
            return;
        }

        yaw = currentTarget.eulerAngles.y; // oppure 0f se vuoi partire sempre dietro


        // ⬇️ Aggiunta: forza subito la posizione corretta al primo frame
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
        transform.position = currentTarget.position + rot * offset;
        transform.LookAt(currentTarget.position);
        defaultOffset = offset;
        currentOffset = offset;
        targetOffset = offset;
        transform.position = cameraInitialPosition;
        transform.rotation = Quaternion.Euler(cameraInitialRotation);
    }

    public void LockRotation(bool locked)
    {
        rotationLocked = locked;
    }
    void Update()
    {
        /*if (Input.GetKeyDown(toggleKey))
        {
            followPirate = !followPirate;
            ratController.enabled = !followPirate;
            if (pirateTransform != null) currentTarget = followPirate ? pirateTransform : ratTransform;
        }*/
    }

    // Invocato dal PlayerInput → Invoke Unity Events sulla action "Look"
    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        // anche con rotationLocked=true, aggiorno sempre posizione e look,
        // soltanto la rotazione (yaw) viene bloccata
        if (!rotationLocked)
        {
            // aggiorna solo yaw (rotazione orizzontale)
            yaw += lookInput.x * sensitivity * Time.deltaTime;
        }

        // costruisci la rotazione orizzontale a partire dal yaw
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        // interpola lo zoom in modo smooth
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, transitionSpeed * Time.deltaTime);

        // posizione e look
        Vector3 desiredCameraPos = currentTarget.position + rot * currentOffset;
        transform.position = desiredCameraPos;
        transform.LookAt(currentTarget.position);

    }


    public void SwitchToPirate(Transform pirate)
    {
        Debug.Log("Switching to pirate: " + pirate.name);
        pirateTransform = pirate;
        followPirate = true;
        ratController.enabled = false;
        currentTarget = pirateTransform;
    }

    public event System.Action OnSwitchedToRat;

    public void SwitchToRat()
    {
        followPirate = false;
        currentTarget = ratTransform;
        ratController.enabled = true;
        OnSwitchedToRat?.Invoke();
    }

    /// <summary>
    /// Zoom out per mostrare tutti gli strands dal topo in modalità selezione
    /// </summary>
    public void ApplySelectionZoom()
    {
        targetOffset = selectionOffset;
    }


    /// <summary>
    /// Ripristina lo zoom di default (usato quando entri in modalità possessione)
    /// </summary>
    public void ResetZoom()
    {
        targetOffset = defaultOffset;
    }

    /// <summary>
    /// Fa seguire alla camera un transform arbitrario (es. il TrailRat)
    /// </summary>
    public void FollowTrail(Transform trailTransform)
    {
        currentTarget = trailTransform;
        ratController.enabled = false;
    }

}