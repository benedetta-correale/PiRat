using UnityEngine;
using UnityEngine.UI;  // Required for Slider

public class SkillCheck : MonoBehaviour
{
    [SerializeField] private Slider _bar;  // Changed from GameObject to Slider
    [SerializeField] private float oscillationSpeed = 2f;
    
    private bool isActive = false;
    private float time = 0f;

    void Start()
    {
        if (_bar != null)  // Fixed syntax error
        {
            _bar.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Bar not found!");
        }
    }

    void Update()
    {
        if (isActive)
        {
            time += Time.deltaTime * oscillationSpeed;
            float value = (Mathf.Sin(time) + 1f) / 2f;
            _bar.value = value;

            if (Input.GetKeyDown(KeyCode.Space))  // Fixed Input capitalization
            {
                CheckResult(value);
                isActive = false;
                _bar.gameObject.SetActive(false);
            }
        }
    }

    public void StartSkillCheck()  // Changed to PascalCase
    { 
        isActive = true;
        time = 0f;
        _bar.gameObject.SetActive(true);
        Debug.Log("Skill check started!");
    }

    private void CheckResult(float value)
    {
        if (value < 0.6f)
        {
            Debug.Log("Success!");
        }
        else if (value < 0.9f)
        {
            Debug.Log("Chase!");
        }
        else
        {
            Debug.Log("Failure!");
        }
    }
}
