using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ARBackButton : MonoBehaviour
{
    [Header("Back Button Icon")]
    public Sprite backIconSprite;   // postavi u Inspectoru

    void Start()
    {
        CreateBackButton();
    }

    private void CreateBackButton()
    {
        // ================= CANVAS =================
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // ================= BACK ICON BUTTON =================
        GameObject backGO = new GameObject("ARBackIconButton");
        backGO.transform.SetParent(canvas.transform, false);

        Image backImg = backGO.AddComponent<Image>();
        backImg.sprite = backIconSprite;
        backImg.preserveAspect = true;
        backImg.color = Color.white;

        Button backBtn = backGO.AddComponent<Button>();
        backBtn.transition = Selectable.Transition.None;
        backBtn.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("MainMenu"); // ← TAČNO IME TVOJE UI SCENE
        });

        // Hover / tap feedback (ako već koristiš ovu skriptu)
    

        // ================= POSITION: DONJI LIJEVI UGAO =================
        RectTransform rt = backGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);

        rt.anchoredPosition = new Vector2(32f, 32f); // margina od ivica
        rt.sizeDelta = new Vector2(72, 72);          // veličina ikonice
    }
}
