using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public enum TrapType { Spring, Glue, Slide }

public class Trap : MonoBehaviour
{
    [Header("Tipo di trappola")]
    [SerializeField] private TrapType trapType;

    [Header("Valori configurabili")]
    [SerializeField] private int springDamage = 30;
    [SerializeField] private float glueDuration = 2f;
    [SerializeField] private float slideForce = 10f;

    [SerializeField] private float springCooldown = 2f;
    private bool springReady = true;

    [Header("VFX")]
    [Tooltip("Assegna qui il prefab VFX specifico per questa trappola")]
    [SerializeField] private GameObject trapVFXPrefab;

    [SerializeField] private float requiredWiggle = 2f; // quanta "energia" serve per liberarsi
    [SerializeField] private float wiggleDecay = 0.5f;  // quanto si scarica nel tempo se non ti dimeni
    [SerializeField] private float wiggleStrength = 0.05f;
    [SerializeField] private float wiggleSpeed = 20f;
    [Header("Replaceable Spring")]
    [SerializeField] private bool isReplaceable = false;
    [SerializeField] private MeshRenderer originalRenderer;
    [SerializeField] private MeshRenderer usedRenderer;
    private bool trapUsed = false;

    private Transform stuckModel; // riferimento al modello visivo del topo
    private Vector3 initialModelLocalPos;

    private bool isStuck = false;
    private float wiggleAmount = 0f;
    private RatInputHandler stuckPlayer = null;

    private GameObject glueVFXInstance;

    private void OnTriggerEnter(Collider other)
    {
        // Reset della trappola spring da parte di un pirata
        if (isReplaceable && trapUsed && other.CompareTag("Pirate"))
        {
            PirateController pc = other.GetComponent<PirateController>();
            if (pc != null)
            {
                string state = pc.CurrentState;
                if (state != "Chasing" && state != "Attacking")
                {
                    NavMeshAgent pirateAgent = other.GetComponent<NavMeshAgent>();
                    StartCoroutine(ResetTrap(pirateAgent));
                }
            }
            return;
        }



        if (!other.CompareTag("Player")) return;

        RatInteractionManager rim = other.GetComponent<RatInteractionManager>();
        if (rim != null)
        {
            Debug.Log($"TRAP: RIM trovato, isBackflipping = {rim.isBackflipping}");

            // Controlla anche se l'animatore sta facendo un backflip
            Animator ratAnimator = other.GetComponent<Animator>();
            bool isBackflipAnimation = false;
            if (ratAnimator != null)
            {
                AnimatorStateInfo stateInfo = ratAnimator.GetCurrentAnimatorStateInfo(0);
                isBackflipAnimation = stateInfo.IsName("Backflip") || stateInfo.IsTag("Backflip");
                Debug.Log($"TRAP: Animazione backflip attiva = {isBackflipAnimation}");
            }

            if (rim.isBackflipping || isBackflipAnimation)
            {
                Debug.Log("Backflip attivo: aspetto che finisca prima di attivare la trappola.");

                // Disabilita il trigger completamente per evitare interferenze
                Collider trapCollider = GetComponent<Collider>();
                if (trapCollider != null)
                {
                    trapCollider.enabled = false;
                }

                // Usa coroutine con delay invece del controllo nell'Update
                StartCoroutine(WaitForBackflipEnd(rim));
                return;
            }
            else
            {
                Debug.Log("TRAP: isBackflipping � FALSE, procedo normalmente");
            }
        }
        else
        {
            Debug.Log("TRAP: RIM � NULL!");
        }

        // Processa normalmente la trappola
        ProcessTrap(other);
    }

    private void Awake()
    {
        // se non assegnati in inspector, cerchiamo automaticamente
        if (originalRenderer == null)
            originalRenderer = GetComponentInChildren<MeshRenderer>();
        if (usedRenderer != null)
            usedRenderer.enabled = false;

    }
    private IEnumerator WaitForBackflipEnd(RatInteractionManager rim)
    {
        Animator ratAnimator = rim.GetComponent<Animator>();

        // Aspetta che ENTRAMBI isBackflipping sia false E l'animazione sia finita
        while (rim != null && (rim.isBackflipping || IsBackflipAnimationActive(ratAnimator)))
        {
            Debug.Log($"WAITING: isBackflipping = {rim.isBackflipping}, Animation = {IsBackflipAnimationActive(ratAnimator)}");
            yield return new WaitForFixedUpdate(); // Usa FixedUpdate per physics
        }

        yield return new WaitForFixedUpdate(); // Un frame extra per sicurezza
        Debug.Log("BACKFLIP COMPLETAMENTE TERMINATO");

        // Riabilita il collider
        Collider trapCollider = GetComponent<Collider>();
        if (trapCollider != null)
        {
            trapCollider.enabled = true;
        }

        // Controlla se il rat � ancora sopra la trappola
        if (rim != null)
        {
            Collider ratCollider = rim.GetComponent<Collider>();
            if (ratCollider != null && trapCollider.bounds.Intersects(ratCollider.bounds))
            {
                Debug.Log("Rat ancora sopra la trappola, attivo l'effetto.");
                ProcessTrap(ratCollider);
            }
            else
            {
                Debug.Log("Rat non pi� sopra la trappola, nessun effetto.");
            }
        }
    }

    private bool IsBackflipAnimationActive(Animator animator)
    {
        if (animator == null) return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool isBackflipAnim = stateInfo.IsName("Backflip") || stateInfo.IsTag("Backflip");

        // Controlla anche se siamo in transizione verso un altro stato
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (nextState.IsName("Backflip") || nextState.IsTag("Backflip"))
                isBackflipAnim = true;
        }

        return isBackflipAnim;
    }

    private void Update()
    {
        // Gestione del wiggle per la trappola di colla
        if (isStuck && stuckPlayer != null)
        {
            Vector2 input = stuckPlayer.GetMoveInputRaw();
            //effetto visivo del wiggle
            Vector3 wiggleOffset = new Vector3(input.x, 0, input.y) * Mathf.Sin(Time.time * wiggleSpeed) * wiggleStrength;
            stuckModel.localPosition = initialModelLocalPos + wiggleOffset;

            wiggleAmount += input.magnitude * Time.deltaTime * 5f; // aumenta "barra di fuga"
            wiggleAmount -= wiggleDecay * Time.deltaTime;          // decadenza
            wiggleAmount = Mathf.Clamp(wiggleAmount, 0f, requiredWiggle);

            if (wiggleAmount >= requiredWiggle)
            {
                // Riattiva animator
                Animator anim = stuckPlayer.GetComponent<Animator>();
                if (anim != null) anim.enabled = true;

                // Riattiva controllo
                stuckPlayer.enabled = true;

                if (stuckModel != null)
                    stuckModel.localPosition = initialModelLocalPos;

                stuckModel = null;

                isStuck = false;
                stuckPlayer = null;

                if (glueVFXInstance != null)
                {
                    Destroy(glueVFXInstance);
                    glueVFXInstance = null;
                }
            }
        }
    }

    private void ProcessTrap(Collider other)
    {
        switch (trapType)
        {
            case TrapType.Spring:
                if (!springReady) break;

                if (trapVFXPrefab != null)
                    SpawnVFX(trapVFXPrefab, other.transform);

                var hp = other.GetComponent<BonusMalus>();
                if (hp != null) hp.TakeDamage(springDamage);

                springReady = false;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;

                if (isReplaceable)
                {
                    // mostro il modello usato, nascondo quello originale
                    if (originalRenderer != null) originalRenderer.enabled = false;
                    if (usedRenderer != null) usedRenderer.enabled = true;
                    trapUsed = true;
                }
                else
                {
                    StartCoroutine(HideAndDestroy());
                }

                break;

            case TrapType.Glue:
                var pc = other.GetComponent<RatInputHandler>();
                if (pc != null && !isStuck)
                {
                    isStuck = true;
                    stuckPlayer = pc;

                    // Salva riferimento al modello
                    stuckModel = pc.transform; // <-- metti qui il nome esatto del figlio con la mesh
                    if (stuckModel != null)
                        initialModelLocalPos = stuckModel.localPosition;

                    // Blocca input
                    pc.enabled = false;

                    // Blocca animator
                    Animator anim = pc.GetComponent<Animator>();
                    if (anim != null) anim.enabled = false;

                    wiggleAmount = 0f;

                    if (trapVFXPrefab != null)
                    {
                        glueVFXInstance = Instantiate(trapVFXPrefab, other.transform.position, Quaternion.identity, other.transform);
                    }
                }
                break;

            case TrapType.Slide:
                if (trapVFXPrefab != null)
                    SpawnVFX(trapVFXPrefab, other.transform);

                var rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = other.transform.forward;
                    rb.AddForce(direction * slideForce, ForceMode.Impulse);
                }
                break;
        }
    }

    private void SpawnVFX(GameObject prefab, Transform parent)
    {
        // Istanzia il VFX come figlio del ratto, così segue sempre la sua posizione
        var vfx = Instantiate(prefab, parent.position, Quaternion.identity, parent);
        Destroy(vfx, 2f);
    }

    private void OnTriggerExit(Collider other)
    {
        // Non pi� necessario con la versione coroutine
        // Il collider viene gestito automaticamente nella WaitForBackflipEnd
    }

    private IEnumerator GlueEffect(RatInputHandler pc)
    {
        pc.enabled = false;
        yield return new WaitForSeconds(glueDuration);
        pc.enabled = true;
    }

    private IEnumerator SpringReset()
    {
        yield return new WaitForSeconds(springCooldown);
        springReady = true;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private IEnumerator HideAndDestroy()
    {
        yield return new WaitForSeconds(1f); // aspetto prima di nascondere
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;

        yield return new WaitForSeconds(1f); // altro secondo prima di distruggere
        Destroy(gameObject);
    }

    private IEnumerator ResetTrap(NavMeshAgent pirateAgent)
    {
        // il pirata si ferma per 1 secondo
        if (pirateAgent != null) pirateAgent.isStopped = true;
        yield return new WaitForSeconds(1f);
        if (pirateAgent != null) pirateAgent.isStopped = false;

        // ripristino mesh e collider
        if (originalRenderer != null) originalRenderer.enabled = true;
        if (usedRenderer != null) usedRenderer.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        springReady = true;
        trapUsed = false;
    }


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            trapType.ToString()
        );
    }
#endif
}