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
    private Color wrongColor = new Color(1f, 0.3f, 0.3f, 1f);         // Crveno

    private void Awake()
    {
        // Učitavanje fonta
        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateCanvas();
        CreateBackground();
        CreateEventSystem();
        
        // Kreiranje UI sekcija
        CreateMenuButtons();   // GLAVNI MENI SA 3 DUGMETA
        SetupQuizData();
        CreateQuizUI();
        CreateInfoUI();

        // Start
        ShowMainMenu();
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
        scaler.referenceResolution = new Vector2(1080, 1920); // portret

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

    // -------------------- GLAVNI MENI (3 DUGMETA) --------------------

    private void CreateMenuButtons()
    {
        // 1. Kontejner panel za dugmad
        menuPanel = new GameObject("MenuPanel");
        menuPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRT = menuPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f); // Centar ekrana
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(600f, 500f); // Veličina panela

        // Vertical Layout Group da ih poreda jedno ispod drugog
        VerticalLayoutGroup vlg = menuPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 40f; // Razmak između dugmadi
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;

        // 2. Kreiranje 3 dugmeta
        CreateMenuButton(menuPanel.transform, "Pokreni AR Kameru", OnARCameraClicked);
        CreateMenuButton(menuPanel.transform, "Kviz Znanja", OnQuizClicked);
        CreateMenuButton(menuPanel.transform, "Info o biljci", OnInfoClicked);
    }

    private void CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(label);
        buttonGO.transform.SetParent(parent, false);

        Image img = buttonGO.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f); // Bijela

        Button button = buttonGO.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500f, 100f);

        // Tekst na dugmetu
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = defaultFont;
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
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
        quizPanel.transform.SetParent(canvas.transform, false);

        Image bg = quizPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f); // Tamna polu-providna

        RectTransform panelRT = quizPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = quizPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 20f;
        vlg.padding = new RectOffset(50, 50, 50, 50);

        // Pitanje
        GameObject qTextGO = new GameObject("QuestionText");
        qTextGO.transform.SetParent(quizPanel.transform, false);
        quizQuestionText = qTextGO.AddComponent<Text>();
        quizQuestionText.font = defaultFont;
        quizQuestionText.fontSize = 40;
        quizQuestionText.alignment = TextAnchor.MiddleCenter;
        quizQuestionText.color = Color.white;
        qTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 150);

        // Odgovori Container
        quizAnswersContainer = new GameObject("AnswersContainer");
        quizAnswersContainer.transform.SetParent(quizPanel.transform, false);
        VerticalLayoutGroup ansVLG = quizAnswersContainer.AddComponent<VerticalLayoutGroup>();
        ansVLG.spacing = 15;
        ansVLG.childAlignment = TextAnchor.MiddleCenter;
        quizAnswersContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4 Dugmeta za odgovore
        for (int i = 0; i < 4; i++)
        {
            GameObject btnGO = new GameObject("AnsBtn" + i);
            btnGO.transform.SetParent(quizAnswersContainer.transform, false);
            Image img = btnGO.AddComponent<Image>();
            img.color = normalButtonColor;
            Button btn = btnGO.AddComponent<Button>();
            quizAnswerButtons.Add(btn);
            
            GameObject tGO = new GameObject("Text");
            tGO.transform.SetParent(btnGO.transform, false);
            Text t = tGO.AddComponent<Text>();
            t.font = defaultFont;
            t.fontSize = 30;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.black;
            
            // Layout
            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.preferredWidth = 600;
            le.preferredHeight = 80;
            
            RectTransform trt = tGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        // Rezultat Text
        GameObject resGO = new GameObject("ResultText");
        resGO.transform.SetParent(quizPanel.transform, false);
        quizResultText = resGO.AddComponent<Text>();
        quizResultText.font = defaultFont;
        quizResultText.fontSize = 40;
        quizResultText.alignment = TextAnchor.MiddleCenter;
        quizResultText.color = Color.yellow;
        resGO.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 100);

        // Back Button
        // Back Button
GameObject backBtnGO = new GameObject("QuizBackBtn");
backBtnGO.transform.SetParent(quizPanel.transform, false);

// VAŽNO: ignorisati VerticalLayoutGroup
LayoutElement backLayout = backBtnGO.AddComponent<LayoutElement>();
backLayout.ignoreLayout = true;

// Image
Image bImg = backBtnGO.AddComponent<Image>();
bImg.color = Color.white;

// Button
quizBackToMenuButton = backBtnGO.AddComponent<Button>();
quizBackToMenuButton.onClick.AddListener(ShowMainMenu);

// RectTransform – isti kao u InfoBackBtn
RectTransform quizBackRT = backBtnGO.GetComponent<RectTransform>();
quizBackRT.anchorMin = new Vector2(0.5f, 0.1f);
quizBackRT.anchorMax = new Vector2(0.5f, 0.1f);
quizBackRT.pivot = new Vector2(0.5f, 0.5f);
quizBackRT.anchoredPosition = Vector2.zero;
quizBackRT.sizeDelta = new Vector2(300, 80);

// Text
GameObject btTextGO = new GameObject("Text");
btTextGO.transform.SetParent(backBtnGO.transform, false);
Text btText = btTextGO.AddComponent<Text>();

btText.text = "Nazad na meni";
btText.font = defaultFont;
btText.fontSize = 30;
btText.color = Color.black;
btText.alignment = TextAnchor.MiddleCenter;

RectTransform txtRT = btTextGO.GetComponent<RectTransform>();
txtRT.anchorMin = Vector2.zero;
txtRT.anchorMax = Vector2.one;
txtRT.offsetMin = Vector2.zero;
txtRT.offsetMax = Vector2.zero;


        quizPanel.SetActive(false);
    }

    private void StartQuiz()
    {
        currentQuestionIndex = 0;
        correctAnswers = 0;

        // napravi listu indeksa pitanja i promiješaj je
        quizOrder = new List<int>();
        for (int i = 0; i < quizQuestions.Count; i++)
            quizOrder.Add(i);
        ShuffleList(quizOrder);

        quizResultText.gameObject.SetActive(false);
        quizBackToMenuButton.gameObject.SetActive(false);
        quizAnswersContainer.SetActive(true);

        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        // Uzmi indeks pitanja iz random reda
        int questionListIndex = quizOrder[currentQuestionIndex];
        QuizQuestion q = quizQuestions[questionListIndex];

        quizQuestionText.text = q.question;

        // Reset boja + reaktiviraj dugmad
        foreach (var btn in quizAnswerButtons)
        {
            btn.interactable = true;
            btn.GetComponent<Image>().color = normalButtonColor;
        }

        // Random raspored odgovora (0,1,2,3)
        List<int> answerOrder = new List<int> { 0, 1, 2, 3 };
        ShuffleList(answerOrder);

        currentCorrectButton = null;

        for (int i = 0; i < quizAnswerButtons.Count; i++)
        {
            Button btn = quizAnswerButtons[i];
            int answerIndex = answerOrder[i]; // koji odgovor ide na ovo dugme

            Text btnText = btn.GetComponentInChildren<Text>();
            btnText.text = q.answers[answerIndex];

            bool isCorrect = (answerIndex == q.correctIndex);

            if (isCorrect)
                currentCorrectButton = btn;

            btn.onClick.RemoveAllListeners();
            Button btnCopy = btn; // capture
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
        quizAnswersContainer.SetActive(false);
        quizResultText.gameObject.SetActive(true);
        quizResultText.text = "Kraj!\nTačnih odgovora: " + correctAnswers + "/" + quizQuestions.Count;
        quizBackToMenuButton.gameObject.SetActive(true);
    }

    // -------------------- INFO PANEL --------------------

    private void CreateInfoUI()
    {
        infoPanel = new GameObject("InfoPanel");
        infoPanel.transform.SetParent(canvas.transform, false);

        // Pozadina
        Image bg = infoPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.2f, 0.1f, 0.95f); // Tamno zelena

        RectTransform rt = infoPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Naslov Info
        GameObject titleGO = new GameObject("InfoTitle");
        titleGO.transform.SetParent(infoPanel.transform, false);
        Text title = titleGO.AddComponent<Text>();
        title.text = "O KOPRIVI";
        title.font = defaultFont;
        title.fontSize = 50;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.8f);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // Tekst tijela
        GameObject bodyGO = new GameObject("InfoBody");
        bodyGO.transform.SetParent(infoPanel.transform, false);
        Text body = bodyGO.AddComponent<Text>();
        body.text = "Kopriva (Urtica dioica) je višegodišnja zeljasta biljka.\n\nPoznata je po svojim žarećim dlačicama, ali i kao izuzetno ljekovita biljka. Bogata je željezom, vitaminom C i kalcijem.\n\nKoristi se za čajeve, tinkture i jela.";
        body.font = defaultFont;
        body.fontSize = 30;
        body.alignment = TextAnchor.UpperCenter;
        body.color = Color.white;
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.1f, 0.2f);
        bodyRT.anchorMax = new Vector2(0.9f, 0.8f);

        // Dugme Nazad
        GameObject backGO = new GameObject("InfoBackBtn");
        backGO.transform.SetParent(infoPanel.transform, false);
        Image bImg = backGO.AddComponent<Image>();
        bImg.color = Color.white;
        Button backBtn = backGO.AddComponent<Button>();
        backBtn.onClick.AddListener(ShowMainMenu);

        RectTransform backRT = backGO.GetComponent<RectTransform>();
        backRT.anchorMin = new Vector2(0.5f, 0.1f);
        backRT.anchorMax = new Vector2(0.5f, 0.1f);
        backRT.sizeDelta = new Vector2(300, 80);

        GameObject bTextGO = new GameObject("Text");
        bTextGO.transform.SetParent(backGO.transform, false);
        Text bt = bTextGO.AddComponent<Text>();
        bt.text = "Nazad";
        bt.font = defaultFont;
        bt.fontSize = 30;
        bt.color = Color.black;
        bt.alignment = TextAnchor.MiddleCenter;
        bTextGO.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        bTextGO.GetComponent<RectTransform>().anchorMax = Vector2.one;

        infoPanel.SetActive(false);
    }

    // -------------------- BUTTON ACTIONS --------------------

    private void OnARCameraClicked()
    {
        Debug.Log("Pokrećem AR kameru...");
        // SceneManager.LoadScene("ImeTvojeARScene");
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
