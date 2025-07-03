using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]
public class RatInteractionManager : MonoBehaviour
{
    [SerializeField] private float _ratRay = 5f;
    [SerializeField] private float biteCooldown = 1f; // cooldown in secondi
    private bool canBite = true;
    [SerializeField] private int Damage = 30;
    private int bonusDamage = 0;

    private Animator _ratAnimator;
    private RatInputHandler _ratInputHandler;

    [SerializeField] private QuickTimeUIManager quickTimeUIManager;
    private bool quickTimeConfirmed = false;
    private bool isQuickTimeActive = false;

    [SerializeField] private QuickTimeVFXManager vfxManager;


    [Header("Effetti dell' attacco")]
    public bool biting = false;
    public InfectionSkillCheckUI skillCheck;
    private PirateController enemyController;

    private CameraControlManager cameraControlManager;


    //  Nuovo: lista dei pirati infettati
    public List<Transform> infectedPirates = new List<Transform>();

    private GameObject poisonPrefab;
    private bool canPee = false;
    public bool isBackflipping = false;


    void Start()
    {
        _ratAnimator = GetComponent<Animator>();
        _ratInputHandler = GetComponent<RatInputHandler>();
    }


    void Update()
    {
        //  Mostra cerchio di rilevamento
        int segments = 32;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;
            Vector3 p1 = transform.position + Quaternion.Euler(0, angle, 0) * Vector3.forward * _ratRay;
            Vector3 p2 = transform.position + Quaternion.Euler(0, nextAngle, 0) * Vector3.forward * _ratRay;
            Debug.DrawLine(p1, p2, Color.red);
        }



        // Premi 1-9 per entrare nei pirati infettati
        for (int i = 0; i < infectedPirates.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (cameraControlManager != null)
                {
                    cameraControlManager.SwitchToPirate(infectedPirates[i]);
                }
            }
        }

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _ratRay);
    }

    public void OnBite(InputAction.CallbackContext context)
    {
        if (context.performed && canBite)
        {
            AttemptInfection();
            Debug.Log("Input morso ricevuto");

        }
    }

    private void AttemptInfection()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float biteDistance = 2f;
        Vector3 direction = transform.forward;

        bool successfulBite = false;

        // 1. 🔍 Raggio frontale
        if (Physics.Raycast(origin, direction, out hit, biteDistance, LayerMask.GetMask("PirateHittable")))
        {
            if (hit.collider.CompareTag("Pirate"))
            {
                TryStartBite(hit.collider.GetComponent<PirateController>());
                successfulBite = true;
            }
            else if (hit.collider.CompareTag("Cheese"))
            {
                TryCheeseBite(hit.collider.GetComponent<CheesePowerUp>());
                successfulBite = true;
            }
        }

        // 2. 🟠 Nessun hit col raggio → tentativo sferico ravvicinato
        if (!successfulBite)
        {
            PirateController bestTarget = null;
            float bestDot = -1f;

            Collider[] hits = Physics.OverlapSphere(transform.position, 0.8f, LayerMask.GetMask("PirateHittable"));

            foreach (Collider c in hits)
            {
                if (c.CompareTag("Pirate"))
                {
                    Vector3 toPirate = (c.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, toPirate);
                    if (dot > 0.5f && dot > bestDot)
                    {
                        Vector3 rayOrigin = origin;
                        Vector3 rayTarget = c.transform.position + Vector3.up * 0.5f;
                        Vector3 dir = (rayTarget - rayOrigin).normalized;
                        float dist = Vector3.Distance(rayOrigin, rayTarget);

                        // Occlusione → ignora se qualcosa blocca
                        if (!Physics.Raycast(rayOrigin, dir, dist, LayerMask.GetMask("Default", "Wall")))
                        {
                            bestDot = dot;
                            bestTarget = c.GetComponentInParent<PirateController>();
                        }
                    }
                }
            }

            if (bestTarget != null)
            {
                TryStartBite(bestTarget);
                successfulBite = true;
            }
            else
            {
                // 🧀 Tentativo ravvicinato su Cheese
                Collider[] cheeseHits = Physics.OverlapSphere(transform.position, 0.8f);
                foreach (Collider c in cheeseHits)
                {
                    if (c.CompareTag("Cheese"))
                    {
                        TryCheeseBite(c.GetComponent<CheesePowerUp>());
                        successfulBite = true;
                        break;
                    }
                }
            }
        }

        // 3. ❌ Fallito → comunque trigger l'animazione del morso
        if (!successfulBite)
        {
            TriggerFailedBite();
        } 

        canBite = false;
        Invoke(nameof(ResetBiteCooldown), biteCooldown);
    }




    private void TryStartBite(PirateController controller)
    {
        if (controller == null) return;
        enemyController = controller;
        _ratInputHandler.movementLocked = true;
        biting = true;
        _ratAnimator.SetTrigger("Bite");
        StartCoroutine(StartQuickTimeEvent(enemyController));
    }

    private void TryCheeseBite(CheesePowerUp cheese)
    {
        if (cheese == null) return;
        _ratInputHandler.movementLocked = true;
        cheese.ActivatePowerUp(this);
        _ratAnimator.SetTrigger("BiteWithJumpBack");
        StartCoroutine(UnlockAfterAnimationFixed(1.5f));
    }

    private void TriggerFailedBite()
    {
        Debug.Log("Nessun bersaglio trovato!");
        _ratInputHandler.movementLocked = true;
        _ratAnimator.SetTrigger("BiteWithJumpBack");
        StartCoroutine(UnlockAfterAnimationFixed(1.8f));
    }



    private void ResetBiteCooldown()
    {
        canBite = true;
    }


    // 👇 Nuovo: registra un pirata nella lista e si sottoscrive alla sua morte
    private void Infect(PirateController pirate)
    {
        if (!infectedPirates.Contains(pirate.transform))
        {
            infectedPirates.Add(pirate.transform);
            pirate.OnPirateDeath += RemoveDeadPirate;
            
        }
    }

    public void ActivateDamageBoost(int bonus)
    {
        bonusDamage = bonus;
    }

    private IEnumerator StartQuickTimeEvent(PirateController targetPirate)
    {
        Debug.Log("QTE INIZIATO");
        quickTimeConfirmed = false;
        isQuickTimeActive = true;

        quickTimeUIManager.StartQuickTime();
        float timer = 0f;

        while (quickTimeUIManager.IsQuickTimeActive)
        {
            timer += Time.deltaTime;
            if (quickTimeConfirmed) break;
            yield return null;
        }

        float precision = quickTimeUIManager.Precision;
        quickTimeUIManager.StopQuickTime();
        isQuickTimeActive = false;

        HandleQuickTimeResult(precision, quickTimeConfirmed, targetPirate);
    }



    private void HandleQuickTimeResult(float precision, bool buttonPressed, PirateController targetPirate)
    {
        if (!buttonPressed)
        {
            Debug.Log("QuickTime fallito, nessun pulsante premuto.");
            _ratAnimator.SetTrigger("JumpBack"); // Animazione fallita
            StartCoroutine(UnlockAfterAnimationFixed(1f));


            return;
        }

        float currentScale = quickTimeUIManager.CurrentScale;
        float startScale = quickTimeUIManager.StartingScale;
        float scaleRatio = currentScale / startScale;

        Debug.Log("QuickTime scale ratio: " + scaleRatio);

        // Zone mapping
        if (scaleRatio > 0.87f || scaleRatio < 0.24f) // zone esterna e interna nere
        {
            Debug.Log("❌ Fuori bersaglio (fallimento)");
            _ratAnimator.SetTrigger("JumpBack");
            StartCoroutine(UnlockAfterAnimationFixed(1f));
            return;
        }
        else if ((scaleRatio >= 0.75f && scaleRatio <= 0.87f) || (scaleRatio <= 0.38f && scaleRatio >= 0.24f))
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🟡 Zona gialla");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(0.5f, 0.4f);
        }
        else if ((scaleRatio >= 0.63f && scaleRatio < 0.75f) || (scaleRatio <= 0.5 && scaleRatio > 0.38f))
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🔵 Zona blu");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(1f, 0.7f);
        }
        else
        {
            vfxManager.PlayBiteVFX();
            isBackflipping = true;
            Debug.Log("🔴 Zona rossa");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(1.5f, 1f);
        }



        bonusDamage = 0; // reset danno extra
    }

    private void ExecuteBackflip(float distanceMultiplier, float delayBeforeMove = 0.35f)
    {
        isBackflipping = true; // ← lo mettiamo subito, non dentro la coroutine
        StartCoroutine(PerformBackflip(distanceMultiplier, delayBeforeMove));
    }


    private IEnumerator PerformBackflip(float distanceMultiplier, float delayBeforeMove = 0.35f)
    {

        Debug.Log("BACKFLIP INIZIATO - isBackflipping = true");
        // Avvia animazione Backflip
        _ratAnimator.SetTrigger("Backflip");

        yield return new WaitForSeconds(delayBeforeMove); // aspetta inizio animazione

        Vector2 inputDir = _ratInputHandler.GetMoveInputRaw();
        Vector3 backwardDir = -transform.forward;

        if (inputDir.magnitude > 0.1f)
        {
            Vector3 desiredDir = new Vector3(inputDir.x, 0, inputDir.y);
            desiredDir = Camera.main.transform.TransformDirection(desiredDir);
            desiredDir.y = 0;
            desiredDir.Normalize();

            float angle = Vector3.Angle(backwardDir, desiredDir);
            if (angle <= 35f)
            {
                backwardDir = desiredDir;
            }
            else
            {
                backwardDir = Vector3.Slerp(backwardDir, desiredDir, 35f / angle);
            }
        }

        float backflipDistance = 2f * distanceMultiplier;

        Vector3 targetPosition = transform.position + backwardDir * backflipDistance;

        float elapsedTime = 0f;
        float duration = 0.3f;
        Vector3 startPos = transform.position;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        _ratInputHandler.movementLocked = false;
        isBackflipping = false;
        Debug.Log("BACKFLIP TERMINATO - isBackflipping = false");
    }


    private IEnumerator UnlockAfterAnimationFixed(float delay)
    {
        yield return new WaitForSeconds(delay);
        _ratInputHandler.movementLocked = false;
    }

    public void OnQuickTimeConfirm(InputAction.CallbackContext context)
    {
        if (context.performed && isQuickTimeActive)
        {
            quickTimeConfirmed = true;
        }
    }

    public void EnablePoisonLeak(GameObject puddlePrefab)
    {
        canPee = true;
        poisonPrefab = puddlePrefab;
    }

    public void OnPiss(InputAction.CallbackContext ctx)
    {
        if (ctx.performed && canPee && poisonPrefab != null)
        {
            StartCoroutine(HandlePeeAction());
        }
    }

    private IEnumerator HandlePeeAction()
    {
        _ratInputHandler.movementLocked = true;

        GameObject puddle = Instantiate(poisonPrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.5f); // Tempo dell'animazione del "piss", regola in base alla durata reale

        _ratInputHandler.movementLocked = false;
        canPee = false;
    }

    // 👇 Aggiungi questo metodo alla classe RatInteractionManager
    public void RegisterInfectedPirate(PirateController pirate)
    {
        if (!infectedPirates.Contains(pirate.transform))
        {
            infectedPirates.Add(pirate.transform);
            pirate.OnPirateDeath += RemoveDeadPirate;
            Debug.Log("Pirata infettato tramite pozza di veleno!");
        }
    }
    // 👇 Nuovo: rimuove il pirata morto
    private void RemoveDeadPirate(PirateController deadPirate)
    {
        if (infectedPirates.Contains(deadPirate.transform))
        {
            infectedPirates.Remove(deadPirate.transform);
        }
    }
}
