using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum CheesePowerUpType { Heal, SpeedBoost, DamageBoost, PoisonLeak }

public class CheesePowerUp : MonoBehaviour
{
    public CheesePowerUpType powerUpType;
    public int healAmount = 20;
    public float speedMultiplier = 1.5f;
    public float speedDuration = 5f;
    public int extraDamage = 10;

    public GameObject poisonPuddlePrefab;
    private Renderer _renderer;
    private Material _defaultMaterial;
    [SerializeField] private Material outlineMaterial;
    private bool outlineActive = false;

    [SerializeField] private SphereCollider triggerCollider;
    [SerializeField] private string playerTag = "Player"; // puoi cambiare se serve


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
        }
    }



    public void ActivatePowerUp(RatInteractionManager rat)
    {
        Debug.Log("ActivatePowerUp chiamato. Tipo: " + powerUpType);
        var bonusMalus = rat.GetComponent<BonusMalus>();
        bool consumed = false;
        switch (powerUpType)
        {
            case CheesePowerUpType.Heal:
                if (bonusMalus != null && bonusMalus.currentHealth < bonusMalus.maxHealth)
                {
                    bonusMalus.Heal(healAmount);
                    Debug.Log("Topo curato di " + healAmount);
                    consumed = true;
                }
                else
                {
                    Debug.Log("Topo già alla salute massima. Il formaggio rimane.");
                }
                break;

            case CheesePowerUpType.SpeedBoost:
                RatInputHandler ratInputHandler = rat.GetComponent<RatInputHandler>();
                if (ratInputHandler != null)
                {
                    ratInputHandler.StartCoroutine(ratInputHandler.SpeedBoostRoutine(speedMultiplier, speedDuration));
                    Debug.Log("Velocità aumentata per " + speedDuration + " secondi!");
                    consumed = true;
                }
                break;

            case CheesePowerUpType.DamageBoost:
                rat.ActivateDamageBoost(extraDamage);
                Debug.Log("Danno del prossimo morso aumentato di " + extraDamage);
                consumed = true;
                break;

            case CheesePowerUpType.PoisonLeak:
                rat.EnablePoisonLeak(poisonPuddlePrefab);
                Debug.Log("Power-up Puddle abilitato!");
                consumed = true;
                break;
        }

        if (consumed)
        {
            StartCoroutine(DestroyAfterDelay(1.7f));
        }
    }

    private IEnumerator DestroyAfterDelay(float totalDelay)
    {
        // 1. Attendi prima di far scomparire la mesh (es. 1s)
        yield return new WaitForSeconds(1f);

        if (_renderer != null)
            _renderer.enabled = false;

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
            mainCollider.enabled = false;

        // 2. Attendi il resto del tempo prima della distruzione
        float remainingDelay = Mathf.Max(0f, totalDelay - 1f);
        yield return new WaitForSeconds(remainingDelay);

        Destroy(gameObject);
    }


    private void OnTriggerExit(Collider other)
    {
        if (outlineActive && other.CompareTag(playerTag))
        {
            EnableOutline(false); // ✅ spegne tutto
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