using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using FischlWorks_FogWar;
using UnityEngine.AI;
using System;



public enum PossessionState { Idle, Selecting, FollowingTrail, Possessing }


public class PossessionManager : MonoBehaviour
{
    [Header("Riferimenti")]
    public RatInteractionManager ratInteraction;
    public GameObject sciaPrefab;
    public DynamicRevealerFollower dynamicRevealerFollower;    // assegna lo script che segue il buco di nebbia
    public float sciaHeightOffset = 1f;                        // quanto alzare le linee sopra quel punto


    public Transform ratTransform;
    public RatInputHandler ratInput;
    public CameraControlManager cameraManager;

    public GameObject trailPrefab;                  // prefabricato con NavMeshAgent
    public float trailStoppingDistance = 0.5f;      // distanza di arrivo del trail
    private TrailRatController currentTrail;        // componente del trail istanziato
    private Transform currentTrailTarget;           // pirata da inseguire


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
        // zoom out camera per vedere tutti gli strands del topo
        cameraManager.ApplySelectionZoom();

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



    private void ConfirmSelection(List<Transform> piratesInRange)
    {
        HideScie();

        // disabilita input e animator del ratto
        if (ratInput != null)
        {
            ratInput.enabled = false;
            ratInput.movementLocked = true;
        }
        if (ratAnimator != null)
            ratAnimator.SetBool("isWalking", false);

        // cattura il target PRIMA di resettare selectedIndex
        Transform target = piratesInRange[selectedIndex];
        currentTrailTarget = target;

        // esci dalla selezione (qui si resetta anche selectedIndex a -1)
        ExitSelectionMode();

        // adesso posso cambiare stato
        currentState = PossessionState.FollowingTrail;

        Vector3 spawnPos = ratTransform.position;
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 1f, NavMesh.AllAreas))
            spawnPos = hit.position;
        GameObject go = Instantiate(trailPrefab, spawnPos, Quaternion.identity);
        currentTrail = go.GetComponent<TrailRatController>();
        // <-- nuova patch qui
        var agent = go.GetComponent<NavMeshAgent>();
        agent.stoppingDistance = trailStoppingDistance;
        // <-- fine patch
        currentTrail.OnArrived += OnTrailArrived;
        currentTrail.MoveTo(currentTrailTarget);


        cameraManager.ResetZoom();
        cameraManager.FollowTrail(currentTrail.transform);
        cameraManager.LockRotation(true);

    }





    void ExitSelectionMode()
    {
        selectedIndex = -1;
        HideScie();
        cameraManager.ResetZoom();

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
    // ─────────────────────────────────────────────────────────────────
    // Chiamato quando il giocatore subisce un attacco:
    // esce da Selection o da Possession a prescindere dallo stato corrente.
    public void OnAttacked()
    {
        if (currentState == PossessionState.Selecting)
        {
            ExitSelectionMode();
        }
        else if (currentState == PossessionState.FollowingTrail)
        {
            InterruptTrail();
            cameraManager.SwitchToRat();
            cameraManager.ResetZoom();
            cameraManager.LockRotation(false);
            ExitSelectionMode();
        }

        else if (currentState == PossessionState.Possessing)
        {
            // torna automaticamente al ratto
            SwitchToRat();
        }
    }
    // ─────────────────────────────────────────────────────────────────

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
            newScia.material.renderQueue = 4000;
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

            // calcola il punto di partenza sopra il buco di nebbia (o fallback sul topo)
            Vector3 start = ratTransform.position + Vector3.up * sciaHeightOffset;
            if (dynamicRevealerFollower != null)
                start = dynamicRevealerFollower.transform.position + Vector3.up * sciaHeightOffset;

            // calcola il punto di arrivo leggermente più in alto sul pirata
            Vector3 end = target.position + Vector3.up * sciaHeightOffset;

            scia.SetPosition(0, start);
            scia.SetPosition(1, end);



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
        if (!context.performed)
            return;

        // prendi la lista aggiornata di pirati
        var piratesInRange = GetPiratesInRange();
        if (piratesInRange.Count == 0)
            return;

        // se sono in Idle (nessuna selezione attiva) e ho almeno 1 pirata
        if (currentState == PossessionState.Idle)
        {
            // entra in selezione (gestisce già animator, input e zoom)
            EnterSelectionMode();

            // conferma subito la selezione
            ConfirmSelection(piratesInRange);
            return;
        }

        // se sono già in Selecting, conferma normalmente
        if (currentState == PossessionState.Selecting)
        {
            ConfirmSelection(piratesInRange);
        }
    }



    private void OnTrailArrived()
    {
        // 1) riattacca camera al pirata
        cameraManager.SwitchToPirate(currentTrailTarget);
        cameraManager.ResetZoom();
        cameraManager.LockRotation(false);

        // 2) distruggi il Trail
        currentTrail.OnArrived -= OnTrailArrived;
        Destroy(currentTrail.gameObject);
        currentTrail = null;

        // 3) completa il possesso
        currentState = PossessionState.Possessing;
        PirateController pc = currentTrailTarget.GetComponent<PirateController>();
        if (pc != null) pc.isPossessed = true;
        currentTrailTarget = null;
    }

    private void InterruptTrail()
    {
        if (currentTrail != null)
        {
            currentTrail.OnArrived -= OnTrailArrived;
            Destroy(currentTrail.gameObject);
            currentTrail = null;
        }
    }



}