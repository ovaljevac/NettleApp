using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Stavi na POI (uniforma). POI mora imati Collider.
// ARCamera mora biti Tag = MainCamera.
public class POIAudioOnClick : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public AudioClip audioClip;

    [Header("Playback")]
    public bool playOnlyOnce = false;
    public bool stopIfClickedAgain = true;
    [Range(0f, 1f)] public float volume = 1f;



    private AudioSource audioSource;
    private bool hasPlayedOnce = false;


    void Awake()
    {
        // AudioSource na samom POI objektu
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;   // 0 = 2D (stabilno u AR). Ako želiš 3D zvuk: stavi 1.
        audioSource.volume = volume;
    }

    void Update()
    {
        if (Camera.main == null) return;

#if ENABLE_INPUT_SYSTEM
        // Touch
        if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            if (t.press.wasPressedThisFrame) TryHitPOI(t.position.ReadValue());
        }
        // Mouse (Editor)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHitPOI(Mouse.current.position.ReadValue());
        }
#else
        if (Input.GetMouseButtonDown(0)) TryHitPOI(Input.mousePosition);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) TryHitPOI(Input.GetTouch(0).position);
#endif
    }

    private void TryHitPOI(Vector2 screenPos)
    {
        // Ako klikneš UI, nemoj okidati POI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                ToggleAudio();
        }
    }

    private void ToggleAudio()
    {
        if (audioClip == null)
        {
            Debug.LogError("POIAudioOnClick: AudioClip nije dodijeljen u Inspectoru.");
            return;
        }

        // play-only-once zaštita
        if (playOnlyOnce && hasPlayedOnce) return;

        // ako već svira
        if (audioSource.isPlaying)
        {
            if (stopIfClickedAgain)
            {
                audioSource.Stop();
            }
            return;
        }

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();

        hasPlayedOnce = true;
    }

}

// Jednostavan billboard (uspravno)
public class BillboardToCameraSimple : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;
        Vector3 dir = transform.position - Camera.main.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }
}
