using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;


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

    private PlayerInput playerInput;
    private InputAction moveAction;



    void Start()
    {
        if (cameraManager != null)
            cameraManager.OnSwitchedToRat += HandleReturnToRat;

        ratAnimator = ratTransform.GetComponent<Animator>();

        // Ottieni riferimento al PlayerInput
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
    }



    void OnDestroy()
    {
        if (cameraManager != null)
            cameraManager.OnSwitchedToRat -= HandleReturnToRat;
    }



    void Update()
    {
        if (currentState == PossessionState.Selecting)
        {
            HandleSelectionInput();
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
        selectedIndex = 0;


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

        // lettura input del movimento dallo stick del nuovo sistema
        Vector2 inputDir = moveAction.ReadValue<Vector2>();

        if (inputDir != Vector2.zero)

            SelectClosestInDirection(inputDir.normalized, piratesInRange);

        AggiornaScie(piratesInRange);
    }



    void ConfirmSelection(List<Transform> piratesInRange)
    {
        Debug.Log("Tentativo di conferma: selectedIndex=" + selectedIndex + ", piratesCount=" + piratesInRange.Count);

        if (selectedIndex < 0 || selectedIndex >= piratesInRange.Count)
        {
            Debug.LogWarning("Conferma fallita: indice selezionato non valido.");
            return;
        }

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

    // Metodo per entrare nella modalità selezione
    public void EnterSelectionMode_Input(InputAction.CallbackContext context)
    {
        if (context.performed && currentState == PossessionState.Idle)
            EnterSelectionMode();
    }

    // Metodo per uscire dalla modalità selezione o possessione
    public void ExitSelectionMode_Input(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentState == PossessionState.Selecting)
                ExitSelectionMode();
            else if (currentState == PossessionState.Possessing && canSwitchBackToRat)
                SwitchToRat();
        }
    }

    // Metodo per confermare la selezione del pirata da possedere
    public void ConfirmPossess_Input(InputAction.CallbackContext context)
    {
        if (context.performed && currentState == PossessionState.Selecting)
        {
            Debug.Log("Possess action triggered");

            var piratesInRange = GetPiratesInRange();
            if (selectedIndex == -1 && piratesInRange.Count > 0)
            {
                Debug.Log("Nessun selezionato, seleziono il primo di default");
                selectedIndex = 0;
            }

            ConfirmSelection(piratesInRange);
        }
    }


}