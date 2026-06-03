using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CreateNpcPanel : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private TMP_InputField maxPriceField;

    [Header("Sprite")]
    [SerializeField] private Button uploadSpriteButton;
    [SerializeField] private Image spritePreview;
    [SerializeField] private Sprite defaultPreviewSprite;

    [Header("Dialogues")]
    [SerializeField] private TMP_InputField happyDialogueField;
    [SerializeField] private TMP_InputField angryDialogueField;

    [Header("Happy Audio")]
    [SerializeField] private Button recordHappyButton;
    [SerializeField] private Button previewHappyButton;
    [SerializeField] private TextMeshProUGUI happyRecordStatus;

    [Header("Angry Audio")]
    [SerializeField] private Button recordAngryButton;
    [SerializeField] private Button previewAngryButton;
    [SerializeField] private TextMeshProUGUI angryRecordStatus;

    [Header("Talking Audio")]
    [SerializeField] private Button recordTalkingButton;
    [SerializeField] private Button previewTalkingButton;
    [SerializeField] private TextMeshProUGUI talkingRecordStatus;

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI costDisplay;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Buttons")]
    [SerializeField] private Button createButton;

    [Header("References")]
    [SerializeField] private NpcPanel npcPanel;

    private Sprite uploadedSprite;
    private AudioClip happyClip;
    private AudioClip angryClip;
    private AudioClip talkingClip;
    private AudioSource audioSource;

    private float maxPrice;
    private bool hasValidName;
    private bool hasValidPrice;
    private bool hasSprite;
    private bool hasHappyAudio;
    private bool hasAngryAudio;
    private bool hasTalkingAudio;
    private bool hasHappyDialogue;
    private bool hasAngryDialogue;

    private const int RECORD_SECONDS = 3;
    private const int SAMPLE_RATE = 44100;

    private void Start()
    {
        // ✅ AudioSource for previewing recordings
        audioSource = gameObject.AddComponent<AudioSource>();

        createButton.interactable = false;
        errorText.gameObject.SetActive(false);
        costDisplay.text = "Cost: ₱ 0.00";

        // ✅ Preview buttons disabled until recorded
        previewHappyButton.interactable = false;
        previewAngryButton.interactable = false;
        previewTalkingButton.interactable = false;

        happyRecordStatus.text = "Not recorded";
        angryRecordStatus.text = "Not recorded";
        talkingRecordStatus.text = "Not recorded";

        nameField.onValueChanged.AddListener(OnNameChanged);
        maxPriceField.onValueChanged.AddListener(OnPriceChanged);
        happyDialogueField.onValueChanged.AddListener(OnHappyDialogueChanged);
        angryDialogueField.onValueChanged.AddListener(OnAngryDialogueChanged);

        uploadSpriteButton.onClick.AddListener(OnUploadSpriteClicked);

        recordHappyButton.onClick.AddListener(() => StartCoroutine(RecordAudio(
            clip => {
                happyClip = clip;
                hasHappyAudio = true;
                previewHappyButton.interactable = true;
                RefreshCreateButton();
            },
            happyRecordStatus // ✅ pass status text
        )));

        recordAngryButton.onClick.AddListener(() => StartCoroutine(RecordAudio(
            clip => {
                angryClip = clip;
                hasAngryAudio = true;
                previewAngryButton.interactable = true;
                RefreshCreateButton();
            },
            angryRecordStatus // ✅ pass status text
        )));

        recordTalkingButton.onClick.AddListener(() => StartCoroutine(RecordAudio(
            clip => {
                talkingClip = clip;
                hasTalkingAudio = true;
                previewTalkingButton.interactable = true;
                RefreshCreateButton();
            },
            talkingRecordStatus // ✅ pass status text
        )));

        previewHappyButton.onClick.AddListener(() => PreviewAudio(happyClip));
        previewAngryButton.onClick.AddListener(() => PreviewAudio(angryClip));
        previewTalkingButton.onClick.AddListener(() => PreviewAudio(talkingClip));

        createButton.onClick.AddListener(OnCreateClicked);
    }

    private void OnNameChanged(string value)
    {
        hasValidName = !string.IsNullOrWhiteSpace(value);
        errorText.gameObject.SetActive(false);
        RefreshCreateButton();
    }

    private void OnHappyDialogueChanged(string value)
    {
        hasHappyDialogue = !string.IsNullOrWhiteSpace(value);
        RefreshCreateButton();
    }

    private void OnAngryDialogueChanged(string value)
    {
        hasAngryDialogue = !string.IsNullOrWhiteSpace(value);
        RefreshCreateButton();
    }

    private void OnPriceChanged(string value)
    {
        errorText.gameObject.SetActive(false);

        if (string.IsNullOrWhiteSpace(value))
        {
            hasValidPrice = false;
            maxPrice = 0f;
            costDisplay.text = "Cost: ₱ 0.00";
            RefreshCreateButton();
            return;
        }

        if (!float.TryParse(value, out maxPrice))
        {
            hasValidPrice = false;
            errorText.text = "Max price must be a number.";
            errorText.gameObject.SetActive(true);
            costDisplay.text = "Cost: ₱ 0.00";
            RefreshCreateButton();
            return;
        }

        if (maxPrice <= 0f)
        {
            hasValidPrice = false;
            errorText.text = "Max price must be greater than 0.";
            errorText.gameObject.SetActive(true);
            costDisplay.text = "Cost: ₱ 0.00";
            RefreshCreateButton();
            return;
        }

        // ✅ Valid — update cost live
        hasValidPrice = true;
        float cost = maxPrice * 1.25f;
        costDisplay.text = $"Cost: ₱ {cost:N2}";
        RefreshCreateButton();
    }

    private void OnUploadSpriteClicked()
    {
#if UNITY_ANDROID
        NativeGallery.GetImageFromGallery(
            (path) =>
            {
                if (path == null) return;

                Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize: 512);
                if (texture == null) return;

                uploadedSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                spritePreview.sprite = uploadedSprite;
                spritePreview.preserveAspect = false; // ✅ stretch to fill
                spritePreview.type = Image.Type.Simple; // ✅ simple stretch
                hasSprite = true;
                RefreshCreateButton();
            },
            "Select NPC Image"
        );
#endif
    }

    /// <summary>
    /// Records audio for RECORD_SECONDS then stops automatically.
    /// </summary>
    private IEnumerator RecordAudio(System.Action<AudioClip> onComplete,
                                 TextMeshProUGUI statusText)
    {
        if (!Microphone.devices.Length.Equals(0))
        {
            if (AudioManager.Instance != null)
            {
                if (!AudioManager.Instance.IsMusicMuted())
                    AudioManager.Instance.ToggleMusicMute();
                if (!AudioManager.Instance.IsSFXMuted())
                    AudioManager.Instance.ToggleSFXMute();
            }

            AudioClip recording = Microphone.Start(null, false, RECORD_SECONDS, SAMPLE_RATE);

            // ✅ Countdown display
            float elapsed = 0f;
            while (elapsed < RECORD_SECONDS)
            {
                int current = Mathf.FloorToInt(elapsed);
                statusText.text = $"{current}/{RECORD_SECONDS} seconds";
                elapsed += Time.deltaTime;
                yield return null;
            }

            Microphone.End(null);

            if (AudioManager.Instance != null)
            {
                if (AudioManager.Instance.IsMusicMuted())
                    AudioManager.Instance.ToggleMusicMute();
                if (AudioManager.Instance.IsSFXMuted())
                    AudioManager.Instance.ToggleSFXMute();
            }

            statusText.text = "Recorded ✓"; // ✅ final status
            onComplete?.Invoke(recording);
        }
        else
        {
            errorText.text = "No microphone detected.";
            errorText.gameObject.SetActive(true);
        }
    }

    private void PreviewAudio(AudioClip clip)
    {
        if (clip == null) return;

        // Mute BGM during preview
        if (AudioManager.Instance != null && !AudioManager.Instance.IsMusicMuted())
            AudioManager.Instance.ToggleMusicMute();

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();

        StartCoroutine(UnmuteAfterPreview(clip.length));
    }

    private IEnumerator UnmuteAfterPreview(float duration)
    {
        yield return new WaitForSeconds(duration);

        // Unmute BGM after preview finishes
        if (AudioManager.Instance != null && AudioManager.Instance.IsMusicMuted())
            AudioManager.Instance.ToggleMusicMute();
    }


    private void RefreshCreateButton()
    {
        bool allFieldsFilled = hasValidName
                            && hasValidPrice
                            && hasSprite
                            && hasHappyAudio
                            && hasAngryAudio
                            && hasTalkingAudio
                            && hasHappyDialogue
                            && hasAngryDialogue;

        if (!allFieldsFilled)
        {
            createButton.interactable = false;
            return;
        }

        float cost = maxPrice * 1.25f;
        bool canAfford = PlayerManager.Instance.CurrentMoney >= cost;

        createButton.interactable = canAfford;

        if (!canAfford)
        {
            errorText.text = $"Not enough money. Need ₱ {cost:N2}.";
            errorText.gameObject.SetActive(true);
        }
        else
        {
            errorText.gameObject.SetActive(false);
        }
    }

    private void OnCreateClicked()
    {
        bool success = NpcManager.Instance.AddCustomNpc(
            nameField.text.Trim(),
            uploadedSprite,
            maxPrice,
            happyDialogueField.text.Trim(),
            angryDialogueField.text.Trim(),
            happyClip, -1,
            angryClip, -1,
            talkingClip, -1
        );

        if (success)
        {
            // ✅ Add new row to NpcPanel
            NpcData newNpc = NpcManager.Instance.CustomNpcs[^1];
            npcPanel.AddCustomRow(newNpc);

            ResetForm();
        }
        else
        {
            errorText.text = "Not enough money to create this NPC.";
            errorText.gameObject.SetActive(true);
        }
    }

    private void ResetForm()
    {
        nameField.text = "";
        maxPriceField.text = "";
        happyDialogueField.text = "";
        angryDialogueField.text = "";
        spritePreview.sprite = defaultPreviewSprite;
        uploadedSprite = null;
        happyClip = null;
        angryClip = null;
        talkingClip = null;
        costDisplay.text = "Cost: ₱ 0.00";
        errorText.gameObject.SetActive(false);

        hasValidName = false;
        hasValidPrice = false;
        hasSprite = false;
        hasHappyAudio = false;
        hasAngryAudio = false;
        hasTalkingAudio = false;
        hasHappyDialogue = false;
        hasAngryDialogue = false;

        previewHappyButton.interactable = false;
        previewAngryButton.interactable = false;
        previewTalkingButton.interactable = false;

        happyRecordStatus.text = "Not recorded";
        angryRecordStatus.text = "Not recorded";
        talkingRecordStatus.text = "Not recorded";

        maxPrice = 0f;
        createButton.interactable = false;
    }
}