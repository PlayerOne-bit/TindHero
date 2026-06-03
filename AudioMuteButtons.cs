using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class AudioMuteButtons : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusicEntry
    {
        [Tooltip("Exact scene name from Build Settings")]
        public string sceneName;

        [Tooltip("Music to play when this scene starts")]
        public AudioClip musicClip;
    }

    [System.Serializable]
    public class ButtonSprites
    {
        [Header("Default State")]
        public Sprite onDefault;
        public Sprite offDefault;

        [Header("Pressed State")]
        public Sprite onPressed;
        public Sprite offPressed;
    }

    [Header("BGM Button")]
    [SerializeField] private Button bgmButton;
    [SerializeField] private ButtonSprites bgmSprites;

    [Header("SFX Button")]
    [SerializeField] private Button sfxButton;
    [SerializeField] private ButtonSprites sfxSprites;

    [Header("Button Click Sound")]
    [SerializeField] private AudioClip buttonClickSFX;

    [Header("Music Per Scene")]
    [SerializeField] private SceneMusicEntry[] sceneMusicEntries;

    private Image bgmImage;
    private Image sfxImage;

    private bool bgmPressed;
    private bool sfxPressed;

    private void Awake()
    {
        bgmImage = bgmButton.GetComponent<Image>();
        sfxImage = sfxButton.GetComponent<Image>();

        bgmButton.transition = Selectable.Transition.None;
        sfxButton.transition = Selectable.Transition.None;

        bgmButton.onClick.RemoveAllListeners();
        sfxButton.onClick.RemoveAllListeners();

        bgmButton.onClick.AddListener(OnBGMButtonClicked);
        sfxButton.onClick.AddListener(OnSFXButtonClicked);

        AddTriggerEvents(bgmButton, true);
        AddTriggerEvents(sfxButton, false);
    }

    private void Start()
    {
        PlayMusicForCurrentScene();
        StartCoroutine(RefreshAfterMusic());
    }

    // ==========================================================
    // SCENE CHANGE HANDLER
    // ==========================================================

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-find buttons by name in case they were destroyed and recreated
        bgmButton = GameObject.Find("BGMButton")?.GetComponent<Button>();
        sfxButton = GameObject.Find("SFXButton")?.GetComponent<Button>();

        if (bgmButton != null)
        {
            bgmImage = bgmButton.GetComponent<Image>();
            bgmButton.transition = Selectable.Transition.None;
            bgmButton.onClick.RemoveAllListeners();
            bgmButton.onClick.AddListener(OnBGMButtonClicked);
            AddTriggerEvents(bgmButton, true);
        }

        if (sfxButton != null)
        {
            sfxImage = sfxButton.GetComponent<Image>();
            sfxButton.transition = Selectable.Transition.None;
            sfxButton.onClick.RemoveAllListeners();
            sfxButton.onClick.AddListener(OnSFXButtonClicked);
            AddTriggerEvents(sfxButton, false);
        }

        PlayMusicForCurrentScene();
        StartCoroutine(RefreshAfterMusic());
    }

    // ==========================================================
    // MUSIC PER SCENE
    // ==========================================================

    private void PlayMusicForCurrentScene()
    {
        if (AudioManager.Instance == null)
            return;

        string currentScene = SceneManager.GetActiveScene().name;

        foreach (SceneMusicEntry entry in sceneMusicEntries)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.sceneName)) continue;

            if (entry.sceneName == currentScene)
            {
                if (entry.musicClip != null)
                    AudioManager.Instance.PlayMusic(entry.musicClip);
                return;
            }
        }
    }

    private System.Collections.IEnumerator RefreshAfterMusic()
    {
        float timeout = 0.5f;
        float elapsed = 0f;

        while (AudioManager.Instance != null &&
               !AudioManager.Instance.IsMusicPlaying() &&
               elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        RefreshUI();
    }

    // ==========================================================
    // BUTTON CLICK HANDLERS
    // ==========================================================

    private void OnBGMButtonClicked()
    {
        if (AudioManager.Instance == null) return;

        if (buttonClickSFX != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX);

        AudioManager.Instance.ToggleMusicMute();

        bgmPressed = false;
        RefreshUI();
    }

    private void OnSFXButtonClicked()
    {
        if (AudioManager.Instance == null) return;

        if (buttonClickSFX != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX);

        AudioManager.Instance.ToggleSFXMute();

        sfxPressed = false;
        RefreshUI();
    }

    // ==========================================================
    // POINTER EVENTS
    // ==========================================================

    private void AddTriggerEvents(Button button, bool isBGM)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry down = new EventTrigger.Entry();
        down.eventID = EventTriggerType.PointerDown;
        down.callback.AddListener((_) =>
        {
            if (isBGM) bgmPressed = true;
            else sfxPressed = true;
            RefreshUI();
        });

        EventTrigger.Entry up = new EventTrigger.Entry();
        up.eventID = EventTriggerType.PointerUp;
        up.callback.AddListener((_) =>
        {
            if (isBGM) bgmPressed = false;
            else sfxPressed = false;
            RefreshUI();
        });

        trigger.triggers.Add(down);
        trigger.triggers.Add(up);
    }

    // ==========================================================
    // UI REFRESH
    // ==========================================================

    private void RefreshUI()
    {
        if (AudioManager.Instance == null) return;
        if (bgmImage == null || sfxImage == null) return;

        bool bgmMuted = AudioManager.Instance.IsMusicMuted();
        bool sfxMuted = AudioManager.Instance.IsSFXMuted();
        bool musicExists = AudioManager.Instance.IsMusicPlaying();

        bgmImage.sprite = GetSprite(bgmSprites, bgmMuted || !musicExists, bgmPressed);
        sfxImage.sprite = GetSprite(sfxSprites, sfxMuted, sfxPressed);
    }

    private Sprite GetSprite(ButtonSprites sprites, bool muted, bool pressed)
    {
        if (muted)
            return pressed ? sprites.offPressed : sprites.offDefault;
        else
            return pressed ? sprites.onPressed : sprites.onDefault;
    }
}