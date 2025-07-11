using UnityEngine;
using State = PirateController.State;

public class PirateAudioManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public State currentState { get; private set; }
    private PirateController pirate;
    private AudioSource audioSource;
    private AudioClip lastClipPlayed = null; // Per evitare di riprodurre lo stesso clip consecutivamente


    [Header("Audio Clips per Stato")]
    public AudioClip idleClip;
    public AudioClip walikingClip;
    public AudioClip attackClip;
    public AudioClip beingHealedClip;
    public AudioClip deadClip;


    void Awake()
    {
        pirate = GetComponent<PirateController>();
        audioSource = GetComponent<AudioSource>();
    }
    void Start()
    {

        //Inizializzo lo stato corrente
        currentState = pirate.GetCurrentState();

    }

    // Update is called once per frame
    void Update()
    {
        currentState = pirate.GetCurrentState();


        switch (currentState)
        {
            case State.Patrol:
                PlayClip(walikingClip, true);
                break;
            case State.Suspicious:
                PlayClip(idleClip, true); // suono sospetto
                break;
            case State.Chasing:
                // suono inseguimento
                PlayClip(walikingClip, true);
                break;
            /*{case State.Attacking:
                PlayClip(attackClip, false); // suono attacco
                break;*/
            case State.BeingHealed:
                PlayClip(beingHealedClip, false); // suono di guarigione
                break;
            case State.Dead:
                PlayClip(deadClip, false); // suono di morte
                break;
        }


    }

    private void PlayClip(AudioClip clip, bool loop = true)
    {
        if (clip == null || clip == lastClipPlayed)
            return;

        audioSource.Stop(); //Ferma immediatamente qualsiasi suono che l'AudioSource sta attualmente riproducendo.
        audioSource.clip = clip; // Imposta il nuovo AudioClip da riprodurre.
        audioSource.loop = loop; // Specifica se il suono deve essere ripetuto in loop continuo (true) o suonato una volta sola (false).
        audioSource.Play(); //riproduce l'audio 

        lastClipPlayed = clip;
    }
    
    public void PlayAttackSound()
    {
        if (attackClip == null) return;

        audioSource.Stop();
        audioSource.clip = attackClip;
        audioSource.loop = false;
        audioSource.Play();

        lastClipPlayed = attackClip;
    }

}
