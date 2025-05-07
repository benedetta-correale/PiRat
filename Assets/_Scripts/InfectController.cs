using UnityEngine;

public class FollowCharacter : MonoBehaviour
{
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float minDistance = 0.5f; // Distanza minima dal player
    [SerializeField] private float viewAngle = 90f; // Angolo del cono visivo
    [SerializeField] private float viewDistance = 10f; // Distanza massima di vista

    [SerializeField] private SkillCheck _skillCheck;

    private GameObject _mainCharacter;

    void Start()
    {
        _mainCharacter = GameObject.FindGameObjectWithTag("Player");
        
        if (_mainCharacter == null)
        {
            Debug.LogError("Main character not found!");
            return;
        }
    }

    void Update()
    {
        if (_mainCharacter != null)
        {
            Vector3 direction = _mainCharacter.transform.position - transform.position;
            // calcola la distanza tra il nemico e il player
            float distance = direction.magnitude;

            // Verifica se il target è nel cono visivo
            bool isInViewCone = IsInViewCone(direction, distance);

            if (isInViewCone && distance > minDistance) 
            {
                if (Input.GetKeyDown(KeyCode.I)) {

                    _skillCheck.StartSkillCheck();


                    
                }

                

                
                
            }
        }
        
    }

    private bool IsInViewCone(Vector3 directionToTarget, float distance)
    {
        if (distance > viewDistance) return false;

        //controllo che il target sia all'interno del cono visivo
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= viewAngle * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftDirection = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        
        // Disegna le linee del cono
        Gizmos.DrawRay(transform.position, leftDirection * viewDistance);
        Gizmos.DrawRay(transform.position, rightDirection * viewDistance);
        
        // Disegna la sfera del raggio di vista
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        
        // Opzionale: disegna più linee per rendere il cono più visibile
        int numLines = 10;
        for (int i = 0; i < numLines; i++)
        {
            float angle = (-viewAngle * 0.5f) + ((viewAngle / (numLines - 1)) * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, direction * viewDistance);
        }
    }
    private void startFollowing(){
        // Pop - UP il topo è stato rilevato
        // il pirata dopo 5 secondi si gira verso il topo
        //e inizia ad inseguirlo 

        //la sua velocità di movimento è 3% > di quella del topo
        
        // quando lo insegue c'è comunque un angolo mas


    }

    
}
