using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum InputKeyType
{
    Down,
    Left,
    Right,
    Up,
    LeftTrigger,
    RightTrigger,
    LeftStick,
    RightStick,
    ButtonEast,
    ButtonWest,
    ButtonSouth,
    ButtonNorth
}

public class PromptUIManager : MonoBehaviour
{
    [Header("Root UI Elements")]
    [Tooltip("GameObject che contiene KeyIndicator + Text (TMP) + icons")]
    public GameObject inputContainer;       // es. InputContainer sotto il Canvas
    public TextMeshProUGUI promptText;      // il Text (TMP) per il messaggio

    [Header("Icon Buttons")]
    public GameObject downButton;
    public GameObject leftButton;
    public GameObject rightButton;
    public GameObject upButton;
    public GameObject leftTrigger;
    public GameObject rightTrigger;
    public GameObject leftStick;
    public GameObject rightStick;
    public GameObject buttonEast;
    public GameObject buttonWest;
    public GameObject buttonSouth;
    public GameObject buttonNorth;

    private Dictionary<InputKeyType, GameObject> _iconMap;
    private bool _isFrozen = false;
    private float _prevTimeScale = 1f;

    private void Awake()
    {
        // build the map
        _iconMap = new Dictionary<InputKeyType, GameObject>()
        {
            { InputKeyType.Down,         downButton },
            { InputKeyType.Left,         leftButton },
            { InputKeyType.Right,        rightButton },
            { InputKeyType.Up,           upButton },
            { InputKeyType.LeftTrigger,  leftTrigger },
            { InputKeyType.RightTrigger, rightTrigger },
            { InputKeyType.LeftStick,    leftStick },
            { InputKeyType.RightStick,   rightStick },
            { InputKeyType.ButtonEast,   buttonEast },
            { InputKeyType.ButtonWest,   buttonWest },
            { InputKeyType.ButtonSouth,  buttonSouth },
            { InputKeyType.ButtonNorth,  buttonNorth }
        };

        // everything off at start
        inputContainer.SetActive(false);
        foreach (var go in _iconMap.Values)
            if (go != null) go.SetActive(false);
    }

    /// <summary>
    /// Mostra la UI prompt con il testo e l’icon selezionato.
    /// Se freezeTime=true, blocca Time.timeScale.
    /// </summary>
    public void ShowPrompt(InputKeyType key, string message, bool freezeTime = false)
    {
        // freeze time?
        if (freezeTime && !_isFrozen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _isFrozen = true;
        }

        // testo
        promptText.text = message;

        // icon
        foreach (var kv in _iconMap)
            if (kv.Value != null)
                kv.Value.SetActive(kv.Key == key);

        // container on
        inputContainer.SetActive(true);
    }

    /// <summary>
    /// Nasconde la UI prompt e ripristina il Time.timeScale se era frozen.
    /// </summary>
    public void HidePrompt()
    {
        // 1) riporta il timeScale, se era congelato
        if (_isFrozen)
        {
            Time.timeScale = _prevTimeScale;
            _isFrozen = false;
        }

        // 2) pulisce immediatamente il testo
        promptText.text = "";

        // 3) nasconde il container e tutte le icone
        inputContainer.SetActive(false);
        foreach (var go in _iconMap.Values)
            if (go != null)
                go.SetActive(false);
    }

}
