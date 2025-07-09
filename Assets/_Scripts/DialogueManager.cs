using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI captainSpeakerText;
    [SerializeField] private TextMeshProUGUI captainDialogueText;
    [SerializeField] private GameObject captainDialogueBox;

    [SerializeField] private TextMeshProUGUI pirateSpeakerText;
    [SerializeField] private TextMeshProUGUI pirateDialogueText;
    [SerializeField] private GameObject pirateDialogueBox;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueSequence introDialogue;

    private DialogueSequence currentSequence;
    private int currentIndex = 0;
    private bool isDialogueActive = false;

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

        captainDialogueBox.SetActive(false);
        pirateDialogueBox.SetActive(false);

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
        ShowLineInCorrectBox(line);
        currentIndex++;
    }

    private void ShowLineInCorrectBox(DialogueLine line)
    {
        bool isCaptain = line.speakerName.ToLower().Contains("captain");

        captainDialogueBox.SetActive(isCaptain);
        pirateDialogueBox.SetActive(!isCaptain);

        if (isCaptain)
        {
            captainSpeakerText.text = line.speakerName;
            captainDialogueText.text = line.text;
        }
        else
        {
            pirateSpeakerText.text = line.speakerName;
            pirateDialogueText.text = line.text;
        }
    }

    private void EndDialogue()
    {
        currentSequence = null;
        isDialogueActive = false;

        captainDialogueBox.SetActive(false);
        pirateDialogueBox.SetActive(false);
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}