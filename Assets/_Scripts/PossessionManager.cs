using UnityEngine;
using System.Collections.Generic;

public enum PossessionState { Idle, Selecting, Possessing }

public class PossessionManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public RatInteractionManager ratInteraction;
    public GameObject sciaPrefab;
    public Transform ratTransform;
    public RatInputHandler ratInput;
    public CameraControlManager cameraManager;

    [Header("Impostazioni selezione")]
    public float maxSelectionDistance = 15f;

    private PossessionState currentState = PossessionState.Idle;
    private int selectedIndex = -1;
    private bool canSwitchBackToRat = true;

    private List<LineRenderer> scieAttive = new List<LineRenderer>();
    private Animator ratAnimator;

    void Start()
    {
        if (cameraManager != null)
            cameraManager.OnSwitchedToRat += HandleReturnToRat;

        ratAnimator = ratTransform.GetComponent<Animator>();
    }

    void OnDestroy()
    {
        if (cameraManager != null)
            cameraManager.OnSwitchedToRat -= HandleReturnToRat;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == PossessionState.Selecting)
            {
                ExitSelectionMode();
                return;
            }
            else if (currentState == PossessionState.Possessing && canSwitchBackToRat)
            {
                SwitchToRat();
                return;
            }
        }

        switch (currentState)
        {
            case PossessionState.Idle:
                if (Input.GetKeyDown(KeyCode.Tab))
                    EnterSelectionMode();
                break;

            case PossessionState.Selecting:
                HandleSelectionInput();
                break;
        }
    }

    void EnterSelectionMode()
    {
        var piratesInRange = GetPiratesInRange();

        if (piratesInRange.Count == 0)
        {
            Debug.Log("Nessun pirata infetto nel raggio di selezione.");
            return;
        }

        currentState = PossessionState.Selecting;

        // ✅ Auto-selezione se c’è solo un pirata
        selectedIndex = (piratesInRange.Count == 1) ? 0 : -1;

        AggiornaScie(piratesInRange);
        ShowScie();

        if (ratInput != null)
        {
            ratInput.enabled = false;
            ratInput.movementLocked = true;
        }

        if (ratAnimator != null)
            ratAnimator.SetBool("isWalking", false);
    }

    void HandleSelectionInput()
    {
        var piratesInRange = GetPiratesInRange();
        if (piratesInRange.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ConfirmSelection(piratesInRange);
            return;
        }

        Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (inputDir != Vector2.zero)
            SelectClosestInDirection(inputDir.normalized, piratesInRange);

        AggiornaScie(piratesInRange);
    }

    void ConfirmSelection(List<Transform> piratesInRange)
    {
        if (selectedIndex < 0 || selectedIndex >= piratesInRange.Count) return;

        cameraManager.SwitchToPirate(piratesInRange[selectedIndex]);
        // ✅ Imposta il flag isPossessed sul PirateController
        PirateController pc = piratesInRange[selectedIndex].GetComponent<PirateController>();
        if (pc != null) pc.isPossessed = true;


        if (ratInput != null)
        {
            ratInput.enabled = false;
            ratInput.movementLocked = true;
        }

        if (ratAnimator != null)
            ratAnimator.SetBool("isWalking", false);

        ExitSelectionMode();
        currentState = PossessionState.Possessing;
    }

    void ExitSelectionMode()
    {
        selectedIndex = -1;
        HideScie();

        if (currentState == PossessionState.Selecting)
        {
            currentState = PossessionState.Idle;

            if (ratInput != null)
            {
                ratInput.enabled = true;
                ratInput.movementLocked = false;
            }

            if (ratAnimator != null)
                ratAnimator.SetBool("isWalking", false);
        }
    }

    void SwitchToRat()
    {
        // ✅ Disattiva il flag isPossessed su tutti i pirati
        foreach (Transform p in ratInteraction.infectedPirates)
        {
            PirateController pc = p.GetComponent<PirateController>();
            if (pc != null) pc.isPossessed = false;
        }

        cameraManager.SwitchToRat();

        if (ratInput != null)
        {
            ratInput.enabled = true;
            ratInput.movementLocked = false;
        }

        if (ratAnimator != null)
            ratAnimator.SetBool("isWalking", false);

        currentState = PossessionState.Idle;
        canSwitchBackToRat = false;
        Invoke(nameof(EnableSwitchBack), 0.2f);
    }

    void EnableSwitchBack() => canSwitchBackToRat = true;

    void HandleReturnToRat()
    {
        currentState = PossessionState.Idle;
    }

    void SelectClosestInDirection(Vector2 inputDir, List<Transform> piratesInRange)
    {
        float bestDot = -1f;
        int bestIndex = -1;

        for (int i = 0; i < piratesInRange.Count; i++)
        {
            Vector3 toPirate = piratesInRange[i].position - ratTransform.position;
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
            Debug.Log("Pirata selezionato: " + piratesInRange[selectedIndex].name);
        }
    }

    void AggiornaScie(List<Transform> piratesInRange)
    {
        if (currentState != PossessionState.Selecting) return;

        while (scieAttive.Count < piratesInRange.Count)
        {
            var newScia = Instantiate(sciaPrefab).GetComponent<LineRenderer>();
            newScia.gameObject.SetActive(false);
            scieAttive.Add(newScia);
        }

        while (scieAttive.Count > piratesInRange.Count)
        {
            Destroy(scieAttive[scieAttive.Count - 1].gameObject);
            scieAttive.RemoveAt(scieAttive.Count - 1);
        }

        Color selectedColor = Color.green;
        Color defaultColor = new Color(1f, 1f, 1f, 0.2f); // bianco traslucido

        for (int i = 0; i < piratesInRange.Count; i++)
        {
            var scia = scieAttive[i];
            var target = piratesInRange[i];

            scia.SetPosition(0, ratTransform.position);
            scia.SetPosition(1, target.position + Vector3.up * 0.5f);

            if (scia.material != null)
            {
                scia.material.color = (i == selectedIndex) ? selectedColor : defaultColor;
            }
        }
    }

    public void ShowScie()
    {
        foreach (var scia in scieAttive)
            scia.gameObject.SetActive(true);
    }

    public void HideScie()
    {
        foreach (var scia in scieAttive)
            scia.gameObject.SetActive(false);
    }

    private List<Transform> GetPiratesInRange()
    {
        return ratInteraction.infectedPirates.FindAll(p =>
            Vector3.Distance(p.position, ratTransform.position) <= maxSelectionDistance);
    }
}