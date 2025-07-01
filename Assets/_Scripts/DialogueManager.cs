using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    private DialogueSequence currentSequence;
    private int currentIndex = 0;

    public void StartDialogue(DialogueSequence sequence)
    {
        currentSequence = sequence;
        currentIndex = 0;
        ShowNextLine();
    }

    void Update()
    {
        if (currentSequence != null && Input.GetKeyDown(KeyCode.Return))
        {
            ShowNextLine();
        }
    }

    public void ShowNextLine()
    {
        if (currentSequence == null) return;
        if (currentIndex >= currentSequence.lines.Count)
        {
            currentSequence = null;
            return;
        }

        DialogueLine line = currentSequence.lines[currentIndex];
        speakerNameText.text = line.speakerName;
        dialogueText.text = line.text;

        currentIndex++;
    }
}

