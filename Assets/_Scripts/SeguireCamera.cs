using UnityEngine;

public class SeguireCamera : MonoBehaviour
{
    [Header("Target da seguire")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Posizione della camera rispetto al target, in coordinate locali mondo")]
    public Vector3 positionOffset = new Vector3(-10f, 10f, -10f);
    [Tooltip("Rotazione della camera (Euler) per l’angolo di visuale")]
    public Vector3 rotationOffset = new Vector3(35f, 45f, 0f);

    [Header("Smoothing")]
    [Tooltip("Velocità di inseguimento")]
    public float followSpeed = 5f;
    [Tooltip("Velocità di allineamento rotazione")]
    public float rotationSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Posizione desiderata
        Vector3 desiredPos = target.position + positionOffset;
        // 2. Smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        // 3. Rotazione fissa (ma interpolata per maggiore fluidità)
        Quaternion desiredRot = Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSpeed * Time.deltaTime);
    }
}
