using UnityEngine;
using UnityEngine.VFX;

public class QuickTimeVFXManager : MonoBehaviour
{
    public VisualEffect biteEffect;

    public void PlayBiteVFX()
    {
        if (biteEffect == null) return;

        biteEffect.gameObject.SetActive(false); // reset visivo
        biteEffect.gameObject.SetActive(true);  // riattiva
        biteEffect.Play();
    }
}
