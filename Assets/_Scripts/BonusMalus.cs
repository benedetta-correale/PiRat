using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BonusMalus : MonoBehaviour
{
    [Header("Parametri Vita")]
    public int maxHealth = 100; //max vita topo 
    public int currentHealth; // vita corrente top

    [Header ("UI Vita topo")]
    [SerializeField] private Slider _healthbar;

    [Header("Eventi")]
    public UnityEvent<int, int> onHealthChanged; // (current, max)
    public UnityEvent onDeath;

    void Awake()
    {
        if (!_healthbar)
        {
            Debug.LogWarning("HealthBar not found");
            return;
        }
        
        _healthbar.minValue = 0f;
        _healthbar.maxValue = 100f;
        currentHealth = maxHealth;
        _healthbar.value = currentHealth;
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

    private void OnColliderEnter(Collider other)


{
        Debug.Log("sta per essere colpito");
    if (other.CompareTag("Pirate"))
        {
            PirateController pirate = other.GetComponent<PirateController>();
            if (pirate != null)

            {
                int damage = pirate.attackDamage;
                TakeDamage(damage); // 
                Debug.Log($"Il topo è stato colpito da {pirate.name} per {damage} danni.");
            }
        }
}


    private void Die()
    {
        Debug.Log("Il topo � morto!");
        onDeath?.Invoke();
        // Qui puoi disattivare movimento, attivare animazioni, ecc.
    }

    private void NotifyHealthChange()
    {
        onHealthChanged?.Invoke(currentHealth, maxHealth);
        if (_healthbar != null)
        {
            _healthbar.value = currentHealth;
        }
    }
}
