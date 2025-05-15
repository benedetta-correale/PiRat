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

    private Transform camTransform;
    private bool followPirate = false;
    private Transform currentTarget;

    void Start()
    {
        camTransform = Camera.main.transform;
        currentTarget = ratTransform;
        // inizializza la rotazione usando Euler
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
        // posiziona la camera rispetto al target + offset
        camTransform.position = currentTarget.position + cameraOffset;
        // Applica la rotazione che vedi in Inspector
        camTransform.rotation = Quaternion.Euler(cameraEulerAngles);
    }
}
