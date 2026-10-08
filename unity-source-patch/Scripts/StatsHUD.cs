using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatsHUD : MonoBehaviour
{
    public static StatsHUD Instance { get; private set; }


    // =====================================================
    // EXPERIMENT DATA
    // =====================================================

    private int totalWalkers = 0;

    private int returnedWalkers = 0;

    private int exhaustedWalkers = 0;

    private int currentMoveLimit = 0;


    // =====================================================
    // MAIN HUD
    // =====================================================

    private TextMeshProUGUI statsText;

    private RectTransform hudRect;


    // =====================================================
    // COMPLETION BANNER
    // =====================================================

    private GameObject completionPanel;

    private RectTransform completionRect;

    private CanvasGroup completionCanvasGroup;

    private TextMeshProUGUI completionTitle;

    private TextMeshProUGUI completionBody;

    private TextMeshProUGUI completionPrompt;


    // =====================================================
    // ANIMATION
    // =====================================================

    private Coroutine hudPopCoroutine;

    private Coroutine completionCoroutine;


    // =====================================================
    // COLORS
    // =====================================================

    private readonly Color accentColor =
        new Color(
            1f,
            0.35f,
            0.95f,
            1f
        );


    private readonly Color backgroundColor =
        new Color(
            0.03f,
            0.02f,
            0.06f,
            0.78f
        );


    private readonly Color completionAccentColor =
        new Color(
            1f,
            0.72f,
            0.12f,
            1f
        );


    // =====================================================
    // INITIALIZATION
    // =====================================================

    void Awake()
    {
        Instance = this;

        CreateHUD();

        UpdateDisplay();
    }


    // =====================================================
    // CREATE HUD
    // =====================================================

    void CreateHUD()
    {
        // -------------------------------------------------
        // CANVAS
        // -------------------------------------------------

        GameObject canvasObject =
            new GameObject(
                "Stats Canvas"
            );


        canvasObject.transform.SetParent(
            transform,
            false
        );


        Canvas canvas =
            canvasObject.AddComponent<Canvas>();


        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;


        CanvasScaler scaler =
            canvasObject.AddComponent<CanvasScaler>();


        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;


        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f
            );


        scaler.matchWidthOrHeight =
            0.5f;


        canvasObject.AddComponent<GraphicRaycaster>();


        // -------------------------------------------------
        // MAIN HUD BACKGROUND
        // -------------------------------------------------

        GameObject backgroundObject =
            new GameObject(
                "HUD Background"
            );


        backgroundObject.transform.SetParent(
            canvasObject.transform,
            false
        );


        Image background =
            backgroundObject.AddComponent<Image>();


        background.color =
            backgroundColor;


        hudRect =
            backgroundObject.GetComponent<RectTransform>();


        hudRect.anchorMin =
            new Vector2(
                0f,
                1f
            );


        hudRect.anchorMax =
            new Vector2(
                0f,
                1f
            );


        hudRect.pivot =
            new Vector2(
                0f,
                1f
            );


        hudRect.anchoredPosition =
            new Vector2(
                22f,
                -22f
            );


        hudRect.sizeDelta =
            new Vector2(
                500f,
                175f
            );


        // -------------------------------------------------
        // MAGENTA STRIPE
        // -------------------------------------------------

        GameObject stripeObject =
            new GameObject(
                "Accent Stripe"
            );


        stripeObject.transform.SetParent(
            backgroundObject.transform,
            false
        );


        Image stripe =
            stripeObject.AddComponent<Image>();


        stripe.color =
            accentColor;


        RectTransform stripeRect =
            stripeObject.GetComponent<RectTransform>();


        stripeRect.anchorMin =
            new Vector2(
                0f,
                0f
            );


        stripeRect.anchorMax =
            new Vector2(
                0f,
                1f
            );


        stripeRect.pivot =
            new Vector2(
                0f,
                0.5f
            );


        stripeRect.anchoredPosition =
            Vector2.zero;


        stripeRect.sizeDelta =
            new Vector2(
                6f,
                0f
            );


        // -------------------------------------------------
        // MAIN STATS TEXT
        // -------------------------------------------------

        GameObject textObject =
            new GameObject(
                "Stats Text"
            );


        textObject.transform.SetParent(
            backgroundObject.transform,
            false
        );


        statsText =
            textObject.AddComponent<TextMeshProUGUI>();


        statsText.fontSize =
            31f;


        statsText.fontStyle =
            FontStyles.Bold;


        statsText.alignment =
            TextAlignmentOptions.TopLeft;


        statsText.textWrappingMode =
            TextWrappingModes.NoWrap;


        statsText.outlineColor =
            new Color32(
                0,
                0,
                0,
                230
            );


        statsText.outlineWidth =
            0.18f;


        RectTransform textRect =
            statsText.GetComponent<RectTransform>();


        textRect.anchorMin =
            Vector2.zero;


        textRect.anchorMax =
            Vector2.one;


        textRect.offsetMin =
            new Vector2(
                24f,
                12f
            );


        textRect.offsetMax =
            new Vector2(
                -18f,
                -12f
            );


        // -------------------------------------------------
        // COMPLETION BANNER
        // -------------------------------------------------

        CreateCompletionBanner(
            canvasObject.transform
        );
    }


    // =====================================================
    // CREATE COMPLETION BANNER
    // =====================================================

    void CreateCompletionBanner(
        Transform canvasTransform
    )
    {
        completionPanel =
            new GameObject(
                "Move Limit Banner"
            );


        completionPanel.transform.SetParent(
            canvasTransform,
            false
        );


        Image background =
            completionPanel.AddComponent<Image>();


        background.color =
            new Color(
                0.025f,
                0.02f,
                0.035f,
                0.94f
            );


        completionRect =
            completionPanel.GetComponent<RectTransform>();


        // Top-center
        completionRect.anchorMin =
            new Vector2(
                0.5f,
                1f
            );


        completionRect.anchorMax =
            new Vector2(
                0.5f,
                1f
            );


        completionRect.pivot =
            new Vector2(
                0.5f,
                1f
            );


        completionRect.anchoredPosition =
            new Vector2(
                0f,
                -30f
            );


        // Plenty of space now.
        completionRect.sizeDelta =
            new Vector2(
                820f,
                205f
            );


        completionCanvasGroup =
            completionPanel.AddComponent<CanvasGroup>();


        completionCanvasGroup.alpha =
            0f;


        // -------------------------------------------------
        // GOLD BOTTOM STRIPE
        // -------------------------------------------------

        GameObject stripeObject =
            new GameObject(
                "Gold Stripe"
            );


        stripeObject.transform.SetParent(
            completionPanel.transform,
            false
        );


        Image stripe =
            stripeObject.AddComponent<Image>();


        stripe.color =
            completionAccentColor;


        RectTransform stripeRect =
            stripeObject.GetComponent<RectTransform>();


        stripeRect.anchorMin =
            new Vector2(
                0f,
                0f
            );


        stripeRect.anchorMax =
            new Vector2(
                1f,
                0f
            );


        stripeRect.pivot =
            new Vector2(
                0.5f,
                0f
            );


        stripeRect.anchoredPosition =
            Vector2.zero;


        stripeRect.sizeDelta =
            new Vector2(
                0f,
                6f
            );


        // -------------------------------------------------
        // TITLE
        // -------------------------------------------------

        completionTitle =
            CreateBannerText(
                "Completion Title",
                completionPanel.transform,
                31f,
                FontStyles.Bold,
                completionAccentColor,
                new Vector2(0f, -18f),
                new Vector2(760f, 42f)
            );


        completionTitle.text =
            "MOVE LIMIT REACHED";


        // -------------------------------------------------
        // BODY
        // -------------------------------------------------

        completionBody =
            CreateBannerText(
                "Completion Body",
                completionPanel.transform,
                24f,
                FontStyles.Bold,
                new Color(
                    0.96f,
                    0.96f,
                    1f,
                    1f
                ),
                new Vector2(0f, -71f),
                new Vector2(750f, 58f)
            );


        completionBody.textWrappingMode =
            TextWrappingModes.Normal;


        // -------------------------------------------------
        // PROMPT
        // -------------------------------------------------

        completionPrompt =
            CreateBannerText(
                "Completion Prompt",
                completionPanel.transform,
                20f,
                FontStyles.Normal,
                new Color(
                    0.74f,
                    0.72f,
                    0.80f,
                    1f
                ),
                new Vector2(0f, -143f),
                new Vector2(750f, 32f)
            );


        completionPrompt.text =
            "PRESS R TO RUN A NEW EXPERIMENT";


        completionPanel.SetActive(
            false
        );
    }


    // =====================================================
    // HELPER: CREATE BANNER TEXT
    // =====================================================

    TextMeshProUGUI CreateBannerText(
        string objectName,
        Transform parent,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject textObject =
            new GameObject(
                objectName
            );


        textObject.transform.SetParent(
            parent,
            false
        );


        TextMeshProUGUI text =
            textObject.AddComponent<TextMeshProUGUI>();


        text.fontSize =
            fontSize;


        text.fontStyle =
            fontStyle;


        text.color =
            color;


        text.alignment =
            TextAlignmentOptions.Center;


        text.outlineColor =
            new Color32(
                0,
                0,
                0,
                220
            );


        text.outlineWidth =
            0.12f;


        RectTransform rect =
            text.GetComponent<RectTransform>();


        rect.anchorMin =
            new Vector2(
                0.5f,
                1f
            );


        rect.anchorMax =
            new Vector2(
                0.5f,
                1f
            );


        rect.pivot =
            new Vector2(
                0.5f,
                1f
            );


        rect.anchoredPosition =
            anchoredPosition;


        rect.sizeDelta =
            size;


        return text;
    }


    // =====================================================
    // RESET EXPERIMENT DATA
    // =====================================================

    public void SetTotalWalkers(
        int total
    )
    {
        totalWalkers =
            Mathf.Max(
                0,
                total
            );


        returnedWalkers =
            0;


        exhaustedWalkers =
            0;


        currentMoveLimit =
            0;


        if (hudPopCoroutine != null)
        {
            StopCoroutine(
                hudPopCoroutine
            );

            hudPopCoroutine = null;
        }


        if (completionCoroutine != null)
        {
            StopCoroutine(
                completionCoroutine
            );

            completionCoroutine = null;
        }


        if (hudRect != null)
        {
            hudRect.localScale =
                Vector3.one;
        }


        HideCompletionBanner();

        UpdateDisplay();
    }


    // =====================================================
    // WALKER RETURNED
    // =====================================================

    public void WalkerReturned()
    {
        // Defensive guard.
        if (
            returnedWalkers
            + exhaustedWalkers
            >= totalWalkers
        )
        {
            return;
        }


        returnedWalkers++;


        UpdateDisplay();


        if (hudPopCoroutine != null)
        {
            StopCoroutine(
                hudPopCoroutine
            );
        }


        hudPopCoroutine =
            StartCoroutine(
                PopAnimation()
            );


        CheckExperimentComplete();
    }


    // =====================================================
    // WALKER HIT MOVE LIMIT
    // =====================================================

    public void WalkerReachedMoveLimit(
        int moveLimit
    )
    {
        // Defensive guard against extra reports.
        if (
            returnedWalkers
            + exhaustedWalkers
            >= totalWalkers
        )
        {
            return;
        }


        exhaustedWalkers++;


        currentMoveLimit =
            moveLimit;


        CheckExperimentComplete();
    }


    // =====================================================
    // EXPERIMENT COMPLETION
    // =====================================================

    void CheckExperimentComplete()
    {
        if (totalWalkers <= 0)
            return;


        int completed =
            returnedWalkers
            + exhaustedWalkers;


        if (completed < totalWalkers)
            return;


        /*
         * Only show MOVE LIMIT REACHED if at least
         * one walker actually failed to return.
         */
        if (exhaustedWalkers > 0)
        {
            ShowCompletionBanner();
        }
    }


    // =====================================================
    // SHOW COMPLETION BANNER
    // =====================================================

    void ShowCompletionBanner()
    {
        completionPanel.SetActive(
            true
        );


        string walkerWord =
            exhaustedWalkers == 1
            ? "WALKER"
            : "WALKERS";


        completionBody.text =
            exhaustedWalkers
            + " "
            + walkerWord
            + " DID NOT RETURN\nWITHIN "
            + currentMoveLimit.ToString("N0")
            + " MOVES";


        completionPrompt.text =
            "PRESS R TO RUN A NEW EXPERIMENT";


        if (completionCoroutine != null)
        {
            StopCoroutine(
                completionCoroutine
            );
        }


        completionCoroutine =
            StartCoroutine(
                AnimateCompletionBanner()
            );
    }


    // =====================================================
    // HIDE COMPLETION BANNER
    // =====================================================

    void HideCompletionBanner()
    {
        if (completionPanel == null)
            return;


        completionCanvasGroup.alpha =
            0f;


        completionRect.localScale =
            Vector3.one;


        completionPanel.SetActive(
            false
        );
    }


    // =====================================================
    // MAIN HUD DISPLAY
    // =====================================================

    void UpdateDisplay()
    {
        float probability =
            0f;


        if (totalWalkers > 0)
        {
            probability =
                (float)returnedWalkers
                / totalWalkers;
        }


        string accentHex =
            ColorUtility.ToHtmlStringRGB(
                accentColor
            );


        statsText.text =
            "<color=#F7F4FF>TOTAL WALKERS</color>   "
            +
            "<color=#"
            + accentHex
            + ">"
            + totalWalkers
            + "</color>"

            + "\n"

            + "<color=#F7F4FF>RETURNED HOME</color>  "
            +
            "<color=#"
            + accentHex
            + ">"
            + returnedWalkers
            + "</color>"

            + "\n"

            + "<color=#F7F4FF>RETURN RATE</color>    "
            +
            "<color=#"
            + accentHex
            + ">"
            + (probability * 100f).ToString("F1")
            + "%</color>";
    }


    // =====================================================
    // HUD POP ANIMATION
    // =====================================================

    IEnumerator PopAnimation()
    {
        Vector3 normalScale =
            Vector3.one;


        Vector3 largeScale =
            Vector3.one
            * 1.055f;


        float duration =
            0.10f;


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                elapsed
                / duration;


            hudRect.localScale =
                Vector3.Lerp(
                    normalScale,
                    largeScale,
                    t
                );


            yield return null;
        }


        elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                elapsed
                / duration;


            hudRect.localScale =
                Vector3.Lerp(
                    largeScale,
                    normalScale,
                    t
                );


            yield return null;
        }


        hudRect.localScale =
            normalScale;


        hudPopCoroutine =
            null;
    }


    // =====================================================
    // COMPLETION BANNER ANIMATION
    // =====================================================

    IEnumerator AnimateCompletionBanner()
    {
        completionCanvasGroup.alpha =
            0f;


        completionRect.localScale =
            Vector3.one * 0.9f;


        float duration =
            0.25f;


        float elapsed =
            0f;


        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed
                    / duration
                );


            float eased =
                1f
                - Mathf.Pow(
                    1f - t,
                    3f
                );


            completionCanvasGroup.alpha =
                eased;


            completionRect.localScale =
                Vector3.Lerp(
                    Vector3.one * 0.9f,
                    Vector3.one,
                    eased
                );


            yield return null;
        }


        completionCanvasGroup.alpha =
            1f;


        completionRect.localScale =
            Vector3.one;


        completionCoroutine =
            null;
    }
}