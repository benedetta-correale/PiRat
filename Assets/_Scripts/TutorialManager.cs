using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private static TutorialManager instance;

    private HashSet<CheesePowerUpType> shownCheeseTutorials = new();
    private HashSet<TrapType> shownTrapTutorials = new();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---- Cheese
    public static bool HasTutorialBeenShown(CheesePowerUpType type)
        => instance != null && instance.shownCheeseTutorials.Contains(type);

    public static void MarkTutorialAsShown(CheesePowerUpType type)
        => instance?.shownCheeseTutorials.Add(type);

    // ---- Trap
    public static bool HasTutorialBeenShown(TrapType type)
        => instance != null && instance.shownTrapTutorials.Contains(type);

    public static void MarkTutorialAsShown(TrapType type)
        => instance?.shownTrapTutorials.Add(type);
}