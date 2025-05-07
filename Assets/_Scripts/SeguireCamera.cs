using UnityEngine;

public class SeguireCamera : MonoBehaviour
{
    public Transform target;        // Il topo da seguire
    public Vector3 offset = new Vector3(0, 5, -7); // Offset posizione camera
    public float smoothSpeed = 0.125f; // Velocità di inseguimento

    void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(target);
    }
}
