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


    [Header("Effetti dell' attacco")]
    public bool biting = false;
    public InfectionSkillCheckUI skillCheck;
    public PirateController enemyController;

    private CameraControlManager cameraControlManager;

    //  Nuovo: lista dei pirati infettati
    public List<Transform> infectedPirates = new List<Transform>();


    void Start()
    {
        //cameraControlManager = FindObjectOfType<CameraControlManager>();
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
                enemyController = hit.collider.GetComponent<PirateController>();

                if (enemyController != null)
                {
                    biting = true;
                    enemyController.TakeDamage(Damage + bonusDamage);
                    Debug.Log("Morso effettuato sul pirata davanti! Danno extra: " + Damage);

                    bonusDamage = 0; // reset dopo il morso, se mi ero potenziato
                    Infect(enemyController);

                    canBite = false;
                    Invoke(nameof(ResetBiteCooldown), biteCooldown);
                }
                else
                {
                    Debug.Log("Il pirata NON può essere infettato (sta inseguendo)");
                }
            }
            else if (hit.collider.CompareTag("Cheese"))
            {
                CheesePowerUp cheese = hit.collider.GetComponent<CheesePowerUp>();
                if (cheese != null)
                {
                    cheese.ActivatePowerUp(this);
                }
            }
            else
            {
                Debug.Log("Oggetto davanti non è un target valido!");
            }
        }
        else
        {
            Debug.Log("Nessun bersaglio davanti al topo!");
        }
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


    // 👇 Nuovo: rimuove il pirata morto
    private void RemoveDeadPirate(PirateController deadPirate)
    {
        if (infectedPirates.Contains(deadPirate.transform))
        {
            infectedPirates.Remove(deadPirate.transform);
        }
    }
}
