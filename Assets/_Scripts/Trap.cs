using UnityEngine;
using System.Collections;

public enum TrapType { Spring, Glue, Slide }

public class Trap : MonoBehaviour
{
    [Header("Tipo di trappola")]
    [SerializeField] private TrapType trapType;

    [Header("Valori configurabili")]
    [SerializeField] private int springDamage = 30;
    [SerializeField] private float glueDuration = 2f;
    [SerializeField] private float slideForce = 10f;

    [SerializeField] private float springCooldown = 2f;
    private bool springReady = true;

    [SerializeField] private float requiredWiggle = 2f; // quanta "energia" serve per liberarsi
    [SerializeField] private float wiggleDecay = 0.5f;  // quanto si scarica nel tempo se non ti dimeni


    private bool isStuck = false;
    private float wiggleAmount = 0f;
    private RatInputHandler stuckPlayer = null;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (trapType)
        {
            case TrapType.Spring:
                if (!springReady) break;

                var hp = other.GetComponent<BonusMalus>();
                if (hp != null) hp.TakeDamage(springDamage);

                springReady = false;
                StartCoroutine(SpringReset());
                break;


            case TrapType.Glue:
                var pc = other.GetComponent<RatInputHandler>();
                if (pc != null && !isStuck)
                {
                    stuckPlayer = pc;
                    isStuck = true;
                    pc.enabled = false;
                    wiggleAmount = 0f;
                }
                break;


            case TrapType.Slide:
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = other.transform.forward;
                    rb.AddForce(direction * slideForce, ForceMode.Impulse);
                }
                break;
        }
    }

    private void Update()
    {
        if (isStuck && stuckPlayer != null)
        {
            Vector2 input = stuckPlayer.GetMoveInputRaw();  // questa è la riga che chiedevi

            wiggleAmount += input.magnitude * Time.deltaTime * 5f; // aumenta "barra di fuga"
            wiggleAmount -= wiggleDecay * Time.deltaTime;          // decadenza
            wiggleAmount = Mathf.Clamp(wiggleAmount, 0f, requiredWiggle);

            if (wiggleAmount >= requiredWiggle)
            {
                stuckPlayer.enabled = true;
                isStuck = false;
                stuckPlayer = null;
            }
        }
    }



    private IEnumerator GlueEffect(RatInputHandler pc)
    {
        pc.enabled = false;
        yield return new WaitForSeconds(glueDuration);
        pc.enabled = true;
    }

    private IEnumerator SpringReset()
    {
        yield return new WaitForSeconds(springCooldown);
        springReady = true;
    }




#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            trapType.ToString()
        );
    }
#endif

}
