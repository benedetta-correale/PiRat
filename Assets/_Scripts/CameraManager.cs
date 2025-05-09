using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public CinemachineCamera vcamRat;
    public CinemachineCamera vcamPirate;

    private void Start()
    {
        // All'avvio attiviamo solo la camera del topo
        vcamRat.gameObject.SetActive(true);
        vcamPirate.gameObject.SetActive(false);
    }

    public void SwitchToPirata(Transform pirateTarget)
    {
        vcamPirate.Follow = pirateTarget;
        vcamPirate.LookAt = pirateTarget;  // opzionale se vuoi sempre guardarlo
        vcamPirate.gameObject.SetActive(true);
        vcamRat.gameObject.SetActive(false);

        Debug.Log("Switched to PIRATA camera");
    }

    public void SwitchToTopo()
    {
        vcamRat.gameObject.SetActive(true);
        vcamPirate.gameObject.SetActive(false);
        Debug.Log("Switched back to TOPO camera");
    }
}

