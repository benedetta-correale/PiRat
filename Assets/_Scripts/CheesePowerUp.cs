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
            StartCoroutine(DestroyAfterDelay(1f));
        }

    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
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
