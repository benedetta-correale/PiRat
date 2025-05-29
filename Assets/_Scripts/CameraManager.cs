using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public CinemachineCamera vcamRat;
    public CinemachineCamera vcamPirate;
    [SerializeField] private SeguireCamera seguireCamera;
    private bool isPirateCameraActive = false;
    private Transform pirateTransform;
    public bool cameraIsSwitched = false;

    private void Start()
    {
        // All'avvio attiviamo solo la camera del topo
        vcamRat.gameObject.SetActive(true);
        vcamPirate.gameObject.SetActive(false);
        seguireCamera = FindObjectOfType<SeguireCamera>();
    }

    void Update()
    {
        HandleCameraSwitch();
    }

    private void HandleCameraSwitch()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!isPirateCameraActive)
            {
                SwitchToPirata(pirateTransform);
                isPirateCameraActive = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.R) && isPirateCameraActive)
        {
            SwitchToTopo();
            isPirateCameraActive = false;
        }
    }

    public void SetPirateTransform(Transform pirate)
    {
        pirateTransform = pirate;
    }

    public void SwitchToPirata(Transform pirateTarget)
    {
        vcamPirate.Follow = pirateTarget;
        vcamPirate.LookAt = pirateTarget;
        vcamPirate.gameObject.SetActive(true);
        vcamRat.gameObject.SetActive(false);
        if (seguireCamera != null)
        {
            seguireCamera.target = pirateTarget;
        }

        Debug.Log("Switched to PIRATA camera");
        cameraIsSwitched = true;

    }

    public void SwitchToTopo()
    {   
        vcamPirate.gameObject.SetActive(false);
        vcamRat.gameObject.SetActive(true);
        Transform ratTransform = GameObject.FindGameObjectWithTag("Player").transform;
        vcamRat.Follow = ratTransform;
        vcamRat.LookAt = ratTransform;
        if (seguireCamera != null)
        {
            seguireCamera.target = ratTransform;
        }
        Debug.Log("Switched back to TOPO camera");
    }
}

