using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    private static TutorialManager instance;
    
    // Usa l'enum globale pubblico definito in CheesePowerUp.cs
    private HashSet<CheesePowerUpType> shownTutorials = new();

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

    public static bool HasTutorialBeenShown(CheesePowerUpType type)
    {
        return instance != null && instance.shownTutorials.Contains(type);
    }

    public static void MarkTutorialAsShown(CheesePowerUpType type)
    {
        if (instance != null)
            instance.shownTutorials.Add(type);
    }
}
