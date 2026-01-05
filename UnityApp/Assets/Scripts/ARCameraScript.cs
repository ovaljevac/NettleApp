using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if VUFORIA_ENGINE
using Vuforia;
#endif

public class ARBackButton : MonoBehaviour
{
    [Header("Back Button Icon")]
    public Sprite backIconSprite;

    [Header("Scene name to load")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Back Button UI")]
    public Vector2 backMargin = new Vector2(32f, 32f);
    public Vector2 backSize = new Vector2(72f, 72f);

    [Header("Info / Help Buttons")]
    public Sprite infoIconSprite;
    public Sprite helpIconSprite;
    public Vector2 topLeftMargin = new Vector2(32f, 32f);
    public Vector2 infoHelpSize = new Vector2(72f, 72f);
    public float spaceBetweenTopButtons = 16f;

    [Header("Panels (Text)")]
    [TextArea(3, 8)] public string appInfoText =
"Dobrodošli u AR aplikaciju o koprivi! 🌿\n\n" +
"Ova aplikacija je napravljena kao projekt za fakultet i njen cilj je da na jednostavan i zanimljiv način prikaže biljku koprivu uz pomoć proširene stvarnosti.\n\n" +
"Kroz aplikaciju možete saznati više o:\n" +
"✔ izgledu koprive\n" +
"✔ njenim osobinama\n" +
"✔ upotrebi u svakodnevnom životu\n\n" +
"Hvala što istražujete s nama! 😊";

    
    [TextArea(3, 8)] public string usageHelpText =
"Upute za korištenje aplikacije\n\n" +
"1️⃣ Pokrenite aplikaciju i usmjerite kameru prema image targetu. Kada marker bude prepoznat, otvorit će se AR prikaz sa biljkom koprivom.\n\n" +
"2️⃣ U AR okruženju oko koprive se nalaze tri POI objekta na koje možete kliknuti:\n\n" +
"🔵 Kalupi za mafine - klik na ovaj objekat otvara panel sa receptom.\n\n" +
"🟢 Džezva - dupli klik pokreće video pripreme čaja od koprive.\n\n" +
"🟡 Vojnik - klik pokreće audio snimak koji objašnjava kako se kopriva koristi u tekstilnoj industriji i na koji način je bila korištena tokom Prvog svjetskog rata.\n\n" +
"Za najbolje iskustvo, držite uređaj stabilno i koristite aplikaciju u dobro osvijetljenom prostoru.";


    [Header("Canvas")]
    public int canvasSortingOrder = 500; // iznad AR scene UI ako treba

    private const string CanvasName = "AR_UI_Canvas";
    private const string BackButtonName = "ARBackIconButton";
    private const string InfoButtonName = "ARInfoIconButton";
    private const string HelpButtonName = "ARHelpIconButton";

    private GameObject infoPanel;
    private GameObject helpPanel;

    void Start()
    {
        Canvas canvas = FindCanvas();

        EnsureBackButton(canvas);
        EnsureInfoAndHelpButtons(canvas);
    }

    private void EnsureBackButton(Canvas canvas)
    {
        // Ako dugme već postoji, ne pravi opet
        Transform existing = canvas.transform.Find(BackButtonName);
        if (existing != null) return;

        GameObject backGO = new GameObject(BackButtonName);
        backGO.transform.SetParent(canvas.transform, false);

        Image backImg = backGO.AddComponent<Image>();
        backImg.sprite = backIconSprite;
        backImg.preserveAspect = true;
        backImg.color = Color.white;

        Button backBtn = backGO.AddComponent<Button>();
        backBtn.transition = Selectable.Transition.None;
        backBtn.onClick.AddListener(OnBackPressed);

        RectTransform rt = backGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = backMargin;
        rt.sizeDelta = backSize;
    }

    private void EnsureInfoAndHelpButtons(Canvas canvas)
    {
        // Već postoje?
        Transform existingInfo = canvas.transform.Find(InfoButtonName);
        Transform existingHelp = canvas.transform.Find(HelpButtonName);
        if (existingInfo != null && existingHelp != null) return;

        // INFO BUTTON (gore lijevo)
        GameObject infoGO = new GameObject(InfoButtonName);
        infoGO.transform.SetParent(canvas.transform, false);

        Image infoImg = infoGO.AddComponent<Image>();
        infoImg.sprite = infoIconSprite;
        infoImg.preserveAspect = true;
        infoImg.color = Color.white;

        Button infoBtn = infoGO.AddComponent<Button>();
        infoBtn.transition = Selectable.Transition.None;
        infoBtn.onClick.AddListener(ToggleInfoPanel);

        RectTransform infoRT = infoGO.GetComponent<RectTransform>();
        infoRT.anchorMin = new Vector2(0f, 1f);
        infoRT.anchorMax = new Vector2(0f, 1f);
        infoRT.pivot = new Vector2(0f, 1f);
        infoRT.anchoredPosition = new Vector2(topLeftMargin.x, -topLeftMargin.y);
        infoRT.sizeDelta = infoHelpSize;

        // HELP BUTTON (?) – desno od INFO
        GameObject helpGO = new GameObject(HelpButtonName);
        helpGO.transform.SetParent(canvas.transform, false);

        Image helpImg = helpGO.AddComponent<Image>();
        helpImg.sprite = helpIconSprite;
        helpImg.preserveAspect = true;
        helpImg.color = Color.white;

        Button helpBtn = helpGO.AddComponent<Button>();
        helpBtn.transition = Selectable.Transition.None;
        helpBtn.onClick.AddListener(ToggleHelpPanel);

        RectTransform helpRT = helpGO.GetComponent<RectTransform>();
        helpRT.anchorMin = new Vector2(0f, 1f);
        helpRT.anchorMax = new Vector2(0f, 1f);
        helpRT.pivot = new Vector2(0f, 1f);

        // Pored info dugmeta (desno)
        float helpX = topLeftMargin.x + infoHelpSize.x + spaceBetweenTopButtons;
        helpRT.anchoredPosition = new Vector2(helpX, -topLeftMargin.y);
        helpRT.sizeDelta = infoHelpSize;

        // Kreiraj panele, ali drži ih skrivenim
        infoPanel = CreatePanel("InfoPanel", appInfoText, canvas);
        infoPanel.SetActive(false);

        helpPanel = CreatePanel("HelpPanel", usageHelpText, canvas);
        helpPanel.SetActive(false);
    }

    private GameObject CreatePanel(string panelName, string bodyText, Canvas canvas)
    {
        // Root (tamna pozadina preko cijelog ekrana)
        GameObject root = new GameObject(panelName);
        root.transform.SetParent(canvas.transform, false);

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f); // poluprozirno crno

        Button bgButton = root.AddComponent<Button>();
        bgButton.transition = Selectable.Transition.None;
        // Klik na pozadinu = zatvori panel
        bgButton.onClick.AddListener(() => root.SetActive(false));

        RectTransform bgRT = root.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(1f, 1f);
        bgRT.pivot = new Vector2(0.5f, 0.5f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Inner panel (bijela kutija u sredini)
        GameObject inner = new GameObject(panelName + "_Inner");
        inner.transform.SetParent(root.transform, false);

        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = Color.white;

        RectTransform innerRT = inner.GetComponent<RectTransform>();
        innerRT.anchorMin = new Vector2(0.5f, 0.5f);
        innerRT.anchorMax = new Vector2(0.5f, 0.5f);
        innerRT.pivot = new Vector2(0.5f, 0.5f);
        innerRT.sizeDelta = new Vector2(800f, 500f);

        // Tekst
        GameObject textGO = new GameObject(panelName + "_Text");
        textGO.transform.SetParent(inner.transform, false);

        Text text = textGO.AddComponent<Text>();
        text.text = bodyText;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.fontSize = 24;

        // Built-in font (da radi odmah bez dodatnih referenci)
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.black;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.offsetMin = new Vector2(30f, 30f);
        textRT.offsetMax = new Vector2(-30f, -30f);

        return root;
    }

    private void ToggleInfoPanel()
    {
        if (infoPanel == null) return;

        bool newState = !infoPanel.activeSelf;

        // ako otvaramo info – zatvori help
        if (newState && helpPanel != null)
            helpPanel.SetActive(false);

        infoPanel.SetActive(newState);
    }

    private void ToggleHelpPanel()
    {
        if (helpPanel == null) return;

        bool newState = !helpPanel.activeSelf;

        // ako otvaramo help – zatvori info
        if (newState && infoPanel != null)
            infoPanel.SetActive(false);

        helpPanel.SetActive(newState);
    }

    private Canvas FindCanvas()
    {
        GameObject canvasGO = GameObject.Find(CanvasName);
        Canvas canvas;

        if (canvasGO == null)
        {
            canvasGO = new GameObject(CanvasName);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortingOrder;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Ako nema EventSystem u sceni, UI klikovi neće raditi
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                Debug.LogWarning("Nema EventSystem u sceni. Dodaj: GameObject -> UI -> Event System");
            }
        }
        else
        {
            canvas = canvasGO.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasGO.AddComponent<Canvas>();
        }

        return canvas;
    }

    private void OnBackPressed()
    {
        // 1) Forsiraj landscape (da Android/Vuforia ne pokušaju portrait)
        ForceLandscape();

        // 2) Zatvori/zaustavi video ako je otvoren (da ne ostane aktivan)
        StopAnyRuntimeVideo();

        // 3) Ugasiti Vuforia prije izlaska (sprječava "Could not set view orientation to PORTRAIT" spam)
#if VUFORIA_ENGINE
        if (VuforiaBehaviour.Instance != null)
        {
            // Disable Vuforia behaviour before leaving the scene
            VuforiaBehaviour.Instance.enabled = false;
        }
#endif

        // 4) Učitaj menu scenu
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ForceLandscape()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
    }

    private void StopAnyRuntimeVideo()
    {
        // Ako koristiš POIVideoAllInOne – ugasi ga fino
        POIVideoAllInOne poi = FindObjectOfType<POIVideoAllInOne>();
        if (poi != null)
        {
            poi.CloseVideo();
        }

        // Dodatno: zaustavi sve VideoPlayer-e u sceni (sigurno)
        var players = FindObjectsOfType<UnityEngine.Video.VideoPlayer>();
        foreach (var vp in players)
        {
            if (vp != null) vp.Stop();
        }
    }
}
