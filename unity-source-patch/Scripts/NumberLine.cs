using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NumberLine : MonoBehaviour
{
    public static NumberLine Instance { get; private set; }


    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Initial Range")]
    [SerializeField] private int initialMinValue = -10;
    [SerializeField] private int initialMaxValue = 10;


    [Header("Dynamic Expansion")]
    [SerializeField] private int expansionAmount = 20;
    [SerializeField] private int cameraPreloadRadius = 40;
    [SerializeField] private int agentBuffer = 5;


    [Header("Spacing")]
    [SerializeField] private float spacing = 1f;


    [Header("Appearance")]
    [SerializeField] private float lineThickness = 0.05f;
    [SerializeField] private float tickLength = 0.4f;
    [SerializeField] private float tickThickness = 0.05f;
    [SerializeField] private float labelSize = 2f;


    [Header("Colors")]

    [SerializeField]
    private Color lineColor =
        new Color(
            0f,
            1f,
            1f,
            1f
        );


    [SerializeField]
    private Color homeColor =
        new Color(
            1f,
            0.9f,
            0f,
            1f
        );


    // =====================================================
    // STATE
    // =====================================================

    private int minValue;

    private int maxValue;

    private GameObject mainLine;

    private Transform generatedRoot;

    private Transform cameraTransform;


    // Cache everything instead of destroying it.
    private Dictionary<int, GameObject> ticks =
        new Dictionary<int, GameObject>();


    private Dictionary<int, GameObject> labels =
        new Dictionary<int, GameObject>();


    private GameObject homeLabelObject;


    public float Spacing =>
        spacing;


    // =====================================================
    // INITIALIZATION
    // =====================================================

    void Awake()
    {
        Instance = this;


        /*
         * EXTREMELY IMPORTANT.
         *
         * HOME is defined as WORLD X = 0.
         *
         * Whatever accidental transform this empty object
         * had in the editor is ignored.
         */

        transform.position =
            Vector3.zero;


        transform.rotation =
            Quaternion.identity;


        transform.localScale =
            Vector3.one;
    }


    void Start()
    {
        FindCamera();

        CreateRoot();

        CreateMainLine();

        ResetNumberLine();
    }


    void Update()
    {
        if (cameraTransform == null)
        {
            FindCamera();
        }


        UpdateCameraRange();
    }


    // =====================================================
    // ROOT
    // =====================================================

    void CreateRoot()
    {
        GameObject rootObject =
            new GameObject(
                "Generated Number Line"
            );


        rootObject.transform.SetParent(
            transform,
            false
        );


        generatedRoot =
            rootObject.transform;
    }


    // =====================================================
    // RESET
    // =====================================================

    public void ResetNumberLine()
    {
        minValue =
            initialMinValue;


        maxValue =
            initialMaxValue;


        /*
         * Hide any ticks created during an earlier
         * expanded experiment.
         */

        foreach (
            KeyValuePair<int, GameObject> pair
            in ticks
        )
        {
            bool shouldBeVisible =
                pair.Key >= minValue
                && pair.Key <= maxValue;


            pair.Value.SetActive(
                shouldBeVisible
            );
        }


        foreach (
            KeyValuePair<int, GameObject> pair
            in labels
        )
        {
            bool shouldBeVisible =
                pair.Key >= minValue
                && pair.Key <= maxValue;


            pair.Value.SetActive(
                shouldBeVisible
            );
        }


        // Make sure initial ticks exist.
        for (
            int i = minValue;
            i <= maxValue;
            i++
        )
        {
            ShowOrCreateTick(i);

            ShowOrCreateLabel(i);
        }


        if (homeLabelObject != null)
        {
            homeLabelObject.SetActive(
                true
            );
        }


        UpdateMainLine();
    }


    // =====================================================
    // CAMERA
    // =====================================================

    void FindCamera()
    {
        if (Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;

            return;
        }


        Camera foundCamera =
            FindFirstObjectByType<Camera>();


        if (foundCamera != null)
        {
            cameraTransform =
                foundCamera.transform;
        }
    }


    void UpdateCameraRange()
    {
        if (cameraTransform == null)
        {
            return;
        }


        int cameraPosition =
            Mathf.RoundToInt(
                cameraTransform.position.x
                / spacing
            );


        EnsureRange(
            cameraPosition
                - cameraPreloadRadius,

            cameraPosition
                + cameraPreloadRadius
        );
    }


    // =====================================================
    // WALKER RANGE
    // =====================================================

    public void EnsureVisible(
        int agentPosition
    )
    {
        EnsureRange(
            agentPosition
                - agentBuffer,

            agentPosition
                + agentBuffer
        );
    }


    // =====================================================
    // RANGE MANAGEMENT
    // =====================================================

    void EnsureRange(
        int requiredMin,
        int requiredMax
    )
    {
        while (requiredMin < minValue)
        {
            ExtendLeft();
        }


        while (requiredMax > maxValue)
        {
            ExtendRight();
        }
    }


    void ExtendRight()
    {
        int oldMax =
            maxValue;


        maxValue +=
            expansionAmount;


        for (
            int i = oldMax + 1;
            i <= maxValue;
            i++
        )
        {
            ShowOrCreateTick(i);

            ShowOrCreateLabel(i);
        }


        UpdateMainLine();
    }


    void ExtendLeft()
    {
        int oldMin =
            minValue;


        minValue -=
            expansionAmount;


        for (
            int i = minValue;
            i < oldMin;
            i++
        )
        {
            ShowOrCreateTick(i);

            ShowOrCreateLabel(i);
        }


        UpdateMainLine();
    }


    // =====================================================
    // MAIN LINE
    // =====================================================

    void CreateMainLine()
    {
        mainLine =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );


        mainLine.name =
            "Main Line";


        mainLine.transform.SetParent(
            generatedRoot,
            false
        );


        Renderer renderer =
            mainLine.GetComponent<Renderer>();


        renderer.material.color =
            lineColor;


        Destroy(
            mainLine.GetComponent<Collider>()
        );
    }


    void UpdateMainLine()
    {
        float length =
            (maxValue - minValue)
            * spacing;


        float centerX =
            (minValue + maxValue)
            * spacing
            / 2f;


        mainLine.transform.localPosition =
            new Vector3(
                centerX,
                0f,
                0f
            );


        mainLine.transform.localScale =
            new Vector3(
                length,
                lineThickness,
                lineThickness
            );
    }


    // =====================================================
    // TICKS
    // =====================================================

    void ShowOrCreateTick(
        int value
    )
    {
        if (ticks.TryGetValue(
            value,
            out GameObject existingTick
        ))
        {
            existingTick.SetActive(
                true
            );

            return;
        }


        GameObject tick =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );


        tick.name =
            "Tick " + value;


        tick.transform.SetParent(
            generatedRoot,
            false
        );


        tick.transform.localPosition =
            new Vector3(
                value * spacing,
                0f,
                0f
            );


        if (value == 0)
        {
            tick.transform.localScale =
                new Vector3(
                    tickThickness * 1.8f,
                    lineThickness * 1.5f,
                    tickLength * 1.6f
                );
        }
        else
        {
            tick.transform.localScale =
                new Vector3(
                    tickThickness,
                    lineThickness,
                    tickLength
                );
        }


        Renderer renderer =
            tick.GetComponent<Renderer>();


        renderer.material.color =
            value == 0
            ? homeColor
            : lineColor;


        Destroy(
            tick.GetComponent<Collider>()
        );


        ticks.Add(
            value,
            tick
        );
    }


    // =====================================================
    // NUMBER LABELS
    // =====================================================

    void ShowOrCreateLabel(
        int value
    )
    {
        if (labels.TryGetValue(
            value,
            out GameObject existingLabel
        ))
        {
            existingLabel.SetActive(
                true
            );

            return;
        }


        GameObject labelObject =
            new GameObject(
                "Label " + value
            );


        labelObject.transform.SetParent(
            generatedRoot,
            false
        );


        TextMeshPro label =
            labelObject.AddComponent<TextMeshPro>();


        label.text =
            value.ToString();


        label.fontSize =
            labelSize;


        label.alignment =
            TextAlignmentOptions.Center;


        label.color =
            value == 0
            ? homeColor
            : lineColor;


        labelObject.transform.localPosition =
            new Vector3(
                value * spacing,
                0.02f,
                -0.5f
            );


        labelObject.transform.localRotation =
            Quaternion.Euler(
                90f,
                0f,
                0f
            );


        labels.Add(
            value,
            labelObject
        );


        if (
            value == 0
            && homeLabelObject == null
        )
        {
            CreateHomeLabel();
        }
    }


    // =====================================================
    // HOME LABEL
    // =====================================================

    void CreateHomeLabel()
    {
        homeLabelObject =
            new GameObject(
                "Home Label"
            );


        homeLabelObject.transform.SetParent(
            generatedRoot,
            false
        );


        TextMeshPro homeLabel =
            homeLabelObject.AddComponent<TextMeshPro>();


        homeLabel.text =
            "HOME";


        homeLabel.fontSize =
            labelSize * 1.2f;


        homeLabel.fontStyle =
            FontStyles.Bold;


        homeLabel.alignment =
            TextAlignmentOptions.Center;


        homeLabel.color =
            homeColor;


        homeLabelObject.transform.localPosition =
            new Vector3(
                0f,
                0.02f,
                -0.82f
            );


        homeLabelObject.transform.localRotation =
            Quaternion.Euler(
                90f,
                0f,
                0f
            );
    }


    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}