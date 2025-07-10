using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    private bool waitingForRatTrigger = false;

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
    public System.Action OnDialogueEnded;
    [SerializeField] private bool forceRightBoxOnly = false;
    private bool waitingForEndConfirmation = false;
    public PromptUIManager promptUIManager;
    public PirateAutoMove pirateAutoMove;
    public UnityEngine.UI.Image bersaglio;
    public QuickTimeUIManager quickTimeUIManager;
    [SerializeField] private DialogueSequence empowermentDialogue;


    public void ForceRightBoxOnly(bool value)
    {
        forceRightBoxOnly = value;
    }

    void Start()
    {
        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);

        rat.position = ratInitialPosition;
        mainCamera.position = cameraInitialPosition;
        mainCamera.rotation = Quaternion.Euler(cameraInitialRotation);

        ratMovementScript.enabled = false;
        cameraScript.enabled = false;
        bersaglio.gameObject.SetActive(false);

        StartDialogue(introDialogue); // ← avvia subito il primo dialogo
    }

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Return))
        {
            ShowNextLine();
        }
    }


    public void StartDialogue(DialogueSequence sequence)
    {
        promptUIManager.HidePrompt(); // ← reset UI a inizio dialogo

        currentSequence = sequence;
        currentIndex = 0;
        isDialogueActive = true;
        waitingForEndConfirmation = false;

        if (sequence.sequenceID == "prisoner") waitingForRatTrigger = true;

        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);

        ShowNextLine();
    }


    private void HandleLineEvents(int index)
    {
        if (currentSequence.sequenceID == "prisoner")
        {
            switch (index)
            {
                case 0:
                    // PRIMA battuta del prigioniero
                    promptUIManager.ShowPrompt(InputKeyType.RightStick, "Rotate camera with right stick or mouse movement", true);
                    cameraScript.enabled = true;
                    break;
                case 1:
                    promptUIManager.ShowPrompt(InputKeyType.LeftStick, "Move with left stick or WASD", true);
                    ratMovementScript.enabled = true;
                    break;
                case 5:
                    // UI morso
                    promptUIManager.ShowPrompt(InputKeyType.RightTrigger, "Bite with right trigger or mouse click", true);
                    break;
                case 6:
                    promptUIManager.ShowPrompt(InputKeyType.ButtonEast, "Hit red target for maximum boost with this button or SPACEBAR", true);
                    bersaglio.gameObject.SetActive(true);
                    quickTimeUIManager.tutorialMode = true;
                    break;
                case 7:
                    // Nascondi prompt se vuoi
                    bersaglio.gameObject.SetActive(false);
                    promptUIManager.HidePrompt();
                    pirateAutoMove?.MoveToTarget();
                    break;
            }
        }

        if (currentSequence.sequenceID == "possession")
        {
            switch (index)
            {
                case 0:
                    promptUIManager.ShowPrompt(InputKeyType.LeftTrigger, "Enter in selection mode with left trigger or TAB", true);
                    /* if (!possessionManager.inSelectionMode && !possessionManager.inPossessionMode)
                    {
                        promptUIManager.ShowPrompt(InputKeyType.LeftTrigger, "Enter in selection mode with left trigger or TAB", true);
                    }
                    else if (possessionManager.inSelectionMode)
                    {
                        promptUIManager.ShowPrompt(InputKeyType.LeftStick, "Select pirate with left stick or WASD", true);
                    }
                    else if (possessionManager.inPossessionMode)
                    {
                        promptUIManager.ShowPrompt(InputKeyType.ButtonSouth, "Possess pirate with this button or ESCAPE", true);
                    } */
                    break;
                case 1:
                    promptUIManager.HidePrompt();
                    break;
            }
        }
    }


    private void ShowNextLine()
    {
        if (currentIndex >= currentSequence.lines.Count)
        {
            EndDialogue(); // subito, senza attesa doppia
            return;
        }

        if (currentSequence.sequenceID == "prisoner" && currentIndex == 2 && waitingForRatTrigger)
            return;

        DialogueLine line = currentSequence.lines[currentIndex];

        bool showLeft = !forceRightBoxOnly && currentIndex % 2 == 0;

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

        HandleLineEvents(currentIndex);

        currentIndex++;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        currentSequence = null;

        leftDialogueBox.SetActive(false);
        rightDialogueBox.SetActive(false);

        OnDialogueEnded?.Invoke();
        quickTimeUIManager.tutorialMode = false;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
    
    public void ContinuePrisonerDialogue()
    {
        if (waitingForRatTrigger)
        {
            waitingForRatTrigger = false;
            ShowNextLine(); // riprende il dialogo da dove si era bloccato
        }
    }
}