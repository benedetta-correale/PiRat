using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Riferimenti al topo")]
    public BonusMalus bonusMalus;                   // componente che gestisce la vita
    public RatInputHandler ratInputHandler;         // tuo handler dei movimenti e boost
    public RatInteractionManager ratInteraction;    // gestisce damage boost, poison leak, ecc.

    [Header("Tag del punto di spawn in ogni scena")]
    public string spawnPointTag = "SpawnPoint";

    [System.Serializable]
    private class RatData
    {
        public int health;
        public List<CheesePowerUpType> activePowerUps = new List<CheesePowerUpType>();
        public List<float> powerUpDurations = new List<float>();
        public List<GameObject[]> puddleTrapPrefabs = new List<GameObject[]>();
    }


    private RatData ratData = new RatData();
    // serve per non applicare il LoadRatData() sulla prima scena all'avvio
    private bool skipInitialLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Chiamalo **prima** di cambiare scena
    /// </summary>
    public void SaveRatData()
    {
        ratData.health = bonusMalus.currentHealth;
        ratData.activePowerUps = ratInteraction.GetActivePowerUps();
        ratData.powerUpDurations = ratInteraction.GetPowerUpRemainingDurations();
        ratData.puddleTrapPrefabs = ratInteraction.GetPuddleTrapPrefabs();

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // La prima volta (all’avvio) vogliamo solo inizializzare, non sovrascrivere
        if (skipInitialLoad)
        {
            skipInitialLoad = false;
            return;
        }

        // → 1) Recupera la nuova istanza del Topo in scena
        var ratGO = GameObject.FindGameObjectWithTag("Player");
        if (ratGO != null)
        {
            bonusMalus = ratGO.GetComponent<BonusMalus>();
            ratInputHandler = ratGO.GetComponent<RatInputHandler>();
            ratInteraction = ratGO.GetComponent<RatInteractionManager>();
        }
        else
        {
            Debug.LogWarning("GameStateManager: non ho trovato il Player in scena.");
        }

        // → 2) Posiziona il Topo sullo SpawnPoint
        var spawn = GameObject.FindWithTag(spawnPointTag);
        if (spawn != null && bonusMalus != null)
        {
            var rt = bonusMalus.transform;
            rt.position = spawn.transform.position;
            rt.rotation = spawn.transform.rotation;
        }

        // → 3) Ripristina vita e power-up sul nuovo Topo
        LoadRatData();
    }



    private void LoadRatData()
    {
        bonusMalus.currentHealth = ratData.health;
        bonusMalus.onHealthChanged?.Invoke(ratData.health, bonusMalus.maxHealth);

        ratInteraction.ApplyPowerUps(
            ratData.activePowerUps,
            ratData.powerUpDurations,
            ratData.puddleTrapPrefabs
        );

    }
}
