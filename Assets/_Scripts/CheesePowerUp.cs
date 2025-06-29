using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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

        switch (powerUpType)
        {
            case CheesePowerUpType.Heal:
                BonusMalus bonusMalus = rat.GetComponent<BonusMalus>();
                
                if (bonusMalus != null)
                {
                    bonusMalus.Heal(healAmount);
                    Debug.Log("Topo curato di " + healAmount);
                }
                break;

            case CheesePowerUpType.SpeedBoost:
                RatInputHandler ratInputHandler = rat.GetComponent<RatInputHandler>();
                if (ratInputHandler != null)
                {
                    ratInputHandler.StartCoroutine(ratInputHandler.SpeedBoostRoutine(speedMultiplier, speedDuration));
                    Debug.Log("Velocità aumentata per " + speedDuration + " secondi!");
                }
                break;

            case CheesePowerUpType.DamageBoost:
                rat.ActivateDamageBoost(extraDamage);
                Debug.Log("Danno del prossimo morso aumentato di " + extraDamage);
                break;

            case CheesePowerUpType.PoisonLeak:
                rat.EnablePoisonLeak(poisonPuddlePrefab);
                Debug.Log("Power-up Puddle abilitato!");
                break;
        }

        StartCoroutine(DestroyAfterDelay(1f));

    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

}
