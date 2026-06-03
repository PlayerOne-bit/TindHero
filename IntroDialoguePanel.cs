using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class IntroDialoguePanel : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image dialogueBoxImage;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Dialogue Box Sprites")]
    [SerializeField] private Sprite talkSprite;
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite angrySprite;

    [Header("References")]
    [SerializeField] private GameObject healthBarPanel;
    [SerializeField] private GameObject timerBarPanel;
    [SerializeField] private GameObject npcButton;
    [SerializeField] private GameplayManager gameplayManager;
    [SerializeField] private TextMeshProUGUI npcNameText;

    [Header("Game Buttons")]
    [SerializeField] private GameObject inventoryButton;
    [SerializeField] private GameObject denominationButton;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject confirmButton;
    [SerializeField] private TextMeshProUGUI hintText1;
    [SerializeField] private TextMeshProUGUI hintText2;

    private Image dialoguePanelImage;
    private bool roundStarted = false;
    private bool dialogueTapped = false;

    public enum DialogueType { Talk, Happy, Angry }

    public bool WasDialogueTapped() => dialogueTapped;
    public void ResetDialogueTap() => dialogueTapped = false;

    private void Start()
    {
        dialoguePanelImage = dialoguePanel.GetComponent<Image>();

        dialoguePanel.SetActive(false);
        healthBarPanel.SetActive(false);
        timerBarPanel.SetActive(false);
        
        HideAllButtons();

        if (dialoguePanelImage != null)
            dialoguePanelImage.raycastTarget = false;
    }

    // ══════════════════════════════════════════════════
    // SHOW DIALOGUE METHODS
    // ══════════════════════════════════════════════════

    private void ShowDialogue(string dialogue, DialogueType type)
    {
        dialogueText.text = dialogue;
        dialoguePanel.SetActive(true);

        if (dialogueBoxImage != null)
        {
            switch (type)
            {
                case DialogueType.Happy:
                    dialogueBoxImage.sprite = happySprite;
                    break;
                case DialogueType.Angry:
                    dialogueBoxImage.sprite = angrySprite;
                    break;
                default:
                    dialogueBoxImage.sprite = talkSprite;
                    break;
            }
        }

        // Hide game buttons when any dialogue shows
        SetGameButtonsActive(false);
        npcButton.SetActive(true);

        if (dialoguePanelImage != null)
            dialoguePanelImage.raycastTarget = true;
    }

    public void ShowIntroDialogue(string dialogue) => ShowDialogue(dialogue, DialogueType.Talk);
    public void ShowHappyDialogue(string dialogue) => ShowDialogue(dialogue, DialogueType.Happy);
    public void ShowAngryDialogue(string dialogue) => ShowDialogue(dialogue, DialogueType.Angry);
    public void ShowDialogueOnly(string dialogue) => ShowDialogue(dialogue, DialogueType.Talk);

    // ══════════════════════════════════════════════════
    // HIDE DIALOGUE
    // ══════════════════════════════════════════════════

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        npcButton.SetActive(true);
        SetGameButtonsActive(true);
        if (dialoguePanelImage != null)
            dialoguePanelImage.raycastTarget = false;
    }

    // ══════════════════════════════════════════════════
    // TAP HANDLER
    // ══════════════════════════════════════════════════

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!dialoguePanel.activeSelf) return;

        dialogueTapped = true;
        HideDialogue();

        npcButton.SetActive(true);

        if (!roundStarted)
        {
            roundStarted = true;
            healthBarPanel.SetActive(true);
            timerBarPanel.SetActive(true);
            gameplayManager.StartRound();
        }
        else
        {
            gameplayManager.RestorePhaseButtons();
        }
    }

    // ══════════════════════════════════════════════════
    // BUTTON VISIBILITY
    // ══════════════════════════════════════════════════

    private void HideAllButtons()
    {
        npcButton.SetActive(false);
        inventoryButton.SetActive(false);
        denominationButton.SetActive(false);
        SetGameButtonsActive(false);
    }

    public void ShowPhase1Buttons()
    {
        SetGameButtonsActive(true);
        npcButton.SetActive(true);
        inventoryButton.SetActive(true);
        denominationButton.SetActive(false);
    }

    public void ShowPhase2Buttons()
    {
        SetGameButtonsActive(true);
        npcButton.SetActive(true);
        inventoryButton.SetActive(false);
        denominationButton.SetActive(true);
    }

    public void ShowPostSuccessButtons()
    {
        npcButton.SetActive(false);
        inventoryButton.SetActive(false);
        denominationButton.SetActive(false);
        SetGameButtonsActive(false);
    }

    // ══════════════════════════════════════════════════
    // RESET
    // ══════════════════════════════════════════════════

    public void ResetForNextRound()
    {
        roundStarted = false;
        dialogueTapped = false;
        HideAllButtons();

        if (npcNameText != null)
            npcNameText.text = "";
    }

    // ══════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════

    public void SetNpcName(string name)
    {
        if (npcNameText != null)
            npcNameText.text = name;
    }

    private void SetGameButtonsActive(bool active)
    {
        if (backButton != null) backButton.SetActive(active);
        if (confirmButton != null) confirmButton.SetActive(active);
        if (hintText1 != null) hintText1.gameObject.SetActive(active);
        if (hintText2 != null) hintText2.gameObject.SetActive(active);
    }
}