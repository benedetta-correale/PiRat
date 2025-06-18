// PossessionManager.cs (aggiornato con listener a OnSwitchedToRat)
using UnityEngine;
using System.Collections.Generic;

public class PossessionManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public RatInteractionManager ratInteraction;
    public GameObject sciaPrefab;
    public Transform ratTransform;
    public RatInputHandler ratInput;
    public CameraControlManager cameraManager;

    [Header("Impostazioni selezione")]
    public bool isSelecting = false;
    private int selectedIndex = -1;
    private bool isPossessingPirate = false;

    [Header("Filtro distanza possessione")]
    public float maxPossessionDistance = 10f;

    private List<Transform> InfectedPirates => ratInteraction.infectedPirates;
    private List<Transform> InfectedPiratesInRange =>
        ratInteraction.infectedPirates.FindAll(p =>
            Vector3.Distance(p.position, ratTransform.position) <= maxPossessionDistance);
    private List<LineRenderer> scieAttive = new List<LineRenderer>();

    void Start()
    {
        if (cameraManager != null)
        {
            cameraManager.OnSwitchedToRat += HandleReturnToRat;
        }
    }

    void OnDestroy()
    {
        if (cameraManager != null)
        {
            cameraManager.OnSwitchedToRat -= HandleReturnToRat;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isPossessingPirate)
        {
            EnterSelectionMode();
        }

        if (isSelecting)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitSelectionMode();
            }

            HandleSelectionInput();
        }

        if (!isSelecting && !ratInput.enabled && Input.GetKeyDown(KeyCode.Escape))
        {
            SwitchToRat();
        }
    }

    void HandleSelectionInput()
    {
        if (InfectedPirates.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelection();
        }

        Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (inputDir != Vector2.zero)
        {
            SelectClosestInDirection(inputDir.normalized);
        }

        AggiornaScie();
    }

    void EnterSelectionMode()
    {
        isSelecting = true;
        selectedIndex = -1;
        AggiornaScie();
        ShowScie();

        if (ratInput != null)
        {
            ratInput.enabled = false;
        }
    }

    void ExitSelectionMode()
    {
        isSelecting = false;
        selectedIndex = -1;
        HideScie();

        if (ratInput != null)
        {
            ratInput.enabled = true;
        }
    }

    void ConfirmSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < InfectedPirates.Count)
        {
            cameraManager.SwitchToPirate(InfectedPirates[selectedIndex]);
            ratInput.enabled = false;
            isPossessingPirate = true;
            ExitSelectionMode();
        }
    }

    void SwitchToRat()
    {
        cameraManager.SwitchToRat();
        ratInput.enabled = true;
        isPossessingPirate = false;
    }

    void HandleReturnToRat()
    {
        isPossessingPirate = false;
    }

    void SelectClosestInDirection(Vector2 inputDir)
    {
        float bestDot = -1f;
        int bestIndex = -1;

        for (int i = 0; i < InfectedPirates.Count; i++)
        {
            Vector3 toPirate = InfectedPirates[i].position - ratTransform.position;
            Vector2 toPirate2D = new Vector2(toPirate.x, toPirate.z).normalized;
            float dot = Vector2.Dot(inputDir, toPirate2D);

            if (dot > bestDot)
            {
                bestDot = dot;
                bestIndex = i;
            }
        }

        if (bestIndex != -1)
        {
            selectedIndex = bestIndex;
            Debug.Log("Pirata selezionato: " + InfectedPirates[selectedIndex].name);
        }
    }

    void AggiornaScie()
    {
        if (!isSelecting) return;

        var infected = InfectedPirates;

        while (scieAttive.Count < infected.Count)
        {
            var newScia = Instantiate(sciaPrefab).GetComponent<LineRenderer>();
            newScia.gameObject.SetActive(false);
            scieAttive.Add(newScia);
        }

        while (scieAttive.Count > infected.Count)
        {
            Destroy(scieAttive[scieAttive.Count - 1].gameObject);
            scieAttive.RemoveAt(scieAttive.Count - 1);
        }

        for (int i = 0; i < infected.Count; i++)
        {
            var scia = scieAttive[i];
            var target = infected[i];

            scia.SetPosition(0, ratTransform.position);
            scia.SetPosition(1, target.position + Vector3.up * 0.5f);
        }
    }

    public void ShowScie()
    {
        foreach (var scia in scieAttive)
        {
            scia.gameObject.SetActive(true);
        }
    }

    public void HideScie()
    {
        foreach (var scia in scieAttive)
        {
            scia.gameObject.SetActive(false);
        }
    }
}