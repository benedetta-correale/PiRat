using UnityEngine;

using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Slider slider;

    public void UpdateHealthBar(int current, int max)
    {
        slider.value = (float)current / max * 100f;
    }
}
