using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class OutlineVisibilityController : MonoBehaviour
{
    [Header("Riferimenti")]
    public GameObject outlineObject;      // RatMesh_Outline
    public Camera cam;
    public LayerMask occluderMask = ~0;   // i layer di muri/oggetti

    SkinnedMeshRenderer _smr;
    Coroutine _visRoutine;
    const float CHECK_INTERVAL = 0.1f;

    void Start()
    {
        if (outlineObject == null)
        {
            Debug.LogError($"[{name}] Devi assegnare RatMesh_Outline!");
            enabled = false;
            return;
        }

        // ☆ Disattiva subito l’outline
        outlineObject.SetActive(false);

        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError($"[{name}] Nessuna Camera.main trovata!");
            enabled = false;
            return;
        }

        _smr = GetComponent<SkinnedMeshRenderer>();
        // rende l’outline sempre “updateable”
        var oSMR = outlineObject.GetComponent<SkinnedMeshRenderer>();
        if (oSMR != null) oSMR.updateWhenOffscreen = true;

        // avvia la routine
        _visRoutine = StartCoroutine(VisibilityRoutine());
    }

    void OnDisable()
    {
        if (_visRoutine != null) StopCoroutine(_visRoutine);
    }

    IEnumerator VisibilityRoutine()
    {
        var wait = new WaitForSeconds(CHECK_INTERVAL);
        while (true)
        {
            CheckVisibility();
            yield return wait;
        }
    }

    void CheckVisibility()
    {
        // centro del bounding box
        Vector3 target = _smr.bounds.center;
        Vector3 dir = target - cam.transform.position;
        float dist = dir.magnitude;

        if (Physics.Raycast(cam.transform.position, dir.normalized, out var hit, dist, occluderMask))
        {
            // se il primo hit non appartiene al topo → è occluso
            bool occluso = !hit.collider.transform.IsChildOf(transform);
            outlineObject.SetActive(occluso);
        }
        else
        {
            // linea di vista pulita
            outlineObject.SetActive(false);
        }
    }
}
