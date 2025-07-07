using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class BonusMalus : MonoBehaviour
{
    [Header("Parametri Vita")]
    public int maxHealth = 100; //max vita topo 
    public int currentHealth; // vita corrente top

    [Header("Eventi")]
    public UnityEvent<int, int> onHealthChanged; // (current, max)
    public UnityEvent onDeath;

    //[SerializeField] private Animator animator; // Animatore per le animazioni del topo
    [SerializeField] private GameObject VFXPrefab;
    public RatInteractionManager rat;
    [SerializeField]
    private PossessionManager pm;
    void Awake()
    {
        currentHealth = maxHealth;
        
        NotifyHealthChange();
    }

    public void TakeDamage(int amount)
    {
      
        if (pm != null)
            pm.OnAttacked();
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        StartCoroutine(EnableVFXAfterDestroy(rat.transform, 1.7f));
        NotifyHealthChange();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator EnableVFXAfterDestroy(Transform ratTransform, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (VFXPrefab != null)
        {
            var vfx = Instantiate(VFXPrefab, ratTransform.position, Quaternion.identity, ratTransform);
            vfx.transform.localPosition = Vector3.zero;
            Destroy(vfx, 2f);
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        NotifyHealthChange();
    }

    private void Die()
    {
        Debug.Log("Il topo è morto!");
        onDeath?.Invoke();
        //animator.SetTrigger("Die"); // Attiva l'animazione di morte
    }

    private void NotifyHealthChange()
    {
        Debug.Log($"NotifyHealthChange fired: {currentHealth}/{maxHealth}");
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
