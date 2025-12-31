using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class POIPhotoAndRecipe : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Sprite recipePhoto;
    public TextAsset recipeTextFile;
    public Sprite closeButtonIcon;

    [Header("POI Anchor (IMPORTANT)")]
    [Tooltip("Ovde prevuci TAČAN objekt kalupa (mesh/collider) iznad kojeg treba panel.")]
    public Transform poiAnchor;

    [Header("Auto placement")]
    public bool autoPlaceAboveObject = true;
    public float extraHeight = 0.03f;     // iznad vrha POI
    public float forwardOffset = 0.10f;   // prema kameri

    [Header("Manual placement (if autoPlaceAboveObject = false)")]
    public Vector3 panelLocalPosition = new Vector3(0f, 0.35f, 0.15f);

    [Header("World-space size")]
    public float panelWorldScale = 0.0004f;

    [Header("UI Look")]
    [Range(0f, 1f)] public float backgroundAlpha = 0.85f;

    [Header("Panel size (canvas space pixels)")]
    public Vector2 panelSize = new Vector2(400, 225);
    public Vector2 imageSize = new Vector2(180, 180);

    [Header("Text")]
    public int fontSize = 10;
    public bool bestFit = true;
    public int bestFitMin = 10;
    public int bestFitMax = 10;

    [Header("Animation")]
    public bool animatePanel = true;
    public float animDuration = 0.18f;
    public float hiddenScale = 0.85f;
    public float shownScale = 1f;

    private GameObject canvasGO;
    private GameObject panelGO;
    private CanvasGroup panelCanvasGroup;

    private bool uiCreated;
    private Coroutine animRoutine;

    void Reset()
    {
        // Ako si skriptu stavila direktno na POI, ovo će automatski setovati anchor
        if (poiAnchor == null) poiAnchor = transform;
    }

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
        // Ne okidaj kad klikneš UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            // ✅ POI provjera: ako si anchor postavila, klik mora pogoditi anchor ili njegovo dijete
            Transform anchor = (poiAnchor != null) ? poiAnchor : transform;

            if (hit.transform == anchor || hit.transform.IsChildOf(anchor))
                ShowPanel();
        }
    }

    private void ShowPanel()
    {
        if (!EnsureUI()) return;

        if (animatePanel && panelCanvasGroup != null)
            OpenPanelAnimated();
        else
            ShowInstant();
    }

    private void ShowInstant()
    {
        panelGO.SetActive(true);
        panelCanvasGroup.alpha = 1f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
        panelGO.transform.localScale = Vector3.one * shownScale;
    }

    public void ClosePanel()
    {
        if (!animatePanel || panelCanvasGroup == null)
        {
            if (panelGO != null) panelGO.SetActive(false);
            return;
        }

        ClosePanelAnimated();
    }

    private bool EnsureUI()
    {
        if (uiCreated && panelGO != null) return true;

        CreateUI();
        uiCreated = (panelGO != null);

        if (panelGO != null)
        {
            panelGO.SetActive(false);
            panelGO.transform.localScale = Vector3.one * hiddenScale;

            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        return uiCreated;
    }

    private void CreateUI()
    {
        Transform anchor = (poiAnchor != null) ? poiAnchor : transform;

    // Canvas (World Space)
    canvasGO = new GameObject("RecipeCanvas_World");
    Canvas canvas = canvasGO.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.WorldSpace;

    canvasGO.AddComponent<GraphicRaycaster>();

    CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

    // ✅ Parent FIRST (false = local space), onda sve radiš u local
    canvasGO.transform.SetParent(anchor, false);
    canvasGO.transform.localRotation = Quaternion.identity;
    canvasGO.transform.localScale = Vector3.one * panelWorldScale;

    // ✅ Pozicioniranje: LOCAL space iznad POI (ne world)
    // ✅ Pozicioniranje:
    if (autoPlaceAboveObject)
{
    // 1. Prvo otkači panel sa parenta privremeno da bi World Position bio precizan
    canvasGO.transform.SetParent(null); 

    Bounds b;
    if (!TryGetBoundsFromAnchor(anchor, out b))
        b = new Bounds(anchor.position, Vector3.one * 0.1f);

    // 2. Izračunaj poziciju strogo u World prostoru
    // b.max.y je apsolutni vrh kolajdera u svijetu
    float yPos = b.max.y + extraHeight;
    Vector3 worldTopPos = new Vector3(b.center.x, yPos, b.center.z);

    if (Camera.main != null)
    {
        Vector3 dirToCam = (Camera.main.transform.position - worldTopPos).normalized;
        worldTopPos += dirToCam * forwardOffset;
    }

    // 3. Dodijeli poziciju
    canvasGO.transform.position = worldTopPos;

    // 4. VRATI ga pod anchor, ali sa 'worldPositionStays: true'
    // Ovo sprečava Unity da "skakuće" sa koordinatama
    canvasGO.transform.SetParent(anchor, true);
}
    else
    {
        canvasGO.transform.localPosition = panelLocalPosition;
    }
    // Billboard
    canvasGO.AddComponent<BillboardToCamera>();

    // ✅ RectTransform: samo size i pivot, NE diraj anchoredPosition3D
    RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
    canvasRT.sizeDelta = panelSize;
    canvasRT.pivot = new Vector2(0.5f, 0.5f);

        // Panel
        panelGO = new GameObject("RecipePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, backgroundAlpha);

        panelCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        RectTransform panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = panelSize;
        panelRT.anchoredPosition = Vector2.zero;

        panelGO.transform.localScale = Vector3.one * hiddenScale;

        // Photo (lijevo)
        GameObject photoGO = new GameObject("Photo");
        photoGO.transform.SetParent(panelGO.transform, false);

        Image photoImg = photoGO.AddComponent<Image>();
        photoImg.sprite = recipePhoto;
        photoImg.preserveAspect = true;
        photoImg.color = Color.white;

        RectTransform photoRT = photoGO.GetComponent<RectTransform>();
        photoRT.anchorMin = new Vector2(0f, 0.5f);
        photoRT.anchorMax = new Vector2(0f, 0.5f);
        photoRT.pivot = new Vector2(0f, 0.5f);
        photoRT.sizeDelta = imageSize;
        photoRT.anchoredPosition = new Vector2(20f, 0f);

        // ScrollView (desno)
        GameObject scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(panelGO.transform, false);

        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.06f);

        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = new Vector2(20f + imageSize.x + 12f, 20f);
        scrollRT.offsetMax = new Vector2(-20f, -60f);

        Mask mask = scrollGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        // Content
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);

        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0f, 10f);

        scroll.content = contentRT;
        scroll.viewport = scrollRT;

        // Text
        GameObject textGO = new GameObject("RecipeText");
        textGO.transform.SetParent(contentGO.transform, false);

        Text t = textGO.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.color = Color.white;

        if (bestFit)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = bestFitMin;
            t.resizeTextMaxSize = bestFitMax;
        }

        string recipe = (recipeTextFile != null) ? recipeTextFile.text : "(Nema dodijeljenog Recept.txt)";
        t.text = "MUFFINI S KOPRIVOM\n\n" + recipe;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 1f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.pivot = new Vector2(0f, 1f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(0f, 10f);

        // Auto visina content-a (da scroll radi)
        Canvas.ForceUpdateCanvases();
        float preferredH = Mathf.Max(t.preferredHeight, scrollRT.rect.height);
        contentRT.sizeDelta = new Vector2(0f, preferredH);
        textRT.sizeDelta = new Vector2(0f, preferredH);

        // Close button
        GameObject btnGO = new GameObject("CloseButton");
        btnGO.transform.SetParent(panelGO.transform, false);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.sprite = closeButtonIcon;
        btnImg.preserveAspect = true;
        btnImg.color = Color.white;

        Button closeBtn = btnGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(ClosePanel);

        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 1f);
        btnRT.anchorMax = new Vector2(1f, 1f);
        btnRT.pivot = new Vector2(1f, 1f);
        btnRT.sizeDelta = new Vector2(55, 55);
        btnRT.anchoredPosition = new Vector2(-8, -8);
    }

    private bool TryGetBoundsFromAnchor(Transform anchor, out Bounds bounds)
    {
        // ✅ UZMI collider/renderer SAMO sa anchora (ne “InChildren” nasumično)
        Collider col = anchor.GetComponent<Collider>();
        if (col != null)
        {
            bounds = col.bounds;
            return true;
        }

        Renderer rend = anchor.GetComponent<Renderer>();
        if (rend != null)
        {
            bounds = rend.bounds;
            return true;
        }

        // Ako anchor nema direktno, onda pokušaj u djeci, ali samo unutar anchora
        col = anchor.GetComponentInChildren<Collider>();
        if (col != null)
        {
            bounds = col.bounds;
            return true;
        }

        rend = anchor.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            bounds = rend.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    // Animation
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
        float t = 0f;

        float startAlpha = panelCanvasGroup.alpha;
        float endAlpha = open ? 1f : 0f;

        float startScale = panelGO.transform.localScale.x;
        float endScale = open ? shownScale : hiddenScale;

        panelCanvasGroup.interactable = open;
        panelCanvasGroup.blocksRaycasts = open;

        while (t < animDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / animDuration);
            p = p * p * (3f - 2f * p); // SmoothStep

            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, p);

            float s = Mathf.Lerp(startScale, endScale, p);
            panelGO.transform.localScale = Vector3.one * s;

            yield return null;
        }

        panelCanvasGroup.alpha = endAlpha;
        panelGO.transform.localScale = Vector3.one * endScale;

        if (!open)
            panelGO.SetActive(false);

        animRoutine = null;
    }

    void OnDestroy()
    {
        if (canvasGO != null) Destroy(canvasGO);
    }
}
