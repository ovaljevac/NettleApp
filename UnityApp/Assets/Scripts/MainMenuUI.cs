using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Background & Images")]
    public Sprite backgroundSprite;
    public Sprite mainPlantSprite;   // Slika biljke (ako želiš da bude iznad dugmadi)

    private Canvas canvas;
    private Font defaultFont;

    // SAFE AREA
    private RectTransform safeAreaRT;

    // PANELS
    private GameObject menuPanel;
    private GameObject quizPanel;
    private GameObject infoPanel;

    // QUIZ UI REFERENCE
    private Text quizQuestionText;
    private GameObject quizAnswersContainer;
    private List<Button> quizAnswerButtons = new List<Button>();
    private Text quizResultText;
    private Button quizBackToMenuButton;
    private Button quizRestartButton; // ✅ NOVO

    public Sprite cameraIconSprite; // dodaj na vrhu skripte
    public Sprite quizIconSprite;
    public Sprite infoIconSprite;

    [Header("Quiz Result Icons")]
public Sprite restartIconSprite;   // npr. refresh/rotate arrow
public Sprite backIconSprite;      // npr. home/back


private void CreateIconMenuButton(
    Transform parent,
    Sprite icon,
    string label,
    UnityEngine.Events.UnityAction onClick)
{
    GameObject buttonGO = new GameObject(label);
    buttonGO.transform.SetParent(parent, false);

    // Transparentna grafika (samo za klik)
    Image bg = buttonGO.AddComponent<Image>();
    bg.color = new Color(1f, 1f, 1f, 0f);

    Button button = buttonGO.AddComponent<Button>();
    button.targetGraphic = bg;
    button.transition = Selectable.Transition.None;
    button.onClick.AddListener(onClick);

    // VELIČINA DUGMETA (ISTA KAO OSTALA)
    RectTransform rt = buttonGO.GetComponent<RectTransform>();
    rt.sizeDelta = new Vector2(320f, 320f);

    // ---------------- IKONA ----------------
    GameObject iconGO = new GameObject("Icon");
    iconGO.transform.SetParent(buttonGO.transform, false);

    Image iconImg = iconGO.AddComponent<Image>();
    iconImg.sprite = icon;
    iconImg.preserveAspect = true;
    iconImg.color = Color.white; // ili tamno zelena

    RectTransform iconRT = iconGO.GetComponent<RectTransform>();
    iconRT.anchorMin = new Vector2(0.5f, 0.68f);
iconRT.anchorMax = new Vector2(0.5f, 0.68f);

    iconRT.pivot = new Vector2(0.5f, 0.5f);
    iconRT.sizeDelta = new Vector2(280f, 280f);


    // ---------------- TEKST ----------------
    // ---------------- TEKST ----------------
GameObject textGO = new GameObject("Text");
textGO.transform.SetParent(buttonGO.transform, false);

Text text = textGO.AddComponent<Text>();
text.text = label;                 // "KVIZ ZNANJA"
text.font = defaultFont;
text.fontSize = 42;
text.fontStyle = FontStyle.Bold;
text.alignment = TextAnchor.MiddleCenter;
text.color = Color.white;
text.horizontalOverflow = HorizontalWrapMode.Wrap;   // ✅ wrap
text.verticalOverflow = VerticalWrapMode.Overflow;   // ✅ ne reže

RectTransform textRT = textGO.GetComponent<RectTransform>();
textRT.anchorMin = new Vector2(0.05f, 0.00f);
textRT.anchorMax = new Vector2(0.95f, 0.28f);        // ✅ veća visina (prije ti je bilo premalo)
textRT.offsetMin = Vector2.zero;
textRT.offsetMax = Vector2.zero;

Shadow shadow = textGO.AddComponent<Shadow>();
shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
shadow.effectDistance = new Vector2(2f, -2f);

}



    // QUIZ DATA
    private class QuizQuestion
    {
        public string question;
        public string[] answers;
        public int correctIndex;

        public QuizQuestion(string q, string[] a, int c)
        {
            question = q;
            answers = a;
            correctIndex = c;
        }
    }

    private List<QuizQuestion> quizQuestions;
    private int currentQuestionIndex = 0;  // indeks u quizOrder
    private int correctAnswers = 0;

    // novi za random poredak
    private List<int> quizOrder;           // random redoslijed pitanja
    private Button currentCorrectButton;   // referenca na tačno dugme

    // BOJE
    private Color normalButtonColor = new Color(0.9f, 0.9f, 0.9f, 1f); // Svijetlo siva
    private Color correctColor = new Color(0f, 0.7f, 0f, 1f);          // Zeleno
    private Color wrongColor = new Color(1f, 0.3f, 0.3f, 1f);          // Crveno

    private void Awake()
    {
        // Učitavanje fonta
        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateCanvas();
        CreateBackground();
        CreateEventSystem();

        // SAFE AREA parent za UI
        CreateSafeArea();

        // Kreiranje UI sekcija (sve ide pod SafeArea)
        CreateMenuButtons();   // GLAVNI MENI SA 3 DUGMETA
        SetupQuizData();
        CreateQuizUI();
        CreateInfoUI();

        // Start
        ShowMainMenu();
    }

    private void OnRectTransformDimensionsChange()
    {
        // Kad se promijeni rezolucija/orijentacija (ili safe area), ponovno primijeni
        if (safeAreaRT != null)
            ApplySafeArea();
    }

    // -------------------- SETUP --------------------

    private void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // LANDSCAPE reference resolution
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.transform.SetParent(transform, false);
    }

    private void CreateBackground()
    {
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvas.transform, false);

        Image img = bgGO.AddComponent<Image>();
        if (backgroundSprite != null)
            img.sprite = backgroundSprite;
        else
            img.color = new Color(0.2f, 0.4f, 0.2f); // Tamno zelena ako nema slike

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        bgGO.transform.SetAsFirstSibling(); // pozadina iza svega
    }

    private void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }
    }

    // SAFE AREA (za notch/rounded corners)
    private void CreateSafeArea()
    {
        GameObject safeGO = new GameObject("SafeArea");
        safeGO.transform.SetParent(canvas.transform, false);

        safeAreaRT = safeGO.AddComponent<RectTransform>();
        safeAreaRT.anchorMin = Vector2.zero;
        safeAreaRT.anchorMax = Vector2.one;
        safeAreaRT.offsetMin = Vector2.zero;
        safeAreaRT.offsetMax = Vector2.zero;

        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeAreaRT.anchorMin = anchorMin;
        safeAreaRT.anchorMax = anchorMax;
    }

    // -------------------- GLAVNI MENI (3 DUGMETA) --------------------

    private void CreateMenuButtons()
    {
        // PANEL u sredini ekrana
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(safeAreaRT, false);

        RectTransform panelRT = menuPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);

        // 3 kvadrata + razmaci
        panelRT.sizeDelta = new Vector2(1600f, 420f);

        // HORIZONTALNI layout (jedno do drugog)
        HorizontalLayoutGroup hlg = menuPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 90f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Dugmad (kvadratna)
        CreateIconMenuButton(menuPanel.transform, cameraIconSprite,"AR KAMERA", OnARCameraClicked);
        CreateIconMenuButton(menuPanel.transform, quizIconSprite, "KVIZ ZNANJA", OnQuizClicked);
        CreateIconMenuButton(menuPanel.transform, infoIconSprite, "INFO PANEL", OnInfoClicked);
    }

    private void CreateSquareMenuButton(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(label);
        buttonGO.transform.SetParent(parent, false);

        // Image
        Image img = buttonGO.AddComponent<Image>();
        img.color = Color.white;

        // Button
        Button button = buttonGO.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        // KVADRAT
        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 320f);

        // Tekst
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = defaultFont;
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.supportRichText = true;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(20, 20);
        textRT.offsetMax = new Vector2(-20, -20);
    }

    // -------------------- LOGIKA PRIKAZA --------------------

    private void ShowMainMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (quizPanel != null) quizPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    // -------------------- KVIZ DIO --------------------

    private void SetupQuizData()
    {
        quizQuestions = new List<QuizQuestion>();

        quizQuestions.Add(new QuizQuestion(
            "Latinski naziv koprive?",
            new string[] { "Urtica dioica", "Mentha piperita", "Rosa canina", "Pinus" },
            0));

        quizQuestions.Add(new QuizQuestion(
            "Zašto kopriva peče?",
            new string[] { "Kiselina u dlačicama", "Trnje", "Magija", "Toplota" },
            0));

        quizQuestions.Add(new QuizQuestion(
            "Koji dio se bere za čaj?",
            new string[] { "Listovi", "Sjeme", "Korijen", "Sve" },
            0));

        quizQuestions.Add(new QuizQuestion(
            "Kada je najbolje brati koprivu?",
            new string[] { "U proljeće, prije cvjetanja", "Usred ljeta", "U kasnu jesen", "Usred zime" },
            0));

        quizQuestions.Add(new QuizQuestion(
            "Kakva je kopriva po tipu biljke?",
            new string[] { "Višegodišnja zeljasta biljka", "Jednogodišnji grm", "Iglasto drvo", "Kaktus" },
            0));

        quizQuestions.Add(new QuizQuestion(
            "Za šta se najčešće koristi čaj od koprive?",
            new string[] { "Podrška radu bubrega i mokraćnih puteva", "Snižavanje tjelesne temperature", "Poboljšanje vida", "Povećanje apetita za slatkiše" },
            0));

        quizQuestions.Add(new QuizQuestion(
            "Koji dio koprive se često koristi i u ishrani (pite, čorbe)?",
            new string[] { "Mladi listovi", "Samo cvijet", "Samo korijen", "Samo stabljika" },
            0));

        quizQuestions.Add(new QuizQuestion(
            "Šta neutralizira peckanje koprive na koži?",
            new string[] { "Pranje zahvaćenog mjesta hladnom vodom i sapunom", "Dodatno trljanje listom koprive", "Ugrijavanje mjesta fenom", "Premazivanje uljem za sunčanje" },
            0));
    }

  private void CreateQuizUI()
{
    quizPanel = new GameObject("QuizPanel");
    quizPanel.transform.SetParent(safeAreaRT, false);

    Image bg = quizPanel.AddComponent<Image>();
    bg.color = new Color(0, 0, 0, 0.8f); // Tamna polu-providna

    RectTransform panelRT = quizPanel.GetComponent<RectTransform>();
    panelRT.anchorMin = Vector2.zero;
    panelRT.anchorMax = Vector2.one;
    panelRT.offsetMin = Vector2.zero;
    panelRT.offsetMax = Vector2.zero;

    VerticalLayoutGroup vlg = quizPanel.AddComponent<VerticalLayoutGroup>();
    vlg.childAlignment = TextAnchor.MiddleCenter;
    vlg.spacing = 16f;
    vlg.padding = new RectOffset(60, 60, 40, 40);

    // ---------------- Pitanje ----------------
    GameObject qTextGO = new GameObject("QuestionText");
    qTextGO.transform.SetParent(quizPanel.transform, false);
    quizQuestionText = qTextGO.AddComponent<Text>();
    quizQuestionText.font = defaultFont;
    quizQuestionText.fontSize = 44;
    quizQuestionText.alignment = TextAnchor.MiddleCenter;
    quizQuestionText.color = Color.white;
    qTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 150);

    // ---------------- Odgovori Container ----------------
    quizAnswersContainer = new GameObject("AnswersContainer");
    quizAnswersContainer.transform.SetParent(quizPanel.transform, false);

    VerticalLayoutGroup ansVLG = quizAnswersContainer.AddComponent<VerticalLayoutGroup>();
    ansVLG.spacing = 22;
    ansVLG.childAlignment = TextAnchor.MiddleCenter;

    // KLJUČNO: da se dugmad NE razvlače preko cijele širine
    ansVLG.childControlWidth = false;
    ansVLG.childControlHeight = false;
    ansVLG.childForceExpandWidth = false;
    ansVLG.childForceExpandHeight = false;

    ContentSizeFitter csf = quizAnswersContainer.AddComponent<ContentSizeFitter>();
    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

    // ---------------- 4 Dugmeta za odgovore ----------------
    for (int i = 0; i < 4; i++)
    {
        GameObject btnGO = new GameObject("AnsBtn" + i);
        btnGO.transform.SetParent(quizAnswersContainer.transform, false);

        Image img = btnGO.AddComponent<Image>();
        img.color = normalButtonColor;

        Button btn = btnGO.AddComponent<Button>();
        quizAnswerButtons.Add(btn);

        LayoutElement le = btnGO.AddComponent<LayoutElement>();
        le.preferredWidth = 700;
        le.preferredHeight = 75;
        le.flexibleWidth = 0;
        le.flexibleHeight = 0;

        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(650, 75);

        GameObject tGO = new GameObject("Text");
        tGO.transform.SetParent(btnGO.transform, false);

        Text t = tGO.AddComponent<Text>();
        t.font = defaultFont;
        t.fontSize = 28;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.black;

        RectTransform trt = tGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
    }

    // ---------------- Rezultat Text (CENTAR, VELIKO) ----------------
    GameObject resGO = new GameObject("ResultText");
    resGO.transform.SetParent(quizPanel.transform, false);

    LayoutElement resLE = resGO.AddComponent<LayoutElement>();
    resLE.ignoreLayout = true;

    quizResultText = resGO.AddComponent<Text>();
    quizResultText.font = defaultFont;
    quizResultText.fontSize = 64;
    quizResultText.alignment = TextAnchor.MiddleCenter;
    quizResultText.color = Color.yellow;
    quizResultText.supportRichText = true;
    quizResultText.gameObject.SetActive(false);

    RectTransform resRT = resGO.GetComponent<RectTransform>();
    resRT.anchorMin = new Vector2(0.5f, 0.55f);
    resRT.anchorMax = new Vector2(0.5f, 0.55f);
    resRT.pivot = new Vector2(0.5f, 0.5f);
    resRT.anchoredPosition = Vector2.zero;
    resRT.sizeDelta = new Vector2(1200, 420);

    // =========================================================
    // BACK IKONA (UVIJEK VIDLJIVA) - DONJI LIJEVI UGAO
    // =========================================================
    GameObject backBtnGO = new GameObject("QuizBackIconBtn");
    backBtnGO.transform.SetParent(quizPanel.transform, false);

    LayoutElement backLE = backBtnGO.AddComponent<LayoutElement>();
    backLE.ignoreLayout = true; // da ne utiče na VerticalLayoutGroup

    Image backImg = backBtnGO.AddComponent<Image>();
    backImg.sprite = backIconSprite;   // postavi u Inspectoru
    backImg.preserveAspect = true;
    backImg.color = Color.white;

    quizBackToMenuButton = backBtnGO.AddComponent<Button>();
    quizBackToMenuButton.transition = Selectable.Transition.None;
    quizBackToMenuButton.onClick.AddListener(ShowMainMenu);

    

    RectTransform backRT = backBtnGO.GetComponent<RectTransform>();
    backRT.anchorMin = new Vector2(0f, 0f);
    backRT.anchorMax = new Vector2(0f, 0f);
    backRT.pivot = new Vector2(0f, 0f);
    backRT.anchoredPosition = new Vector2(32f, 32f);
    backRT.sizeDelta = new Vector2(72, 72);

    // =========================================================
    // RESTART IKONA (SAMO NA REZULTATU) - DONJI CENTAR, VEĆE
    // =========================================================
    GameObject restartBtnGO = new GameObject("QuizRestartIconBtn");
    restartBtnGO.transform.SetParent(quizPanel.transform, false);

    LayoutElement restartLayout = restartBtnGO.AddComponent<LayoutElement>();
    restartLayout.ignoreLayout = true; // da ne utiče na VerticalLayoutGroup

    Image restartImg = restartBtnGO.AddComponent<Image>();
    restartImg.sprite = restartIconSprite;   // postavi u Inspectoru
    restartImg.preserveAspect = true;
    restartImg.color = Color.white;

    quizRestartButton = restartBtnGO.AddComponent<Button>();
    quizRestartButton.transition = Selectable.Transition.None;
    quizRestartButton.onClick.AddListener(StartQuiz);

    
    RectTransform restartRT = restartBtnGO.GetComponent<RectTransform>();
    restartRT.anchorMin = new Vector2(0.5f, 0f);
    restartRT.anchorMax = new Vector2(0.5f, 0f);
    restartRT.pivot = new Vector2(0.5f, 0f);
    restartRT.anchoredPosition = new Vector2(0f, 32f);
    restartRT.sizeDelta = new Vector2(96, 96); // veće od back ikonice

    // restart je samo na rezultatima
    quizRestartButton.gameObject.SetActive(false);

    // back je UVIJEK vidljiv
    quizBackToMenuButton.gameObject.SetActive(true);

    quizPanel.SetActive(false);
}


    private void StartQuiz()
    {
        currentQuestionIndex = 0;
        correctAnswers = 0;

        // random redoslijed pitanja
        quizOrder = new List<int>();
        for (int i = 0; i < quizQuestions.Count; i++)
            quizOrder.Add(i);
        ShuffleList(quizOrder);

        // ✅ vrati prikaz pitanja kad se kviz ponovo pokrene
        quizQuestionText.gameObject.SetActive(true);

        quizResultText.gameObject.SetActive(false);
       // quizBackToMenuButton.gameObject.SetActive(false);
        if (quizRestartButton != null) quizRestartButton.gameObject.SetActive(false);

        quizAnswersContainer.SetActive(true);

        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        int questionListIndex = quizOrder[currentQuestionIndex];
        QuizQuestion q = quizQuestions[questionListIndex];

        quizQuestionText.text = q.question;

        foreach (var btn in quizAnswerButtons)
        {
            btn.interactable = true;
            btn.GetComponent<Image>().color = normalButtonColor;
        }

        List<int> answerOrder = new List<int> { 0, 1, 2, 3 };
        ShuffleList(answerOrder);

        currentCorrectButton = null;

        for (int i = 0; i < quizAnswerButtons.Count; i++)
        {
            Button btn = quizAnswerButtons[i];
            int answerIndex = answerOrder[i];

            Text btnText = btn.GetComponentInChildren<Text>();
            btnText.text = q.answers[answerIndex];

            bool isCorrect = (answerIndex == q.correctIndex);

            if (isCorrect)
                currentCorrectButton = btn;

            btn.onClick.RemoveAllListeners();
            Button btnCopy = btn;
            btn.onClick.AddListener(() => OnAnswerClicked(btnCopy, isCorrect));
        }
    }

    private void OnAnswerClicked(Button clickedButton, bool isCorrect)
    {
        foreach (var btn in quizAnswerButtons)
            btn.interactable = false;

        if (isCorrect)
        {
            correctAnswers++;
            clickedButton.GetComponent<Image>().color = correctColor;
        }
        else
        {
            clickedButton.GetComponent<Image>().color = wrongColor;
            if (currentCorrectButton != null)
                currentCorrectButton.GetComponent<Image>().color = correctColor;
        }

        StartCoroutine(NextQuestionCoroutine());
    }

    private IEnumerator NextQuestionCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        currentQuestionIndex++;
        if (currentQuestionIndex < quizQuestions.Count) ShowCurrentQuestion();
        else ShowResults();
    }

    private void ShowResults()
    {
        // sakrij pitanje i odgovore
        quizAnswersContainer.SetActive(false);
        quizQuestionText.gameObject.SetActive(false);

        int total = quizQuestions.Count;
        int correct = correctAnswers;

        string message;

        if (correct == total) // 8/8
            message = "<b>BRAVO! 🎉</b>\nSavršeno! " + correct + "/" + total;
        else if (correct >= 5) // 5-7/8
            message = "<b>Odlično! 👏</b>\nSkoro savršeno: " + correct + "/" + total;
        else if (correct >= 2) // 2-4/8
            message = "<b>Dobar pokušaj 🙂</b>\nImaš " + correct + "/" + total + ". Pokušaj ponovo!";
        else // 0-1/8
            message = "<b>Ne odustaj 💪</b>\nRezultat: " + correct + "/" + total + "\nPročitaj Info pa pokušaj opet!";

        quizResultText.text = message;
        quizResultText.gameObject.SetActive(true);

        if (quizRestartButton != null) quizRestartButton.gameObject.SetActive(true);
        quizBackToMenuButton.gameObject.SetActive(true);
    }

    // -------------------- INFO PANEL (LANDSCAPE: 2 kolone) --------------------

    private void CreateInfoUI()
    {
        infoPanel = new GameObject("InfoPanel");
        infoPanel.transform.SetParent(safeAreaRT, false);

        Image bg = infoPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.2f, 0.1f, 0.95f);

        RectTransform rt = infoPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // NASLOV
        GameObject titleGO = new GameObject("InfoTitle");
        titleGO.transform.SetParent(infoPanel.transform, false);
        Text title = titleGO.AddComponent<Text>();
        title.text = "O KOPRIVI";
        title.font = defaultFont;
        title.fontSize = 46;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;

        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.86f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // SLIKA LIJEVO
        GameObject imgGO = new GameObject("PlantImage");
        imgGO.transform.SetParent(infoPanel.transform, false);
        Image plantImg = imgGO.AddComponent<Image>();

        if (mainPlantSprite != null)
            plantImg.sprite = mainPlantSprite;
        else
            plantImg.color = new Color(0f, 0.1f, 0f, 1f);

        plantImg.preserveAspect = true;
        plantImg.type = Image.Type.Sliced;

        RectTransform imgRT = imgGO.GetComponent<RectTransform>();
        imgRT.anchorMin = new Vector2(0.05f, 0.22f);
        imgRT.anchorMax = new Vector2(0.45f, 0.84f);
        imgRT.offsetMin = Vector2.zero;
        imgRT.offsetMax = Vector2.zero;

        // TEKST DESNO
        GameObject bodyGO = new GameObject("InfoBody");
        bodyGO.transform.SetParent(infoPanel.transform, false);
        Text body = bodyGO.AddComponent<Text>();

        body.text =
        "<b>Porijeklo i opis</b>\n" +
        "Kopriva (Urtica dioica) je višegodišnja zeljasta biljka rasprostranjena širom Evrope, Azije i Sjeverne Afrike. " +
        "Prepoznatljiva je po žarećim dlačicama koje sadrže histamin i druge supstance koje izazivaju peckanje kože pri dodiru.\n\n" +

        "<b>Nutritivne osobine</b>\n" +
        "Mladi listovi koprive izuzetno su bogati vitaminom C, željezom, kalcijem, kalijem i proteinima. " +
        "Zbog toga se koristi kao nutritivno vrijedna namirnica u supama, pitama i varivima, a čaj od sušenih listova je veoma popularan.\n\n" +

        "<b>Ljekovita tradicionalna upotreba</b>\n" +
        "Kopriva se tradicionalno koristi za podršku radu bubrega, mokraćnih puteva i kod osjećaja težine u zglobovima. " +
        "Poznata je i po svojim antioksidativnim i protuupalnim svojstvima koja se i danas istražuju u fitoterapiji.\n\n" +

        "<b>Upotreba u ishrani</b>\n" +
        "Mladi listovi se beru u proljeće i koriste kao zamjena za špinat. Često se dodaju u čorbe, pite, rižota i smutije. " +
        "Termičkom obradom kopriva gubi žareća svojstva i postaje potpuno sigurna za konzumaciju.\n\n" +

        "<b>Industrijska i ekološka upotreba</b>\n" +
        "Vlakna iz stabljike koprive mogu se koristiti za izradu tekstila, slično lanu. " +
        "Kopriva je takođe cijenjena u organskoj poljoprivredi kao prirodno tečno đubrivo i sredstvo za jačanje biljaka.\n\n" +

        "<b>Napomena</b>\n" +
        "Informacije u ovoj aplikaciji namijenjene su edukaciji i ne predstavljaju medicinski savjet. " +
        "Za zdravstvene tegobe uvijek se obratite stručnoj osobi.";

        body.font = defaultFont;
        body.fontSize = 26;
        body.alignment = TextAnchor.UpperLeft;
        body.color = Color.white;

        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.48f, 0.18f);
        bodyRT.anchorMax = new Vector2(0.95f, 0.84f);
        bodyRT.offsetMin = Vector2.zero;
        bodyRT.offsetMax = Vector2.zero;

        // DUGME NAZAD
        // ---------------- BACK ICON BUTTON ----------------
GameObject backGO = new GameObject("InfoBackIcon");
backGO.transform.SetParent(infoPanel.transform, false);

// Image (ikona)
Image backImg = backGO.AddComponent<Image>();
backImg.sprite = backIconSprite;
backImg.preserveAspect = true;
backImg.color = Color.white;

// Button
Button backBtn = backGO.AddComponent<Button>();
backBtn.transition = Selectable.Transition.None;
backBtn.onClick.AddListener(ShowMainMenu);

// Hover / tap feedback (ako već koristiš)


RectTransform backRT = backGO.GetComponent<RectTransform>();


// DONJI LIJEVI UGAO
backRT.anchorMin = new Vector2(0f, 0f);
backRT.anchorMax = new Vector2(0f, 0f);
backRT.pivot = new Vector2(0f, 0f);

// MARGINA OD IVICA
backRT.anchoredPosition = new Vector2(40f, 40f);

// VELIČINA IKONE
backRT.sizeDelta = new Vector2(80, 80);




        infoPanel.SetActive(false);
    }

    // -------------------- BUTTON ACTIONS --------------------

    private void OnARCameraClicked()
    {
        Debug.Log("Pokrećem AR kameru...");
        SceneManager.LoadScene("SampleScene");
    }

    private void OnQuizClicked()
    {
        menuPanel.SetActive(false);
        infoPanel.SetActive(false);
        quizPanel.SetActive(true);
        StartQuiz();
    }

    private void OnInfoClicked()
    {
        menuPanel.SetActive(false);
        quizPanel.SetActive(false);
        infoPanel.SetActive(true);
    }

    // -------------------- HELPER: SHUFFLE --------------------

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
