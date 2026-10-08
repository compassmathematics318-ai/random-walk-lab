using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

public class AgentSpawner : MonoBehaviour
{
    public static AgentSpawner Instance { get; private set; }

    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Agent Settings")]

    [SerializeField]
    private GameObject agentPrefab;

    [SerializeField]
    private int agentCount = 100;

    [SerializeField]
    private float agentHeight = 0.5f;


    [Header("Web Configurable Experiment")]

    [SerializeField]
    private int maxMoves = 1000;

    [SerializeField]
    [Range(0.25f, 5f)]
    private float walkSpeedMultiplier = 1f;


    [Header("Visual Randomness")]

    /*
     * Walkers are still mathematically 1D.
     *
     * This ONLY slightly desynchronizes when they begin
     * moving so that 100 walkers don't visually move
     * like one giant synchronized object.
     */
    [SerializeField]
    private float maxInitialDelay = 0.20f;

    /*
     * Slight variation in the visual timing of each step.
     * Does NOT change the random-walk probabilities.
     */
    [SerializeField]
    private float maxTimingJitter = 0.04f;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private Transform agentContainer;

    private bool isResetting = false;

    private int experimentNumber = 0;

    private float baseMoveDuration = 0.2f;

    private float basePauseBetweenMoves = 0.05f;


    [Serializable]
    private class WebExperimentConfig
    {
        public int agentCount = 100;
        public int maxMoves = 1000;
        public float speedMultiplier = 1f;
    }


    // =====================================================
    // INITIALIZATION
    // =====================================================

    void Awake()
    {
        Instance = this;

        /*
         * Preserve the prefab's existing movement timing as the 1x baseline.
         * The website speed slider scales from these values instead of
         * hard-coding a second copy of the timing constants.
         */
        if (agentPrefab != null)
        {
            RandomWalkAgent prefabAgent =
                agentPrefab.GetComponent<RandomWalkAgent>();

            if (prefabAgent != null)
            {
                baseMoveDuration =
                    Mathf.Max(0.01f, prefabAgent.MOVE_DURATION);

                basePauseBetweenMoves =
                    Mathf.Max(0f, prefabAgent.PAUSE_BETWEEN_MOVES);
            }
        }
    }


    IEnumerator Start()
    {
        Time.timeScale = 1f;

        /*
         * IMPORTANT:
         *
         * Remove any RandomWalkAgent accidentally left
         * manually in the scene.
         *
         * This is exactly what caused the stationary
         * center ball visible in your video.
         */
        yield return RemoveStraySceneAgents();

        StartFreshExperiment();
    }


    // =====================================================
    // START NEW EXPERIMENT
    // =====================================================

    void StartFreshExperiment()
    {
        experimentNumber++;


        // Reset HUD.
        if (StatsHUD.Instance != null)
        {
            StatsHUD.Instance.SetTotalWalkers(
                agentCount
            );
        }


        // Create holder for this experiment's walkers.
        GameObject containerObject =
            new GameObject(
                "Spawned Random Walk Agents"
            );


        agentContainer =
            containerObject.transform;


        SpawnAgents();
    }


    // =====================================================
    // SPAWN AGENTS
    // =====================================================

    void SpawnAgents()
    {
        /*
         * The yellow HOME tick is at world X = 0.
         *
         * Every official walker begins here.
         */

        Vector3 homePosition =
            new Vector3(
                0f,
                agentHeight,
                0f
            );


        /*
         * New seed for every experiment.
         *
         * experimentNumber prevents rapid resets from
         * accidentally creating the same seed.
         */

        int experimentSeed =
            unchecked(
                Environment.TickCount
                ^ (experimentNumber * 73856093)
                ^ GetInstanceID()
            );


        System.Random seedGenerator =
            new System.Random(
                experimentSeed
            );


        for (int i = 0; i < agentCount; i++)
        {
            GameObject agentObject =
                Instantiate(
                    agentPrefab,
                    homePosition,
                    Quaternion.identity,
                    agentContainer
                );


            RandomWalkAgent agent =
                agentObject.GetComponent<RandomWalkAgent>();


            if (agent == null)
            {
                Debug.LogError(
                    "Agent prefab does not contain RandomWalkAgent!"
                );

                Destroy(agentObject);

                continue;
            }


            // Apply the current website-configurable experiment settings.
            float safeSpeed =
                Mathf.Clamp(walkSpeedMultiplier, 0.25f, 5f);

            agent.MAX_MOVES =
                Mathf.Clamp(maxMoves, 1, 100000);

            agent.MOVE_DURATION =
                Mathf.Max(
                    0.01f,
                    baseMoveDuration / safeSpeed
                );

            agent.PAUSE_BETWEEN_MOVES =
                Mathf.Max(
                    0f,
                    basePauseBetweenMoves / safeSpeed
                );


            // Independent random seed for THIS walker.
            int agentSeed =
                seedGenerator.Next();


            // Tiny random start offset.
            float initialDelay =
                (float)seedGenerator.NextDouble()
                * maxInitialDelay
                / Mathf.Clamp(walkSpeedMultiplier, 0.25f, 5f);


            agent.Initialize(
                homePosition,
                agentSeed,
                initialDelay,
                maxTimingJitter
                / Mathf.Clamp(walkSpeedMultiplier, 0.25f, 5f)
            );
        }
    }


    // =====================================================
    // RESET EXPERIMENT
    // =====================================================

    public IEnumerator ResetExperiment()
    {
        if (isResetting)
        {
            yield break;
        }


        isResetting = true;


        // =================================================
        // 1. STOP / REMOVE OLD WALKERS
        // =================================================

        if (agentContainer != null)
        {
            RandomWalkAgent[] oldAgents =
                agentContainer
                    .GetComponentsInChildren<RandomWalkAgent>(
                        true
                    );


            foreach (RandomWalkAgent agent in oldAgents)
            {
                if (agent != null)
                {
                    agent.PrepareForReset();
                }
            }


            GameObject oldContainer =
                agentContainer.gameObject;


            agentContainer =
                null;


            // Hide immediately.
            oldContainer.SetActive(false);


            Destroy(
                oldContainer
            );


            // Destroy() completes at end of frame.
            yield return null;
        }


        // =================================================
        // 2. DELETE ANY STRAY WALKER
        // =================================================

        yield return RemoveStraySceneAgents();


        // =================================================
        // 3. RESET NUMBER LINE
        // =================================================

        if (NumberLine.Instance != null)
        {
            NumberLine.Instance.ResetNumberLine();
        }


        // =================================================
        // 4. NEW EXPERIMENT
        // =================================================

        StartFreshExperiment();


        isResetting = false;
    }


    // =====================================================
    // REMOVE WALKERS THAT DO NOT BELONG TO SPAWNER
    // =====================================================

    IEnumerator RemoveStraySceneAgents()
    {
        RandomWalkAgent[] agents =
            FindObjectsByType<RandomWalkAgent>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        bool destroyedAnything = false;


        foreach (RandomWalkAgent agent in agents)
        {
            if (agent == null)
            {
                continue;
            }


            /*
             * At initial startup there should be ZERO
             * legitimate spawned agents yet.
             *
             * During reset, the old container has already
             * been destroyed before we arrive here.
             */

            agent.PrepareForReset();


            agent.gameObject.SetActive(false);


            Destroy(
                agent.gameObject
            );


            destroyedAnything = true;
        }


        if (destroyedAnything)
        {
            yield return null;
        }
    }


    // =====================================================
    // WEBSITE BRIDGE
    // =====================================================

    /*
     * The webpage calls this with:
     *
     * unityInstance.SendMessage(
     *     "AgentSpawner",
     *     "ApplyWebConfig",
     *     JSON.stringify({
     *         agentCount: 250,
     *         maxMoves: 5000,
     *         speedMultiplier: 1.5
     *     })
     * );
     *
     * Keep the scene GameObject named "AgentSpawner" unless the
     * frontend SendMessage target is changed too.
     */
    [Preserve]
    public void ApplyWebConfig(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning(
                "ApplyWebConfig received an empty configuration."
            );

            return;
        }


        WebExperimentConfig config;


        try
        {
            config =
                JsonUtility.FromJson<WebExperimentConfig>(
                    json
                );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not parse web experiment config: "
                + exception.Message
            );

            return;
        }


        if (config == null)
        {
            return;
        }


        agentCount =
            Mathf.Clamp(
                config.agentCount,
                1,
                2000
            );


        maxMoves =
            Mathf.Clamp(
                config.maxMoves,
                1,
                100000
            );


        walkSpeedMultiplier =
            Mathf.Clamp(
                config.speedMultiplier,
                0.25f,
                5f
            );


        if (!isResetting)
        {
            StartCoroutine(
                ResetExperiment()
            );
        }
    }


    // =====================================================
    // CLEANUP
    // =====================================================

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}