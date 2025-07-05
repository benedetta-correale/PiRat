using UnityEngine;
using System.Collections;

public class PoisonPuddle : MonoBehaviour
{
    private bool used = false;
    Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;

        // 👇 DEBUG: Verifica setup collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("PoisonPuddle: NESSUN COLLIDER trovato! Aggiungine uno e impostalo come Trigger");
        }
        else
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning("PoisonPuddle: Collider non è un Trigger! Impostalo come Trigger");
                col.isTrigger = true; // Fix automatico
            }
            Debug.Log($"PoisonPuddle: Collider OK - Tipo: {col.GetType().Name}, IsTrigger: {col.isTrigger}");
        }
    }

    void Start()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(ScaleUp());
    }

    IEnumerator ScaleUp()
    {
        float t = 0f;
        float duration = 0.5f;
        while (t < duration)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;
        Debug.Log("PoisonPuddle: Scale completato, pronto per trigger");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔥 TRIGGER ENTER con: {other.name} (Tag: {other.tag})");

        if (used)
        {
            Debug.Log("❌ Pozza già usata, ignoro");
            return;
        }

        if (other.CompareTag("Pirate"))
        {
            PirateController pirate = other.GetComponentInParent<PirateController>();
            if (pirate == null)
            {
                Debug.LogError($"❌ PirateController NON trovato su {other.name}!");
                return;
            }

            Debug.Log("✅ PIRATA RILEVATO! Inizio infezione...");
            used = true;

            // Registra il pirata come infetto
            RatInteractionManager ratManager = FindFirstObjectByType<RatInteractionManager>();
            if (ratManager != null)
            {
                ratManager.RegisterInfectedPirate(pirate);
                Debug.Log("✅ Pirata registrato come infetto");
            }
            else
            {
                Debug.LogError("❌ RatInteractionManager non trovato!");
            }

            StartCoroutine(ApplyPoison(pirate));
        }
        else
        {
            Debug.Log($"❌ Non è un pirata: {other.name} (Tag: {other.tag})");
        }
    }


    private void OnTriggerStay(Collider other)
    {
        // Debug per vedere se il pirata rimane nel trigger
        if (other.CompareTag("Pirate") && !used)
        {
            Debug.Log($"🔄 Pirata {other.name} ancora nel trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pirate"))
        {
            Debug.Log($"🚪 Pirata {other.name} uscito dal trigger");
        }
    }

    IEnumerator ApplyPoison(PirateController pirate)
    {
        int totalDamage = 30;
        int ticks = 30;
        for (int i = 0; i < ticks; i++)
        {
            if (pirate == null) break;
            pirate.TakeDamage(1);
            yield return new WaitForSeconds(1f);
        }

        yield return new WaitForSeconds(0.5f); // breve pausa

        // ✅ Se il pirata era attratto da questa puddle, notifica per trappola
        PeeAttractor attractor = GetComponent<PeeAttractor>();
        if (attractor != null && attractor.IsAttracted(pirate))
        {
            attractor.OnPuddleConsumed(pirate);
        }

        Destroy(gameObject);
    }




    IEnumerator DestroyAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Debug.Log("💥 Distruggo PoisonPuddle");
        Destroy(gameObject);
    }
}