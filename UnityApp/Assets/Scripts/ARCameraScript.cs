using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ARBackButton : MonoBehaviour
{
    void Start()
    {
        CreateBackButton();
    }

    private void CreateBackButton()
    {
        // Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // BACK BUTTON (identičan InfoPanel dugmetu)
        GameObject backGO = new GameObject("ARBackButton");
        backGO.transform.SetParent(canvas.transform, false);

        Image img = backGO.AddComponent<Image>();
        img.color = Color.white;

        Button btn = backGO.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("MainMenu");   // ✨ OVDJE STAVI TAČNO IME TVOJE UI SCENE
        });

        RectTransform rt = backGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.08f);
        rt.anchorMax = new Vector2(0.5f, 0.08f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 80);
        rt.anchoredPosition = Vector2.zero;

        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(backGO.transform, false);

        Text t = textGO.AddComponent<Text>();
        t.text = "Nazad";
        t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.fontSize = 30;
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform txtRT = textGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;
    }
}
