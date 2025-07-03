using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CheesePowerUpType { Heal, SpeedBoost, DamageBoost, PoisonLeak }

public class CheesePowerUp : MonoBehaviour
{
    [Header("Power-up Settings")]
    public CheesePowerUpType powerUpType;
    public int healAmount = 20;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 5f;
    public int extraDamage = 10;

    [Header("Prefabs & VFX")]
    public GameObject poisonPuddlePrefab;
    [SerializeField] private GameObject healVFXPrefab;  // Prefab particelle

    [Header("Outline & Trigger")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private SphereCollider triggerCollider;
    [SerializeField] private string playerTag = "Player";

    private Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }
    }

    public void EnableOutline(bool enable)
    {
        if (_renderer == null || outlineMaterial == null || triggerCollider == null) return;
        if (enable)
        {
            _renderer.materials = new Material[] { _renderer.materials[0], outlineMaterial };
            triggerCollider.enabled = true;
        }
        else
        {
            _renderer.materials = new Material[] { _renderer.materials[0] };
            triggerCollider.enabled = false;
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
                    Debug.Log($"Topo curato di {healAmount}");
                    consumed = true;
                }
                break;

            case CheesePowerUpType.SpeedBoost:
                var ratInput = rat.GetComponent<RatInputHandler>();
                if (ratInput != null)
                {
                    ratInput.StartCoroutine(ratInput.SpeedBoostRoutine(speedMultiplier, speedDuration));
                    Debug.Log($"SpeedBoost: x{speedMultiplier} per {speedDuration}s");
                    consumed = true;
                }
                break;

            case CheesePowerUpType.DamageBoost:
                rat.ActivateDamageBoost(extraDamage);
                Debug.Log($"DamageBoost: +{extraDamage}");
                consumed = true;
                break;

            case CheesePowerUpType.PoisonLeak:
                rat.EnablePoisonLeak(poisonPuddlePrefab);
                Debug.Log("PoisonLeak abilitato");
                consumed = true;
                break;
        }

        if (consumed)
        {
            // 1) distruggi il formaggio con delay, e contemporaneamente 
            //    fai spawn del VFX al momento della distruzione
            StartCoroutine(DestroyAfterDelay(rat.transform, 1.7f));
        }
    }

    /// <summary>
    /// Aspetta totalDelay, nasconde il formaggio, spawna VFX come figlio di ratTransform, quindi distrugge il formaggio.
    /// </summary>
    private IEnumerator DestroyAfterDelay(Transform ratTransform, float totalDelay)
    {
        // parte 1: animazione formaggio (1s)
        yield return new WaitForSeconds(1f);

        if (_renderer != null) _renderer.enabled = false;
        if (triggerCollider != null) triggerCollider.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // parte 2: aspetta il resto
        yield return new WaitForSeconds(totalDelay - 1f);

        // al momento della distruzione, spawn VFX come figlio del topo
        if (healVFXPrefab != null && ratTransform != null)
        {
            var vfx = Instantiate(
                healVFXPrefab,
                ratTransform.position,
                Quaternion.identity,
                ratTransform    // <--- parent impostato al ratto
            );
            // opzionale: reset locale
            vfx.transform.localPosition = Vector3.zero;
            Destroy(vfx, 2f);
        }

        Destroy(gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (outlineMaterial != null && other.CompareTag(playerTag))
            EnableOutline(false);
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