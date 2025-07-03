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

    [Header("Prefabs & VFX")]
    public GameObject poisonPuddlePrefab;
    [SerializeField] private GameObject VFXPrefab;

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
            StartCoroutine(HideAndDestroy(1.7f));
    }

    private void SpawnTemporaryVFX(Transform ratTransform)
    {
        if (VFXPrefab == null) return;

        var vfx = Instantiate(VFXPrefab, ratTransform.position, Quaternion.identity, ratTransform);
        vfx.transform.localPosition = Vector3.zero;
        Destroy(vfx, 2f);
    }

    private IEnumerator HideAndDestroy(float delay)
    {
        yield return new WaitForSeconds(1f);
        if (_renderer != null) _renderer.enabled = false;
        if (triggerCollider != null) triggerCollider.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(delay - 1f);
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
        rat.EnablePoisonLeak(poisonPuddlePrefab, VFXPrefab);
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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, powerUpType.ToString());
    }
#endif
}