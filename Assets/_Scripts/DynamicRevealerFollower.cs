using UnityEngine;

public class DynamicRevealerFollower : MonoBehaviour
{
    [Header("Riferimenti")]
    public Transform ratTransform;
    public Transform cameraTransform;
    [Range(0f, 1f)]
    public float distanceFactor = 0.9f; // 0 = camera, 1 = topo

    void LateUpdate()
    {
        if (ratTransform == null || cameraTransform == null) return;

        Vector3 direction = ratTransform.position - cameraTransform.position;
        transform.position = cameraTransform.position + direction * distanceFactor;

        // Mantieni altezza costante sulla fog plane
        transform.position = new Vector3(transform.position.x, ratTransform.position.y, transform.position.z);
    }
}
