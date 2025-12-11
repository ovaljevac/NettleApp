using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;   // za novi Input System (UI)

public class MainMenuUI : MonoBehaviour
{
    [Header("Background")]
    public Sprite backgroundSprite;   // ovdje prevučeš KoprivaBackground

    private Canvas canvas;
    private Font defaultFont;

    // ----- MAIN MENU -----
    private GameObject menuPanel;

    // ----- QUIZ -----
    private GameObject quizPanel;
    private Text quizQuestionText;
    private GameObject quizAnswersContainer;
    private List<Button> quizAnswerButtons = new List<Button>();
    private Text quizResultText;
    private Button quizBackToMenuButton;

    private class QuizQuestion
    {
        public string question;
        public string[] answers;
        public int correctIndex;

        public QuizQuestion(string q, string[] a, int correct)
        {
            question = q;
            answers = a;
            correctIndex = correct;
        }
    }

    private List<QuizQuestion> quizQuestions;
    private int currentQuestionIndex = 0;
    private int correctAnswers = 0;

    private Color normalButtonColor = new Color(0.85f, 0.95f, 0.85f, 1f);
    private Color correctColor = new Color(0.2f, 0.5f, 1f, 1f); // plavo
    private Color wrongColor = new Color(1f, 0.3f, 0.3f, 1f);   // crveno

    private void Awake()
    {
        // Unity 6: umjesto Arial.ttf
        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CreateCanvas();
        CreateBackground();
        CreateEventSystem();
        CreateMenuButtons();
        SetupQuizData();
        CreateQuizUI();
    }

    // =====================  CANVAS / BACKGROUND  =====================

    private void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.layer = LayerMask.NameToLayer("UI");

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        canvasGO.transform.SetParent(this.transform, false);
    }

    private void CreateBackground()
    {
        if (backgroundSprite == null) return;

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvas.transform, false);

        Image img = bgGO.AddComponent<Image>();
        img.sprite = backgroundSprite;
        img.preserveAspect = false;
        img.color = Color.white;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        bgGO.transform.SetAsFirstSibling();
    }

    private void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();  // novi Input System
        }
    }

    // =====================  MAIN MENU BUTTONS  =====================

    private void CreateMenuButtons()
    {
        GameObject panelGO = new GameObject("MenuPanel");
        panelGO.transform.SetParent(canvas.transform, false);
        menuPanel = panelGO;

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(400f, 300f);

        VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 15f;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = panelGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateMenuButton(panelGO.transform, "Pokreni AR kameru", OnARCameraClicked);
        CreateMenuButton(panelGO.transform, "Kviz znanja", OnQuizClicked);
        CreateMenuButton(panelGO.transform, "Info o biljci", OnInfoClicked);
    }

    private void CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(label);
        buttonGO.transform.SetParent(parent, false);

        Image img = buttonGO.AddComponent<Image>();
        img.color = normalButtonColor;

        Button button = buttonGO.AddComponent<Button>();

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 70f);

        LayoutElement le = buttonGO.AddComponent<LayoutElement>();
        le.preferredHeight = 70f;
        le.preferredWidth = 300f;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        Text text = textGO.AddComponent<Text>();
        text.text = label;
        text.font = defaultFont;
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;

        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        button.onClick.AddListener(onClick);
    }

    // =====================  QUIZ DATA  =====================

    private void SetupQuizData()
    {
        quizQuestions = new List<QuizQuestion>();

        quizQuestions.Add(new QuizQuestion(
            "Koji je latinski naziv obične koprive?",
            new string[]
            {
                "Urtica dioica",
                "Mentha piperita",
                "Taraxacum officinale",
                "Rosa canina"
            },
            0
        ));

        quizQuestions.Add(new QuizQuestion(
            "Zašto kopriva \"peče\" kožu kada je dotaknemo?",
            new string[]
            {
                "Zbog sitnih dlačica ispunjenih iritirajućom tekućinom",
                "Zbog oštrog trnja na stabljici",
                "Zbog otrovnih bobica",
                "Zbog mirisnih ulja u listu"
            },
            0
        ));

        quizQuestions.Add(new QuizQuestion(
            "Koji dio koprive se najčešće koristi za čaj?",
            new string[]
            {
                "Listovi",
                "Korijen",
                "Cvijet",
                "Sjeme"
            },
            0
        ));
    }

    // =====================  QUIZ UI  =====================

    private void CreateQuizUI()
    {
        quizPanel = new GameObject("QuizPanel");
        quizPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRT = quizPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(600f, 500f);

        VerticalLayoutGroup vlg = quizPanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 15f;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        // Pitanje
        GameObject qTextGO = new GameObject("QuestionText");
        qTextGO.transform.SetParent(quizPanel.transform, false);
        quizQuestionText = qTextGO.AddComponent<Text>();
        quizQuestionText.font = defaultFont;
        quizQuestionText.fontSize = 30;
        quizQuestionText.alignment = TextAnchor.MiddleCenter;
        quizQuestionText.color = Color.white;
        RectTransform qrt = qTextGO.GetComponent<RectTransform>();
        qrt.sizeDelta = new Vector2(600f, 150f);

        // Container za odgovore
        quizAnswersContainer = new GameObject("AnswersContainer");
        quizAnswersContainer.transform.SetParent(quizPanel.transform, false);
        RectTransform acrt = quizAnswersContainer.AddComponent<RectTransform>();
        acrt.sizeDelta = new Vector2(600f, 300f);

        VerticalLayoutGroup answersVLG = quizAnswersContainer.AddComponent<VerticalLayoutGroup>();
        answersVLG.childAlignment = TextAnchor.MiddleCenter;
        answersVLG.spacing = 10f;
        answersVLG.childForceExpandHeight = false;
        answersVLG.childForceExpandWidth = true;

        ContentSizeFitter answersCSF = quizAnswersContainer.AddComponent<ContentSizeFitter>();
        answersCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 4 dugmeta za odgovore
        for (int i = 0; i < 4; i++)
        {
            GameObject btnGO = new GameObject("AnswerButton" + i);
            btnGO.transform.SetParent(quizAnswersContainer.transform, false);

            Image img = btnGO.AddComponent<Image>();
            img.color = normalButtonColor;

            Button btn = btnGO.AddComponent<Button>();
            quizAnswerButtons.Add(btn);

            RectTransform brt = btnGO.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(550f, 60f);

            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;
            le.preferredWidth = 550f;

            GameObject tGO = new GameObject("Text");
            tGO.transform.SetParent(btnGO.transform, false);
            Text t = tGO.AddComponent<Text>();
            t.font = defaultFont;
            t.fontSize = 24;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.black;

            RectTransform trt = tGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        // Rezultat
        GameObject resultGO = new GameObject("ResultText");
        resultGO.transform.SetParent(quizPanel.transform, false);
        quizResultText = resultGO.AddComponent<Text>();
        quizResultText.font = defaultFont;
        quizResultText.fontSize = 26;
        quizResultText.alignment = TextAnchor.MiddleCenter;
        quizResultText.color = Color.white;
        RectTransform rrt = resultGO.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(600f, 80f);
        quizResultText.gameObject.SetActive(false);

        // Nazad na meni
        GameObject backBtnGO = new GameObject("BackToMenuButton");
        backBtnGO.transform.SetParent(quizPanel.transform, false);

        Image backImg = backBtnGO.AddComponent<Image>();
        backImg.color = normalButtonColor;

        quizBackToMenuButton = backBtnGO.AddComponent<Button>();
        quizBackToMenuButton.onClick.AddListener(OnBackToMenuClicked);

        RectTransform backRT = backBtnGO.GetComponent<RectTransform>();
        backRT.sizeDelta = new Vector2(300f, 60f);

        GameObject backTextGO = new GameObject("Text");
        backTextGO.transform.SetParent(backBtnGO.transform, false);
        Text backText = backTextGO.AddComponent<Text>();
        backText.text = "Nazad na meni";
        backText.font = defaultFont;
        backText.fontSize = 24;
        backText.alignment = TextAnchor.MiddleCenter;
        backText.color = Color.black;

        RectTransform btrt = backTextGO.GetComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero;
        btrt.anchorMax = Vector2.one;
        btrt.offsetMin = Vector2.zero;
        btrt.offsetMax = Vector2.zero;

        quizBackToMenuButton.gameObject.SetActive(false);

        // Na početku sakriven kviz
        quizPanel.SetActive(false);
    }

    // =====================  BUTTON HANDLERS  =====================

    private void OnARCameraClicked()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private void OnQuizClicked()
    {
        menuPanel.SetActive(false);
        quizPanel.SetActive(true);
        StartQuiz();
    }

    private void OnInfoClicked()
    {
        Debug.Log("Info o biljci (ovdje kasnije možeš dodati poseban ekran).");
    }

    private void OnBackToMenuClicked()
    {
        quizPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    // =====================  QUIZ LOGIC  =====================

    private void StartQuiz()
    {
        currentQuestionIndex = 0;
        correctAnswers = 0;
        quizResultText.gameObject.SetActive(false);
        quizBackToMenuButton.gameObject.SetActive(false);
        quizAnswersContainer.SetActive(true);

        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        QuizQuestion q = quizQuestions[currentQuestionIndex];
        quizQuestionText.text = q.question;

        for (int i = 0; i < quizAnswerButtons.Count; i++)
        {
            Button btn = quizAnswerButtons[i];
            Image img = btn.GetComponent<Image>();
            img.color = normalButtonColor;

            Text txt = btn.GetComponentInChildren<Text>();
            txt.text = q.answers[i];

            int answerIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnAnswerClicked(answerIndex));
            btn.interactable = true;
        }
    }

    private void OnAnswerClicked(int index)
    {
        QuizQuestion q = quizQuestions[currentQuestionIndex];

        foreach (var btn in quizAnswerButtons)
            btn.interactable = false;

        Button clickedBtn = quizAnswerButtons[index];
        Image clickedImg = clickedBtn.GetComponent<Image>();

        if (index == q.correctIndex)
        {
            correctAnswers++;
            clickedImg.color = correctColor; // plavo
        }
        else
        {
            clickedImg.color = wrongColor;   // crveno
            Image correctImg = quizAnswerButtons[q.correctIndex].GetComponent<Image>();
            correctImg.color = correctColor;
        }

        StartCoroutine(NextQuestionCoroutine());
    }

    private IEnumerator NextQuestionCoroutine()
    {
        yield return new WaitForSeconds(1.0f);

        currentQuestionIndex++;

        if (currentQuestionIndex < quizQuestions.Count)
        {
            ShowCurrentQuestion();
        }
        else
        {
            ShowResults();
        }
    }

    private void ShowResults()
    {
        quizAnswersContainer.SetActive(false);
        quizResultText.gameObject.SetActive(true);
        quizResultText.text = $"Imali ste {correctAnswers}/{quizQuestions.Count} tačnih odgovora.";
        quizBackToMenuButton.gameObject.SetActive(true);
    }
}
