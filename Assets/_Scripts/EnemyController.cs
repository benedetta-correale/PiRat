using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float _followSpeed = 3f;
    [SerializeField] private float _viewAngle = 90f; // Angolo del cono visivo
    [SerializeField] private float _viewDistance = 10f; // Distanza massima di vista
    [SerializeField] private float _rayAttachment = 3f; // distanza del raggio di attaccamento
    [SerializeField] private Material visionConeMaterial; // Aggiungi questo campo
    public bool startFollowing; // Fixed incomplete boolean declaration
    private GameObject _mainCharacter;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Start()
    {
        _mainCharacter = GameObject.FindGameObjectWithTag("Player");
        
        if (_mainCharacter == null)
        {
            Debug.LogError("Main character not found!");
            return;
        }

        // Inizializza il cono di visione
        GameObject visionCone = new GameObject("VisionCone");
        visionCone.transform.parent = transform;
        visionCone.transform.localPosition = Vector3.zero;
        
        meshFilter = visionCone.AddComponent<MeshFilter>();
        meshRenderer = visionCone.AddComponent<MeshRenderer>();
        meshRenderer.material = visionConeMaterial;
        
        UpdateVisionCone();
    }

    void Update()
    {
        if (_mainCharacter != null)
        {
            Vector3 direction = _mainCharacter.transform.position - transform.position;
            float distance = direction.magnitude;

            bool isInViewCone = IsInViewCone(direction, distance);

            if (isInViewCone) 
            {
                startFollowing = true;   
            }
        }

        // Fixed if statement syntax
        if (startFollowing)
        {
            StartFollowing();
        }

        UpdateVisionCone(); // Aggiorna il cono di visione ogni frame
    }

   private void StartFollowing()

    {
        Vector3 direction = _mainCharacter.transform.position - transform.position;
        float distance = direction.magnitude;

        if (distance <= _rayAttachment)
        {
            Debug.Log("Il pirata ha raggiunto il topo!");

            // LOGICA PER QUANDO IL PIRATA RAGGIUNGE IL TOPO
            startFollowing = false; //DA MODIFICARE PERCHE' IL TOPO POTREBBE INFETTARE ANCORA IL PIRATA
            return;
        }

        //ROTAZIONE GRADUALE
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);

        // MOVIMENTO IN AVANTI
        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * _followSpeed * Time.deltaTime;
    }


    public bool IsInViewCone(Vector3 directionToTarget, float distance)
    {
        
        if (distance > _viewDistance) return false;

        float angle = Vector3.Angle(transform.forward, directionToTarget);
        return angle <= _viewAngle * 0.5f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftDirection = Quaternion.Euler(0, -_viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDirection = Quaternion.Euler(0, _viewAngle * 0.5f, 0) * transform.forward;
        
        Gizmos.DrawRay(transform.position, leftDirection * _viewDistance);
        Gizmos.DrawRay(transform.position, rightDirection * _viewDistance);
        
        Gizmos.DrawWireSphere(transform.position, _viewDistance);
        
        int numLines = 10;
        for (int i = 0; i < numLines; i++)
        {
            float angle = (-_viewAngle * 0.5f) + ((_viewAngle / (numLines - 1)) * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            Gizmos.DrawRay(transform.position, direction * _viewDistance);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _rayAttachment);
    }

    //metodo per disegnare il cono visivo
    private void UpdateVisionCone()
    {
        int segments = 32;
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        float angleStep = _viewAngle / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (-_viewAngle / 2) + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            vertices[i + 1] = direction * _viewDistance;
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
    }
}
