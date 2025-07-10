using System.Collections.Generic;
using UnityEngine;

public class PirateProximityOutlining : MonoBehaviour
{
    // Rimosso il riferimento esterno al ratTransform
    [SerializeField] private float biteRaycastDistance = 2f;
    [SerializeField] private float nearSphereRadius = 0.8f;

    void Update()
    {
        // 1) Spegni tutte le outline
        PirateController[] allPirates = Object.FindObjectsByType<PirateController>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var p in allPirates)
        {
            var outl = p.GetComponent<PirateOutline>();
            if (outl != null)
                outl.SetOutline(false);
        }

        // 2) Raycast dal muso in avanti
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, biteRaycastDistance,
                            LayerMask.GetMask("PirateHittable")))
        {
            // 3) Se è un Pirate, accendi solo quello
            if (hit.collider.CompareTag("Pirate"))
            {
                var outl = hit.collider.GetComponent<PirateOutline>();
                if (outl != null)
                    outl.SetOutline(true);
            }
        }
    }



    private List<Transform> FindPirates()
    {
        List<Transform> result = new List<Transform>();

        // Cambiato ratTransform.position in this.transform.position
        Vector3 origin = this.transform.position + Vector3.up * 0.5f;
        Vector3 dir = this.transform.forward;

        // Raggio frontale
        if (Physics.Raycast(origin, dir, out RaycastHit hit, biteRaycastDistance,
            LayerMask.GetMask("PirateHittable")))
        {
            if (hit.collider.CompareTag("Pirate"))
            {
                result.Add(hit.collider.transform);
            }
        }

        // Sfera ravvicinata
        Collider[] hits = Physics.OverlapSphere(
            this.transform.position,
            nearSphereRadius,
            LayerMask.GetMask("PirateHittable")
        );
        foreach (Collider c in hits)
        {
            if (c.CompareTag("Pirate") && !result.Contains(c.transform))
            {
                result.Add(c.transform);
            }
        }

        return result;
    }
}
