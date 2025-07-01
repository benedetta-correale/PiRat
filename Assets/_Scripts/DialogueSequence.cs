using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(3,10)]
    public string text;
}

[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public List<DialogueLine> lines;
}

