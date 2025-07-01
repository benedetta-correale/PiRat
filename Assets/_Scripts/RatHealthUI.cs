using UnityEngine;
using UnityEngine.UI;

public class RatHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BonusMalus ratHealth;
    [SerializeField] private Image healthBarImage;

    private void OnEnable()
    {
        if (ratHealth != null)
            ratHealth.onHealthChanged.AddListener(UpdateHealthBar);
    }

    private void OnDisable()
    {
        if (ratHealth != null)
            ratHealth.onHealthChanged.RemoveListener(UpdateHealthBar);
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (healthBarImage != null)
        {
            float fill = (float)current / max;
            healthBarImage.fillAmount = fill;
        }
    }
}
