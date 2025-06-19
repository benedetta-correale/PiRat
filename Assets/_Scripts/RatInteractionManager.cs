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




    [Header("Effetti dell' attacco")]
    public bool biting = false;
    public InfectionSkillCheckUI skillCheck;
    private PirateController enemyController;

    private CameraControlManager cameraControlManager;

    //  Nuovo: lista dei pirati infettati
    public List<Transform> infectedPirates = new List<Transform>();


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

        

        //  Nuovo: premi 1-9 per entrare nei pirati infettati
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

        if (Physics.Raycast(origin, direction, out hit, biteDistance))
        {
            if (hit.collider.CompareTag("Pirate"))
            {
                Debug.Log("Colpito: " + hit.collider.name);
                enemyController = hit.collider.GetComponent<PirateController>();
                if (enemyController != null)
                {
                    biting = true;
                    _ratAnimator.SetTrigger("Bite"); // Avvia animazione morso
                    StartCoroutine(StartQuickTimeEvent(enemyController)); // Avvia QTE
                }
            }
            else if (hit.collider.CompareTag("Cheese"))
            {
                CheesePowerUp cheese = hit.collider.GetComponent<CheesePowerUp>();
                if (cheese != null)
                {
                    cheese.ActivatePowerUp(this);
                    _ratAnimator.SetTrigger("BiteWithJumpBack"); // salto indietro dopo aver preso il formaggio
                }
            }
            else
            {
                Debug.Log("Oggetto davanti non è un target valido!");
                _ratAnimator.SetTrigger("BiteWithJumpBack"); // animazione morso a vuoto
            }
        }
        else
        {
            Debug.Log("Nessun bersaglio davanti al topo!");
            _ratAnimator.SetTrigger("BiteWithJumpBack"); // morso a vuoto
        }

        canBite = false;
        Invoke(nameof(ResetBiteCooldown), biteCooldown);
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
            return;
        }

        float currentScale = quickTimeUIManager.CurrentScale;
        float startScale = quickTimeUIManager.StartingScale;
        float scaleRatio = currentScale / startScale;

        Debug.Log("QuickTime scale ratio: " + scaleRatio);

        // Zone mapping
        if (scaleRatio > 0.89f || scaleRatio < 0.24f) // zone esterna e interna nere
        {
            Debug.Log("❌ Fuori bersaglio (fallimento)");
            _ratAnimator.SetTrigger("JumpBack");
            return;
        }
        else if ((scaleRatio >= 0.75f && scaleRatio <=0.89f) || (scaleRatio <=0.38f && scaleRatio >=0.24f))
        {
            Debug.Log("🟡 Zona gialla");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(0.5f);
        }
        else if ((scaleRatio >= 0.63f && scaleRatio <0.75f) || (scaleRatio <= 0.5 && scaleRatio > 0.38f))
        {
            Debug.Log("🔵 Zona blu");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(1f);
        }
        else
        {
            Debug.Log("🔴 Zona rossa");
            targetPirate.TakeDamage(Damage + bonusDamage);
            Infect(targetPirate);
            ExecuteBackflip(1.5f);
        }



        bonusDamage = 0; // reset danno extra
    }

    private void ExecuteBackflip(float distanceMultiplier)
    {
        StartCoroutine(PerformBackflip(distanceMultiplier));
    }

    private IEnumerator PerformBackflip(float distanceMultiplier)
    {
        // Avvia animazione Backflip
        _ratAnimator.SetTrigger("Backflip");

        yield return new WaitForSeconds(0.1f); // aspetta inizio animazione

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
    }

    public void OnQuickTimeConfirm(InputAction.CallbackContext context)
    {
        if (context.performed && isQuickTimeActive)
        {
            quickTimeConfirmed = true;
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
