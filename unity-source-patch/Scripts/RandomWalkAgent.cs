using System.Collections;
using UnityEngine;

public class RandomWalkAgent : MonoBehaviour
{
    // =====================================================
    // SIMULATION SETTINGS
    // =====================================================

    [Header("Simulation")]

    public int MAX_MOVES = 1000;

    public float MOVE_DURATION = 0.2f;

    public float PAUSE_BETWEEN_MOVES = 0.05f;


    // =====================================================
    // MATHEMATICAL STATE
    // =====================================================

    private int movesMade = 0;

    private int position = 0;


    // =====================================================
    // UNITY STATE
    // =====================================================

    private Vector3 origin;

    private bool initialized = false;

    private bool hasFinished = false;

    private bool isBeingReset = false;


    // =====================================================
    // RANDOMNESS
    // =====================================================

    /*
     * Every walker gets its OWN random generator.
     *
     * This makes every trajectory independent and avoids
     * coupling the visual agents through Unity's global RNG.
     */

    private System.Random rng;

    private float initialDelay = 0f;

    private float maxTimingJitter = 0f;


    // =====================================================
    // INITIALIZE
    // =====================================================

    public void Initialize(
        Vector3 homePosition,
        int randomSeed,
        float startDelay,
        float timingJitter
    )
    {
        if (initialized)
        {
            return;
        }


        initialized = true;

        hasFinished = false;

        isBeingReset = false;


        movesMade = 0;

        position = 0;


        // =================================================
        // HOME
        // =================================================

        origin =
            homePosition;


        transform.position =
            origin;


        // =================================================
        // INDEPENDENT RNG
        // =================================================

        rng =
            new System.Random(
                randomSeed
            );


        initialDelay =
            Mathf.Max(
                0f,
                startDelay
            );


        maxTimingJitter =
            Mathf.Max(
                0f,
                timingJitter
            );


        StartCoroutine(
            RandomWalk()
        );
    }


    // =====================================================
    // SAFETY FOR ACCIDENTAL SCENE BALLS
    // =====================================================

    IEnumerator Start()
    {
        /*
         * A legitimate spawned walker gets Initialize()
         * during the same frame it is instantiated.
         *
         * A manually placed RandomWalkAgent does NOT.
         */

        yield return null;


        if (!initialized)
        {
            Debug.LogWarning(
                "Removing stray RandomWalkAgent from scene."
            );


            gameObject.SetActive(false);


            Destroy(
                gameObject
            );
        }
    }


    // =====================================================
    // RANDOM WALK
    // =====================================================

    IEnumerator RandomWalk()
    {
        // -------------------------------------------------
        // RANDOMIZE VISUAL START TIME
        // -------------------------------------------------

        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(
                initialDelay
            );
        }


        if (isBeingReset)
        {
            yield break;
        }


        int moveLimit =
            Mathf.Max(
                1,
                MAX_MOVES
            );


        while (
            movesMade < moveLimit
            && !isBeingReset
        )
        {
            // =================================================
            // 1. RANDOM LEFT / RIGHT
            // =================================================

            int direction;


            if (rng.NextDouble() < 0.5)
            {
                direction = -1;
            }
            else
            {
                direction = 1;
            }


            // =================================================
            // 2. UPDATE MATHEMATICAL POSITION
            // =================================================

            position += direction;

            movesMade++;


            // =================================================
            // 3. NUMBER-LINE SETTINGS
            // =================================================

            float stepSize = 1f;


            if (NumberLine.Instance != null)
            {
                stepSize =
                    NumberLine.Instance.Spacing;


                NumberLine.Instance.EnsureVisible(
                    position
                );
            }


            // =================================================
            // 4. PHYSICAL TARGET
            // =================================================

            Vector3 targetPosition =
                origin
                + Vector3.right
                * position
                * stepSize;


            // =================================================
            // 5. SLIGHT RANDOM VISUAL TIMING
            // =================================================

            float movementJitter =
                RandomBetween(
                    -maxTimingJitter,
                    maxTimingJitter
                );


            float thisMoveDuration =
                Mathf.Max(
                    0.02f,
                    MOVE_DURATION
                    + movementJitter
                );


            // =================================================
            // 6. MOVE
            // =================================================

            yield return StartCoroutine(
                MoveSmoothly(
                    targetPosition,
                    thisMoveDuration
                )
            );


            if (isBeingReset)
            {
                yield break;
            }


            // =================================================
            // 7. HOME CHECK
            // =================================================

            /*
             * Notice this uses the INTEGER mathematical
             * position.
             *
             * We are NOT guessing based on transform.x.
             */

            if (position == 0)
            {
                FinishAsReturned();

                yield break;
            }


            // =================================================
            // 8. RANDOMIZED VISUAL PAUSE
            // =================================================

            float pauseJitter =
                RandomBetween(
                    0f,
                    maxTimingJitter
                );


            float thisPause =
                PAUSE_BETWEEN_MOVES
                + pauseJitter;


            if (thisPause > 0f)
            {
                yield return new WaitForSeconds(
                    thisPause
                );
            }
        }


        // =====================================================
        // MOVE LIMIT
        // =====================================================

        if (!isBeingReset)
        {
            FinishAsExhausted(
                moveLimit
            );
        }
    }


    // =====================================================
    // RANDOM FLOAT HELPER
    // =====================================================

    float RandomBetween(
        float minimum,
        float maximum
    )
    {
        if (rng == null)
        {
            return minimum;
        }


        double value =
            rng.NextDouble();


        return minimum
            + (float)value
            * (maximum - minimum);
    }


    // =====================================================
    // RETURNED HOME
    // =====================================================

    void FinishAsReturned()
    {
        if (
            hasFinished
            || isBeingReset
        )
        {
            return;
        }


        hasFinished = true;


        if (StatsHUD.Instance != null)
        {
            StatsHUD.Instance.WalkerReturned();
        }


        Destroy(
            gameObject
        );
    }


    // =====================================================
    // MOVE LIMIT REACHED
    // =====================================================

    void FinishAsExhausted(
        int moveLimit
    )
    {
        if (
            hasFinished
            || isBeingReset
        )
        {
            return;
        }


        hasFinished = true;


        if (StatsHUD.Instance != null)
        {
            StatsHUD.Instance.WalkerReachedMoveLimit(
                moveLimit
            );
        }


        /*
         * Deliberately leave the ball visible at its
         * final position.
         */
    }


    // =====================================================
    // RESET
    // =====================================================

    public void PrepareForReset()
    {
        if (isBeingReset)
        {
            return;
        }


        isBeingReset = true;

        hasFinished = true;


        StopAllCoroutines();


        enabled = false;
    }


    // =====================================================
    // SMOOTH MOVEMENT
    // =====================================================

    IEnumerator MoveSmoothly(
        Vector3 targetPosition,
        float duration
    )
    {
        Vector3 startPosition =
            transform.position;


        if (duration <= 0f)
        {
            transform.position =
                targetPosition;

            yield break;
        }


        float elapsedTime =
            0f;


        while (
            elapsedTime < duration
            && !isBeingReset
        )
        {
            elapsedTime +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsedTime
                    / duration
                );


            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );


            yield return null;
        }


        if (!isBeingReset)
        {
            transform.position =
                targetPosition;
        }
    }
}