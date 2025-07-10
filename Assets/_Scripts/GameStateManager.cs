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

    [Header("PowerUp Configuration")]
    public PowerUpConfig powerUpConfig;


    [Header("Tag del punto di spawn in ogni scena")]
    public string spawnPointTag = "SpawnPoint";

    [System.Serializable]
    private class RatData
    {
        public int health;

        public bool speedActive;
        public float speedMultiplier;
        public float speedTimeLeft;

        public bool damageActive;
        public int damageAmount;

        public bool poisonReady;
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

        ratData.speedActive = ratInputHandler.speedBoostActive;
        ratData.speedMultiplier = ratInputHandler.currentSpeedBoostMultiplier;
        ratData.speedTimeLeft = ratInputHandler.speedBoostRemainingTime;

        ratData.damageActive = ratInteraction.IsDamageBoostActive;
        ratData.damageAmount = ratInteraction.GetCurrentDamageBoostAmount();

        ratData.poisonReady = ratInteraction.CanPee;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (skipInitialLoad)
        {
            skipInitialLoad = false;
            return;
        }

        // 1) Recupera il nuovo Topo
        var ratGO = GameObject.FindGameObjectWithTag("Player");
        if (ratGO != null)
        {
            bonusMalus = ratGO.GetComponent<BonusMalus>();
            ratInputHandler = ratGO.GetComponent<RatInputHandler>();
            ratInteraction = ratGO.GetComponent<RatInteractionManager>();
        }
        else Debug.LogWarning("GameStateManager: non ho trovato il Player in scena.");

        // 2) Posiziona il Topo
        var spawn = GameObject.FindWithTag(spawnPointTag);
        if (spawn != null && bonusMalus != null)
        {
            var rt = bonusMalus.transform;
            rt.position = spawn.transform.position;
            rt.rotation = spawn.transform.rotation;
        }

        // 3) Ripristina vita e power-up
        LoadRatData();

        // 4) Forza l'aggiornamento della UI salute
        var healthUI = GameObject.FindObjectOfType<RatHealthUI>();
        if (healthUI != null)
            healthUI.UpdateHealthBar(ratData.health, bonusMalus.maxHealth);
        
        // 5) Ripristino power-up via flags + config
        if (powerUpConfig != null)
        {
            if (ratData.speedActive)
            {
                // 1) Ripristina il VFX di speed boost
                ratInputHandler.SetSpeedVFX(powerUpConfig.speedVFXPrefab);

                // 2) Riparte la coroutine con il tempo rimanente
                StartCoroutine(ratInputHandler.SpeedBoostRoutine(
                    ratData.speedMultiplier,
                    ratData.speedTimeLeft
                ));
            }


            if (ratData.damageActive)
                ratInteraction.ActivateDamageBoost(
                    ratData.damageAmount,
                    powerUpConfig.damageVFXPrefab
                );

            if (ratData.poisonReady)
            {
                ratInteraction.PreparePoisonLeak(
                    powerUpConfig.poisonPuddlePrefab,
                    powerUpConfig.poisonVFXPrefab
                );
                ratInteraction.ConfigurePuddleTrap(
                    powerUpConfig.poisonTrapPrefabs
                );
            }
        }

    }



    private void LoadRatData()
    {
        // 1) Ripristina la vita
        bonusMalus.currentHealth = ratData.health;
        bonusMalus.onHealthChanged?.Invoke(ratData.health, bonusMalus.maxHealth);

        // 2) Ripristino power-up via flags + config
        ratInteraction.ApplyPowerUps(
            ratData.speedActive,      // flag speed
            ratData.speedMultiplier,  // moltiplicatore
            ratData.speedTimeLeft,    // tempo rimanente

            ratData.damageActive,     // flag damage
            ratData.damageAmount,     // quantità danno

            ratData.poisonReady       // flag pipì
        );
    }

}
