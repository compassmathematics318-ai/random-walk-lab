using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class FreeCameraController : MonoBehaviour
{
    // =====================================================
    // MOVEMENT
    // =====================================================

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;


    // =====================================================
    // MOUSE LOOK
    // =====================================================

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;

    [SerializeField] private float minPitch = -80f;

    [SerializeField] private float maxPitch = 80f;


    // =====================================================
    // ZOOM
    // =====================================================

    [Header("Zoom")]

    [SerializeField] private float fovPerScrollStep = 6f;

    [SerializeField] private float minFOV = 15f;

    [SerializeField] private float maxFOV = 85f;

    [SerializeField] private float orthoZoomPerScrollStep = 1f;

    [SerializeField] private float minOrthoSize = 1f;

    [SerializeField] private float maxOrthoSize = 50f;

    [SerializeField] private float zoomSmoothness = 12f;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private Camera cam;

    private float yaw;

    private float pitch;

    private float targetFOV;

    private float targetOrthoSize;

    private InputAction scrollAction;


    private bool isPaused = false;

    private bool isResetting = false;


    public static bool IsPaused
    {
        get;
        private set;
    }


    // =====================================================
    // INITIAL CAMERA STATE
    // =====================================================

    private Vector3 initialPosition;

    private Quaternion initialRotation;

    private float initialFOV;

    private float initialOrthoSize;


    // =====================================================
    // INITIALIZATION
    // =====================================================

    void Awake()
    {
        cam =
            GetComponent<Camera>();


        scrollAction =
            new InputAction(
                "MouseScroll",
                InputActionType.PassThrough,
                "<Mouse>/scroll/y"
            );
    }


    void OnEnable()
    {
        scrollAction.Enable();
    }


    void OnDisable()
    {
        scrollAction.Disable();
    }


    void Start()
    {
        initialPosition =
            transform.position;


        initialRotation =
            transform.rotation;


        initialFOV =
            cam.fieldOfView;


        initialOrthoSize =
            cam.orthographicSize;


        SynchronizeLookAngles();


        targetFOV =
            initialFOV;


        targetOrthoSize =
            initialOrthoSize;


        ResumeSimulation();
    }


    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        if (
            Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame &&
            !isResetting
        )
        {
            StartCoroutine(
                ResetEverything()
            );

            return;
        }


        if (isResetting)
        {
            return;
        }


        HandlePauseInput();


        if (isPaused)
        {
            return;
        }


        HandleMovement();

        HandleMouseLook();

        HandleZoom();
    }


    // =====================================================
    // RESET
    // =====================================================

    IEnumerator ResetEverything()
    {
        isResetting =
            true;


        // ---------------------------------------------
        // RESUME TIME
        // ---------------------------------------------

        Time.timeScale =
            1f;


        isPaused =
            false;


        IsPaused =
            false;


        // ---------------------------------------------
        // RESET CAMERA
        // ---------------------------------------------

        transform.position =
            initialPosition;


        transform.rotation =
            initialRotation;


        cam.fieldOfView =
            initialFOV;


        cam.orthographicSize =
            initialOrthoSize;


        targetFOV =
            initialFOV;


        targetOrthoSize =
            initialOrthoSize;


        SynchronizeLookAngles();


        // ---------------------------------------------
        // RESET EXPERIMENT
        // ---------------------------------------------

        if (AgentSpawner.Instance != null)
        {
            yield return StartCoroutine(
                AgentSpawner.Instance.ResetExperiment()
            );
        }


        // ---------------------------------------------
        // RE-LOCK CURSOR
        // ---------------------------------------------

        yield return null;


        Cursor.lockState =
            CursorLockMode.Locked;


        Cursor.visible =
            false;


        isResetting =
            false;
    }


    void SynchronizeLookAngles()
    {
        yaw =
            transform.eulerAngles.y;


        pitch =
            transform.eulerAngles.x;


        if (pitch > 180f)
        {
            pitch -= 360f;
        }
    }


    // =====================================================
    // PAUSE
    // =====================================================

    void HandlePauseInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }


        // Q is used instead of Escape so the browser can reserve Esc
        // for leaving fullscreen mode in the Web build.
        if (
            Keyboard.current.qKey
                .wasPressedThisFrame
        )
        {
            if (isPaused)
            {
                ResumeSimulation();
            }
            else
            {
                PauseSimulation();
            }
        }
    }


    void PauseSimulation()
    {
        isPaused =
            true;


        IsPaused =
            true;


        Time.timeScale =
            0f;


        Cursor.lockState =
            CursorLockMode.None;


        Cursor.visible =
            true;
    }


    void ResumeSimulation()
    {
        isPaused =
            false;


        IsPaused =
            false;


        Time.timeScale =
            1f;


        StartCoroutine(
            LockCursorNextFrame()
        );
    }


    IEnumerator LockCursorNextFrame()
    {
        yield return null;


        if (
            !isPaused &&
            !isResetting
        )
        {
            Cursor.lockState =
                CursorLockMode.Locked;


            Cursor.visible =
                false;
        }
    }


    // =====================================================
    // MOVEMENT
    // =====================================================

    void HandleMovement()
    {
        if (Keyboard.current == null)
        {
            return;
        }


        Vector3 direction =
            Vector3.zero;


        if (Keyboard.current.wKey.isPressed)
        {
            direction +=
                transform.forward;
        }


        if (Keyboard.current.sKey.isPressed)
        {
            direction -=
                transform.forward;
        }


        if (Keyboard.current.aKey.isPressed)
        {
            direction -=
                transform.right;
        }


        if (Keyboard.current.dKey.isPressed)
        {
            direction +=
                transform.right;
        }


        if (Keyboard.current.spaceKey.isPressed)
        {
            direction +=
                Vector3.up;
        }


        if (
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed
        )
        {
            direction -=
                Vector3.up;
        }


        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }


        transform.position +=
            direction
            * moveSpeed
            * Time.deltaTime;
    }


    // =====================================================
    // MOUSE LOOK
    // =====================================================

    void HandleMouseLook()
    {
        if (Mouse.current == null)
        {
            return;
        }


        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();


        yaw +=
            mouseDelta.x
            * mouseSensitivity;


        pitch -=
            mouseDelta.y
            * mouseSensitivity;


        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );


        transform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );
    }


    // =====================================================
    // ZOOM
    // =====================================================

    void HandleZoom()
    {
        float rawScroll =
            scrollAction.ReadValue<float>();


        if (
            Mathf.Abs(rawScroll) < 0.001f &&
            Mouse.current != null
        )
        {
            rawScroll =
                Mouse.current.scroll
                    .ReadValue().y;
        }


        if (Mathf.Abs(rawScroll) > 0.001f)
        {
            float scrollDirection =
                Mathf.Sign(
                    rawScroll
                );


            if (cam.orthographic)
            {
                targetOrthoSize -=
                    scrollDirection
                    * orthoZoomPerScrollStep;


                targetOrthoSize =
                    Mathf.Clamp(
                        targetOrthoSize,
                        minOrthoSize,
                        maxOrthoSize
                    );
            }
            else
            {
                targetFOV -=
                    scrollDirection
                    * fovPerScrollStep;


                targetFOV =
                    Mathf.Clamp(
                        targetFOV,
                        minFOV,
                        maxFOV
                    );
            }
        }


        float smoothFactor =
            1f
            - Mathf.Exp(
                -zoomSmoothness
                * Time.deltaTime
            );


        if (cam.orthographic)
        {
            cam.orthographicSize =
                Mathf.Lerp(
                    cam.orthographicSize,
                    targetOrthoSize,
                    smoothFactor
                );
        }
        else
        {
            cam.fieldOfView =
                Mathf.Lerp(
                    cam.fieldOfView,
                    targetFOV,
                    smoothFactor
                );
        }
    }
}