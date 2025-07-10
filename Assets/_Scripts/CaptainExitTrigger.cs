using UnityEngine;

public class CaptainExitTrigger : MonoBehaviour
{
    [SerializeField] private PrisonerDialogueTrigger prisonerTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Captain")) // Assicurati che il Capitano abbia questo tag
        {
            prisonerTrigger.TriggerPrisonerDialogue();

            // Disattiva per sicurezza
            gameObject.SetActive(false);
        }
    }
}
