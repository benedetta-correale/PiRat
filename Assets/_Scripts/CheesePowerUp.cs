using System.Collections;
using UnityEngine;

public enum CheesePowerUpType { Heal, SpeedBoost, DamageBoost, PoisonLeak }

public class CheesePowerUp : MonoBehaviour
{
    [Header("Power-up Settings")]
    public CheesePowerUpType powerUpType;
    public int healAmount = 20;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 5f;
    public int extraDamage = 10;

    [HideInInspector] public bool wasNear = false;

    [Header("Prefabs & VFX")]
    public GameObject poisonPuddlePrefab;
    
    [SerializeField] private GameObject VFXPrefab;

    


    [Header("Outline & Trigger")]
    private Material _defaultMaterial;
    private bool outlineActive = false;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private SphereCollider triggerCollider;
    [SerializeField] private string playerTag = "Player";

    private Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null)
        {
            _defaultMaterial = _renderer.material;
        }
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }
    }

    public void EnableOutline(bool enable)
    {
        if (_renderer == null || outlineMaterial == null || triggerCollider == null) return;

        if (outlineActive == enable) return;
        outlineActive = enable;

        if (enable)
        {
            Material[] newMats = new Material[2];
            newMats[0] = _renderer.materials[0];
            newMats[1] = outlineMaterial;
            _renderer.materials = newMats;

            triggerCollider.enabled = true; // ✅ attiva il collider
        }
        else
        {
            Material[] newMats = new Material[1];
            newMats[0] = _renderer.materials[0];
            _renderer.materials = newMats;

            triggerCollider.enabled = false; // ✅ spegne anche il collider
            wasNear = false; // ← serve per bloccare riattivazioni immediate

        }
    }

    public void ActivatePowerUp(RatInteractionManager rat)
    {
        Debug.Log($"ActivatePowerUp: {powerUpType}");
        var bonusMalus = rat.GetComponent<BonusMalus>();
        bool consumed = false;

        switch (powerUpType)
        {
            case CheesePowerUpType.Heal:
                if (bonusMalus != null && bonusMalus.currentHealth < bonusMalus.maxHealth)
                {
                    bonusMalus.Heal(healAmount);
                    consumed = true;
                    StartCoroutine(EnableHealVFXAfterDestroy(rat.transform, 1.7f));
                }
                break;

            case CheesePowerUpType.SpeedBoost:
                var ratInput = rat.GetComponent<RatInputHandler>();
                if (ratInput != null)
                {
                    ratInput.StartCoroutine(ratInput.SpeedBoostRoutine(speedMultiplier, speedDuration));
                    consumed = true;
                    StartCoroutine(EnableSpeedVFXAfterDestroy(ratInput, 1.7f));
                }
                break;

            case CheesePowerUpType.DamageBoost:
                consumed = true;
                StartCoroutine(EnableDamageVFXAfterDestroy(rat, 1.7f));
                break;

            case CheesePowerUpType.PoisonLeak:
                consumed = true;
                StartCoroutine(EnablePeeVFXAfterDestroy(rat, 1.7f));
                break;
        }

        if (consumed)
            StartCoroutine(DestroyAfterDelay(1.7f));
    }

    private IEnumerator DestroyAfterDelay(float totalDelay)
    {
        yield return new WaitForSeconds(1f);

        if (_renderer != null)
            _renderer.enabled = false;

        // NON disabilitare qui il trigger
        // if (triggerCollider != null)
        //     triggerCollider.enabled = false;

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
            mainCollider.enabled = false;

        float remainingDelay = Mathf.Max(0f, totalDelay - 1f);
        yield return new WaitForSeconds(remainingDelay);

        //if (triggerCollider != null)
          //  triggerCollider.enabled = false; // spegne il trigger definitivamente per sicurezza

        // forza sempre spegnimento dell'outline
        EnableOutline(false);

        Destroy(gameObject);
    }

    private IEnumerator EnableSpeedVFXAfterDestroy(RatInputHandler ratInput, float delay)
    {
        yield return new WaitForSeconds(delay);
        ratInput.SetSpeedVFX(VFXPrefab);
    }

    private IEnumerator EnableDamageVFXAfterDestroy(RatInteractionManager rat, float delay)
    {
        yield return new WaitForSeconds(delay);
        rat.ActivateDamageBoost(extraDamage, VFXPrefab);
    }

    private IEnumerator EnablePeeVFXAfterDestroy(RatInteractionManager rat, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Solo segnala che può pisciare + VFX
        rat.PreparePoisonLeak(poisonPuddlePrefab, VFXPrefab);

        // Se il formaggio ha un TrapConfig, passa le sue trappole
        var trapConfig = GetComponent<TrapConfig>();
        if (trapConfig != null && trapConfig.enableTrapFromPuddle)
        {
            rat.ConfigurePuddleTrap(trapConfig.trapPrefabs);
        }
    }




    private IEnumerator EnableHealVFXAfterDestroy(Transform ratTransform, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (VFXPrefab != null)
        {
            var vfx = Instantiate(VFXPrefab, ratTransform.position, Quaternion.identity, ratTransform);
            vfx.transform.localPosition = Vector3.zero;
            Destroy(vfx, 2f);
        }
    }
    
     private void OnTriggerExit(Collider other)
    {
        if (outlineActive && other.CompareTag(playerTag))
        {
            EnableOutline(false); 
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            powerUpType.ToString()
        );
    }
#endif
}