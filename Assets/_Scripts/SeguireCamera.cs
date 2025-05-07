using UnityEngine;

public class SeguireCamera : MonoBehaviour
{
    public Transform target;            // Il topo
    public Vector3 offset; // Offset rispetto al topo
    public float sideOffset = 2f;        // Quanto spostare lateralmente (a destra)
    public float smoothSpeed = 0.125f;  // Velocità smooth

    void LateUpdate()
    {
        // Posizione base dietro al topo
        Vector3 desiredPosition = target.position 
                                  + target.up * offset.y 
                                  + target.forward * offset.z;

        // Aggiungiamo spostamento laterale a destra (usa target.right)
        desiredPosition += target.right * sideOffset;

        // Smooth movimento
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Guarda sempre il topo, leggermente sopra
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}