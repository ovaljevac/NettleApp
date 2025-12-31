using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// ✅ Stavi ovu skriptu na DŽEZVU (objekat mora imati Collider!)
// ✅ AR Camera treba biti Tag = MainCamera
// ✅ EventSystem treba postojati u sceni (UI -> Event System)
// ✅ Video panel: WORLD SPACE, automatski iznad VRHA džežve (ne upada u stol) + manji
public class POIVideoAllInOne : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public VideoClip videoClip;
    public Sprite closeButtonIcon;

    [Header("Auto placement (recommended)")]
    public bool autoPlaceAboveObject = true;
    public float extraHeight = 0.06f;      // koliko iznad vrha džežve
    public float forwardOffset = 0.12f;    // malo prema kameri

    [Header("Manual placement (used only if autoPlaceAboveObject = false)")]
    public Vector3 panelLocalPosition = new Vector3(0f, 0.30f, 0.12f);

    [Header("World-space size (smaller = better)")]
    public float panelWorldScale = 0.002f;
    public Vector2 canvasSize = new Vector2(400, 225);
    public Vector2 rawImageSize = new Vector2(380, 200);
    public Vector2 closeButtonSize = new Vector2(50, 50);

    [Header("Video")]
    public bool loop = false;

    [Header("UI Look")]
    [Range(0f, 1f)] public float backgroundAlpha = 0.85f;

    [Header("Animation")]
    public bool animatePanel = true;
    public float animDuration = 0.18f;
    public float hiddenScale = 0.85f;
    public float shownScale = 1f;

    private Canvas canvas;
    private GameObject canvasGO;
    private GameObject panelGO;
    private RawImage rawImage;
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private bool uiCreated = false;

    private CanvasGroup panelCanvasGroup;
    private Coroutine animRoutine;

    void Update()
    {
        if (Camera.main == null) return;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                TryHitPOI(touch.position.ReadValue());
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryHitPOI(Mouse.current.position.ReadValue());
#else
        if (Input.GetMouseButtonDown(0)) TryHitPOI(Input.mousePosition);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) TryHitPOI(Input.GetTouch(0).position);
#endif
    }

    private void TryHitPOI(Vector2 screenPos)
    {
        // Ako klikneš UI, ne raycastaj u 3D
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                PlayVideo();
        }
    }

    private void PlayVideo()
    {
        if (videoClip == null)
        {
            Debug.LogError("POIVideoAllInOne: VideoClip nije dodijeljen u Inspectoru.");
            return;
        }

        if (!EnsureUI())
        {
            Debug.LogError("POIVideoAllInOne: UI nije mogao biti kreiran (panelGO/videoPlayer null).");
            return;
        }

        // Video start
        videoPlayer.Stop();
        videoPlayer.Play();

        // Panel open
        if (animatePanel && panelCanvasGroup != null)
            OpenPanelAnimated();
        else
        {
            panelGO.SetActive(true);
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }
            panelGO.transform.localScale = Vector3.one * shownScale;
        }
    }

    public void CloseVideo()
    {
        if (!animatePanel || panelCanvasGroup == null)
        {
            if (videoPlayer != null) videoPlayer.Stop();
            if (panelGO != null) panelGO.SetActive(false);
            return;
        }

        ClosePanelAnimated();
    }

    private bool EnsureUI()
    {
        if (uiCreated && panelGO != null && videoPlayer != null) return true;

        CreateVideoUI();
        uiCreated = (panelGO != null && videoPlayer != null);

        // početno skriveno
        if (panelGO != null)
        {
            panelGO.SetActive(false);
            panelGO.transform.localScale = Vector3.one * hiddenScale;
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
        }

        return uiCreated;
    }

    private void CreateVideoUI()
    {
        if (panelGO != null) return;

        // =========================
        // WORLD SPACE CANVAS
        // =========================
        canvasGO = new GameObject("VideoCanvas_World");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Parent: džežva
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * panelWorldScale;

        // Auto placement: iznad VRHA džežve
        if (autoPlaceAboveObject)
        {
            float yTop = 0f;

            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null)
                yTop = transform.InverseTransformPoint(r.bounds.max).y;

            Collider c = GetComponentInChildren<Collider>();
            if (c != null)
            {
                float colTop = transform.InverseTransformPoint(c.bounds.max).y;
                if (colTop > yTop) yTop = colTop;
            }

            canvasGO.transform.localPosition = new Vector3(0f, yTop + extraHeight, forwardOffset);
        }
        else
        {
            canvasGO.transform.localPosition = panelLocalPosition;
        }

        // Billboard: uvijek gleda kameru
        // (moraš imati svoju BillboardToCamera skriptu u projektu)
        canvasGO.AddComponent<BillboardToCamera>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = canvasSize;

        // =========================
        // Panel
        // =========================
        panelGO = new GameObject("VideoPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, backgroundAlpha);

        // CanvasGroup za animaciju (alpha + klikovi)
        panelCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = canvasSize;
        panelRT.anchoredPosition = Vector2.zero;

        panelGO.transform.localScale = Vector3.one * hiddenScale;

        // =========================
        // RawImage (video)
        // =========================
        GameObject rawGO = new GameObject("VideoRawImage");
        rawGO.transform.SetParent(panelGO.transform, false);
        rawImage = rawGO.AddComponent<RawImage>();

        RectTransform rawRT = rawGO.GetComponent<RectTransform>();
        rawRT.anchorMin = new Vector2(0.5f, 0.5f);
        rawRT.anchorMax = new Vector2(0.5f, 0.5f);
        rawRT.pivot = new Vector2(0.5f, 0.5f);
        rawRT.sizeDelta = rawImageSize;
        rawRT.anchoredPosition = Vector2.zero;

        // RenderTexture
        renderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
        renderTexture.name = "VideoRT_Runtime";
        rawImage.texture = renderTexture;

        // VideoPlayer
        videoPlayer = rawGO.AddComponent<VideoPlayer>();
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = loop;
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;

        // Audio
        AudioSource audio = rawGO.AddComponent<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.SetTargetAudioSource(0, audio);

        // =========================
        // Close button (X)
        // =========================
        GameObject btnGO = new GameObject("CloseButton");
        btnGO.transform.SetParent(panelGO.transform, false);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = closeButtonIcon;
        btnImg.preserveAspect = true;
        btnImg.color = Color.white;

        Button closeBtn = btnGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseVideo);

        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 1f);
        btnRT.anchorMax = new Vector2(1f, 1f);
        btnRT.pivot = new Vector2(1f, 1f);
        btnRT.sizeDelta = new Vector2(70, 70);
        btnRT.anchoredPosition = new Vector2(-10, -10);
    }

    // =========================
    // Animation helpers
    // =========================
    private void OpenPanelAnimated()
    {
        if (panelGO == null) return;

        panelGO.SetActive(true);

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimatePanel(open: true));
    }

    private void ClosePanelAnimated()
    {
        if (panelGO == null) return;

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimatePanel(open: false));
    }

    private System.Collections.IEnumerator AnimatePanel(bool open)
    {
        if (panelCanvasGroup == null) yield break;

        float t = 0f;

        float startAlpha = panelCanvasGroup.alpha;
        float endAlpha = open ? 1f : 0f;

        float startScale = panelGO.transform.localScale.x;
        float endScale = open ? shownScale : hiddenScale;

        // klikovi samo kad je otvoreno
        panelCanvasGroup.interactable = open;
        panelCanvasGroup.blocksRaycasts = open;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / animDuration);

            // SmoothStep easing
            p = p * p * (3f - 2f * p);

            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, p);

            float s = Mathf.Lerp(startScale, endScale, p);
            panelGO.transform.localScale = Vector3.one * s;

            yield return null;
        }

        panelCanvasGroup.alpha = endAlpha;
        panelGO.transform.localScale = Vector3.one * endScale;

        if (!open)
        {
            if (videoPlayer != null) videoPlayer.Stop();
            panelGO.SetActive(false);
        }

        animRoutine = null;
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }
}
