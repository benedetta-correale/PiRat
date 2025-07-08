using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI leftSpeakerName;
    [SerializeField] private TextMeshProUGUI leftDialogueText;
    [SerializeField] private TextMeshProUGUI rightSpeakerName;
    [SerializeField] private TextMeshProUGUI rightDialogueText;

    [Header("Dialogue")]
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
        ShowNextLine();
    }

    public void ShowNextLine()
    {
        if (currentSequence == null) return;

        if (currentIndex >= currentSequence.lines.Count)
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

        if (line.speakerName == "Capitano")
        {
            leftSpeakerName.text = line.speakerName;
            leftDialogueText.text = line.text;
        }
        else
        {
            rightSpeakerName.text = line.speakerName;
            rightDialogueText.text = line.text;
        }
    }

    private void EndDialogue()
    {
        currentSequence = null;
        isDialogueActive = false;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}