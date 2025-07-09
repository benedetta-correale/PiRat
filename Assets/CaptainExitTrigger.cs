using UnityEngine;

public class CaptainExitTrigger : MonoBehaviour
{
    [SerializeField] private PrisonerDialogueTrigger prisonerTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pirate")) // Assicurati che il Capitano abbia questo tag
        {
            Debug.Log("Capitano entrato nel trigger. Avvio dialogo prigioniero.");
            prisonerTrigger.TriggerPrisonerDialogue();

            // Disattiva per sicurezza
            gameObject.SetActive(false);
        }
    }
}
