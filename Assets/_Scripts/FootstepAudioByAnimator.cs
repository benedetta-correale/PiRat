using UnityEngine;

public class FootstepAudioByAnimator : MonoBehaviour
{
    public AudioSource footstepAudio;   // L'audio dei passi (loop, volume basso)
    public string walkingStateName = "WalkRatAnimation"; // Nome dello stato di camminata
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(walkingStateName))
        {
            if (!footstepAudio.isPlaying)
                footstepAudio.Play();
        }
        else
        {
            if (footstepAudio.isPlaying)
                footstepAudio.Stop();
        }
    }
}