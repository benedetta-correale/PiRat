using UnityEngine;
using UnityEngine.Events;

public class BonusMalus : MonoBehaviour
{
    [Header("Parametri Vita")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Eventi")]
    public UnityEvent<int, int> onHealthChanged; // (current, max)
    public UnityEvent onDeath;

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
        // Qui puoi disattivare movimento, attivare animazioni, ecc.
    }

    private void NotifyHealthChange()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
