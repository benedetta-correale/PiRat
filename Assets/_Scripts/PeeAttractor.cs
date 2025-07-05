using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class PeeAttractor : MonoBehaviour
{
    [Header("Attrazione pirati")]
    [SerializeField] private float attractionRadius = 5f;

    [HideInInspector] public bool spawnTrapOnFirstInfection = false;
    [HideInInspector] public GameObject[] possibleTraps;

    private SphereCollider attractionCollider;
    private HashSet<PirateController> attractedPirates = new HashSet<PirateController>();
    private PirateController firstToReach = null;
    private bool trapSpawned = false;

    private void Awake()
    {
        attractionCollider = gameObject.AddComponent<SphereCollider>();
        attractionCollider.isTrigger = true;
        attractionCollider.radius = attractionRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pirate")) return;

        PirateController pirate = other.GetComponentInParent<PirateController>();
        if (pirate != null
            && !pirate.infected
            && !attractedPirates.Contains(pirate)
            && (pirate.CurrentState == "Patrol" || pirate.CurrentState == "Suspicious"))

        {
            attractedPirates.Add(pirate);
            NavMeshAgent agent = pirate.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.SetDestination(transform.position);
                Debug.Log($"🧲 Pirate {pirate.name} attratto dalla pozza");
            }
        }
    }

    public void OnPuddleConsumed(PirateController consumer)
    {
        if (firstToReach != null) return; // già gestito

        firstToReach = consumer;

        if (spawnTrapOnFirstInfection
            && !trapSpawned
            && possibleTraps.Length > 0
            && (consumer.CurrentState == "Patrol" || consumer.CurrentState == "Suspicious"))

        {
            GameObject selectedTrap = possibleTraps[Random.Range(0, possibleTraps.Length)];
            Instantiate(selectedTrap, transform.position, Quaternion.identity);
            trapSpawned = true;
            Debug.Log("🪤 Trappola istanziata casualmente!");
        }

        foreach (var pirate in attractedPirates)
        {
            if (pirate != null && pirate != consumer && !pirate.infected)
            {
                pirate.SendMessage("EnterPatrol", SendMessageOptions.DontRequireReceiver);
                Debug.Log($"🔙 Pirate {pirate.name} torna in patrol");
            }
        }

        attractedPirates.Clear();
    }

    public void SetTrapMechanic(bool enable, GameObject[] traps)
    {
        spawnTrapOnFirstInfection = enable;
        possibleTraps = traps;
    }
    // Questo riceve il SendMessage da PirateController
    private void CancelAttractionFromPuddle(PirateController pirate)
    {
        CancelAttraction(pirate);
    }

    public void CancelAttraction(PirateController pirate)
    {
        if (attractedPirates.Contains(pirate))
        {
            attractedPirates.Remove(pirate);
            Debug.Log($"❌ Pirate {pirate.name} ha abbandonato l'attrazione verso la pipì");
        }
    }

    public bool IsAttracted(PirateController pirate)
    {
        return attractedPirates.Contains(pirate);
    }


}

