using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RatInteractionManager : MonoBehaviour
{
    [SerializeField] private float _ratRay = 5f;

    [Header("Effetti dell' attacco")]
    public bool biting = false;
    public InfectionSkillCheckUI skillCheck;
    public PirateController enemyController;

    private CameraControlManager cameraControlManager;

    // 👇 Nuovo: lista dei pirati infettati
    public List<Transform> infectedPirates = new List<Transform>();


    void Start()
    {
        cameraControlManager = FindObjectOfType<CameraControlManager>();
    }

    void Update()
    {
        // 👇 Mostra cerchio di rilevamento
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

        infectPirate();

        // 👇 Nuovo: premi 1-9 per entrare nei pirati infettati
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

    private void infectPirate()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _ratRay);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Pirate"))
            {
                enemyController = hit.GetComponent<PirateController>();

                if (Input.GetKeyDown(KeyCode.I))
                {
                    Debug.Log("Pirata infettato");

                    if (enemyController != null)
                    {
                        biting = true;
                        enemyController.TakeDamage();

                        // 👇 Nuovo: registra il pirata infettato
                        Infect(enemyController);
                    }
                    else
                    {
                        Debug.Log("Il pirata NON può essere infettato (sta inseguendo)");
                    }
                }
                break;
            }
        }
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

    // 👇 Nuovo: rimuove il pirata morto
    private void RemoveDeadPirate(PirateController deadPirate)
    {
        if (infectedPirates.Contains(deadPirate.transform))
        {
            infectedPirates.Remove(deadPirate.transform);
        }
    }
}
