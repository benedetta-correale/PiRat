using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("References (assign in Inspector)")]
    public RatControllerTeaser ratController;
    public Transform ratTransform;
    public Transform pirateTransform;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.P;

    [Header("Camera Offset & Rotation")]
    public Vector3 cameraOffset = new Vector3(0f, 5f, -10f);
    public Vector3 cameraEulerAngles = new Vector3(20f, 0f, 0f);

    [Header("Transition Settings")]
    [Range(0.1f, 10f)] public float transitionSpeed = 3f;

    private Transform camTransform;
    private bool followPirate = false;
    private Transform currentTarget;

    void Start()
    {
        camTransform = Camera.main.transform;
        currentTarget = ratTransform;
        camTransform.rotation = Quaternion.Euler(cameraEulerAngles);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            followPirate = !followPirate;
            ratController.enabled = !followPirate;
            currentTarget = followPirate ? pirateTransform : ratTransform;
        }
    }

    void LateUpdate()
    {
        // Posizione target desiderata
        Vector3 targetPosition = currentTarget.position + cameraOffset;

        // Interpolazione della posizione
        camTransform.position = Vector3.Lerp(camTransform.position, targetPosition, Time.deltaTime * transitionSpeed);

        // Interpolazione della rotazione
        Quaternion targetRotation = Quaternion.Euler(cameraEulerAngles);
        camTransform.rotation = Quaternion.Slerp(camTransform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
    }
}