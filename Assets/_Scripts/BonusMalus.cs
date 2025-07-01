using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BonusMalus : MonoBehaviour
{
    [Header("Parametri Vita")]
    public int maxHealth = 100; //max vita topo 
    public int currentHealth; // vita corrente top

    [Header("Eventi")]
    public UnityEvent<int, int> onHealthChanged; // (current, max)
    public UnityEvent onDeath;

    //[SerializeField] private Animator animator; // Animatore per le animazioni del topo

    void Awake()
    {
        currentHealth = maxHealth;
        
        NotifyHealthChange();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        NotifyHealthChange();

        if (currentHealth <= 0)
        {
            Die();
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
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
