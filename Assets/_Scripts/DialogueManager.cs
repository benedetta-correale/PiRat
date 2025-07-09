using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI leftSpeakerText;
    [SerializeField] private TextMeshProUGUI leftDialogueText;
    [SerializeField] private GameObject leftDialogueBox;

    [SerializeField] private TextMeshProUGUI rightSpeakerText;
    [SerializeField] private TextMeshProUGUI rightDialogueText;
    [SerializeField] private GameObject rightDialogueBox;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueSequence introDialogue;

    private DialogueSequence currentSequence;
    private int currentIndex = 0;
    private bool isDialogueActive = false;
    public Transform rat;
    public Vector3 ratInitialPosition;
    public Vector3 cameraInitialPosition;
    public Vector3 cameraInitialRotation;
    public Transform mainCamera;
    public RatInputHandler ratMovementScript;
    public CameraControlManager cameraScript;


    void Start()
    {
        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);
        rat.position = ratInitialPosition;
        mainCamera.position = cameraInitialPosition;
        mainCamera.rotation = Quaternion.Euler(cameraInitialRotation);
        ratMovementScript.enabled = false;
        cameraScript.enabled = false;
    }


    void Update()
    {
        if (!isDialogueActive && Input.GetKeyDown(KeyCode.P))
        {
            StartDialogue(introDialogue);
        }

        if (isDialogueActive && Input.GetKeyDown(KeyCode.Return))
        {
            ShowNextLine();
        }
    }

    public void StartDialogue(DialogueSequence sequence)
    {
        currentSequence = sequence;
        currentIndex = 0;
        isDialogueActive = true;

        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (currentSequence == null || currentIndex >= currentSequence.lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentSequence.lines[currentIndex];

        // Alternanza sinistra/destra
        bool showLeft = currentIndex % 2 == 0;

        leftDialogueBox.SetActive(showLeft);
        rightDialogueBox.SetActive(!showLeft);

        if (showLeft)
        {
            leftSpeakerText.text = line.speakerName;
            leftDialogueText.text = line.text;
        }
        else
        {
            rightSpeakerText.text = line.speakerName;
            rightDialogueText.text = line.text;
        }

        currentIndex++;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        currentSequence = null;

        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);
        ratMovementScript.enabled = true;
        cameraScript.enabled = true;
        mainCamera.position = cameraInitialPosition;
        mainCamera.rotation = Quaternion.Euler(cameraInitialRotation);
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}