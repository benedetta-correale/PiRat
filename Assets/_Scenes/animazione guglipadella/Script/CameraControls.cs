using UnityEngine;
using UnityEngine.InputSystem;

public class CameraControls : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Il Transform del personaggio da tenere al centro")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Offset locale rispetto al target: X = spostamento laterale, Y = altezza, Z = distanza dietro")]
    public Vector3 offset = new Vector3(0f, 13f, -13f);

    [Header("Settings")]
    [Tooltip("Velocità di rotazione orizzontale")]
    public float sensitivity = 120f;

    float yaw;
    Vector2 lookInput;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraController: manca il riferimento a Target!");
            enabled = false;
            return;
        }
        // inizializza yaw dalla rotazione corrente
        yaw = transform.eulerAngles.y;
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
        transform.position = target.position + rot * offset;

        // guarda sempre il target
        transform.LookAt(target.position);
    }
}
