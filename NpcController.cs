using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class NpcController : MonoBehaviour
{
    [Header("NPC Panel")]
    [SerializeField] private RectTransform npcPanel;
    [SerializeField] private Image npcImage;
    [SerializeField] private Button npcTapButton;

    [Header("Movement Settings")]
    [SerializeField] private float slideSpeed = 800f;
    [SerializeField] private float pauseAtCenter = 0.5f;
    [SerializeField] private float delayBetweenNpcs = 3f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("References")]
    [SerializeField] private GameplayManager gameplayManager;
    [SerializeField] private TransactionPanel transactionPanel;
    [SerializeField] private IntroDialoguePanel introDialoguePanel;

    private NpcData currentNpc;
    private bool slideFromLeft;

    private Vector2 offScreenLeft;
    private Vector2 offScreenRight;
    private Vector2 centerPosition = Vector2.zero;

    private int[] currentQuantities;
    private string[] currentProductNames;
    private float[] currentProductPrices;
    private float totalProductCost;
    private List<(float denomination, int quantity)> preGeneratedPayment;
    private float preGeneratedPaymentTotal;
    private int customerCount;
    public NpcData CurrentNpc => currentNpc;
    public float TotalProductCost => totalProductCost;

    public List<(Product product, int quantity)> CurrentOrder
    {
        get
        {
            List<(Product product, int quantity)> order = new List<(Product, int)>();
            for (int i = 0; i < currentProductNames.Length; i++)
            {
                Product p = StockManager.Instance.Products.Find(
                    prod => prod.productName == currentProductNames[i]);
                if (p != null)
                    order.Add((p, currentQuantities[i]));
            }
            return order;
        }
    }

    private void Awake()
    {
        customerCount = 0;
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;

        offScreenLeft = new Vector2(-canvasWidth, 0f);
        offScreenRight = new Vector2(canvasWidth, 0f);

        npcTapButton.onClick.AddListener(OnNpcTapped);
        npcPanel.anchoredPosition = offScreenRight;
    }

    private void OnNpcTapped()
    {
        if (!gameplayManager.RoundActive) return;
        introDialoguePanel.ShowDialogueOnly(
            currentNpc.GetTalkingDialogue(currentQuantities, currentProductNames)
        );
    }

    public void SpawnNextNpc(List<NpcData> allNpcs, int[] quantities,
                             string[] productNames, float[] productPrices)
    {
        currentQuantities = quantities;
        currentProductNames = productNames;
        currentProductPrices = productPrices;

        totalProductCost = 0f;
        for (int i = 0; i < productPrices.Length; i++)
            totalProductCost += productPrices[i] * quantities[i];

        StopAllCoroutines();
        StartCoroutine(SpawnNpcRoutine(allNpcs));
    }

    private IEnumerator SpawnNpcRoutine(List<NpcData> allNpcs)
    {
        preGeneratedPayment = null;
        preGeneratedPaymentTotal = 0f;

        yield return new WaitForSeconds(delayBetweenNpcs);

        currentNpc = allNpcs[Random.Range(0, allNpcs.Count)];
        npcImage.sprite = currentNpc.sprite;

        slideFromLeft = Random.value > 0.5f;
        npcPanel.anchoredPosition = slideFromLeft ? offScreenLeft : offScreenRight;
        npcImage.rectTransform.localScale = slideFromLeft ?
            new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);

        yield return StartCoroutine(SlideTo(centerPosition));
        yield return new WaitForSeconds(pauseAtCenter);
        introDialoguePanel.SetNpcName(currentNpc.npcName);
        // ✅ Pre-generate payment based on player's current money
        bool payExact = Random.value < 0.333f;

        if (payExact)
        {
            preGeneratedPayment = NpcPaymentHandler.GenerateExactPayment(totalProductCost);
        }
        else
        {
            preGeneratedPayment = NpcPaymentHandler.GenerateAffordablePayment(
                totalProductCost,
                PlayerManager.Instance.CurrentMoney
            );
        }

        preGeneratedPaymentTotal = 0f;
        foreach (var (denom, qty) in preGeneratedPayment)
            preGeneratedPaymentTotal += denom * qty;

        PlayAudio(currentNpc.talkingAudio);
        introDialoguePanel.ShowIntroDialogue(
            currentNpc.GetTalkingDialogue(currentQuantities, currentProductNames));

        gameplayManager.OnNpcReady(currentNpc);
        introDialoguePanel.ShowPhase1Buttons();
    }

    public void OnPlayerConfirmed()
    {
        StartCoroutine(NpcPayRoutine());
    }

    private IEnumerator NpcPayRoutine()
    {
        List<(Denomination denomination, int quantity)> denomList =
            new List<(Denomination, int)>();
        float paymentTotal = 0f;

        foreach (var (denom, qty) in preGeneratedPayment)
        {
            Sprite denomSprite = transactionPanel.GetDenominationSprite(denom);
            // ✅ Only add if sprite exists
            if (denomSprite == null)
            {
                Debug.LogWarning($"No sprite for denomination {denom} — skipping");
                continue;
            }
            denomList.Add((new Denomination(denom, denomSprite), qty));
            paymentTotal += denom * qty;
        }

        // ✅ Fallback if all sprites were null
        if (denomList.Count == 0)
        {
            Debug.LogWarning("No valid denominations to spawn — using exact payment");
            preGeneratedPayment = NpcPaymentHandler.GenerateExactPayment(totalProductCost);
            foreach (var (denom, qty) in preGeneratedPayment)
            {
                Sprite denomSprite = transactionPanel.GetDenominationSprite(denom);
                if (denomSprite != null)
                {
                    denomList.Add((new Denomination(denom, denomSprite), qty));
                    paymentTotal += denom * qty;
                }
            }
        }

        // ✅ Always spawn denominations first
        transactionPanel.SetNpcPaymentTotal(paymentTotal);
        transactionPanel.ReceiveDenominations(denomList, paymentTotal, true);

        yield return null;
    }

    public void OnTransactionSuccess()
    {
        StopAllCoroutines();
        StartCoroutine(LeaveRoutine(true, false));
    }

    public void OnTransactionFailed(string errorMessage = "")
    {
        StopAllCoroutines();
        StartCoroutine(LeaveRoutine(false, true, errorMessage));
    }

    private IEnumerator LeaveRoutine(bool success, bool loseLife, string errorMessage = "")
    {
        introDialoguePanel.HideDialogue();

        if (success)
        {
            PlayAudio(currentNpc.happyAudio);
            // ✅ Reset BEFORE showing so old taps don't count
            introDialoguePanel.ResetDialogueTap();
            introDialoguePanel.ShowHappyDialogue(currentNpc.GetHappyDialogue());
        }
        else if (loseLife)
        {
            PlayAudio(currentNpc.angryAudio);
            string msg = string.IsNullOrEmpty(errorMessage) ?
                currentNpc.GetAngryDialogue() :
                $"{currentNpc.GetAngryDialogue()}: {errorMessage}";
            // ✅ Reset BEFORE showing
            introDialoguePanel.ResetDialogueTap();
            introDialoguePanel.ShowAngryDialogue(msg);
        }

        // ✅ Wait for fresh tap only
        if (success || loseLife)
            yield return new WaitUntil(() => introDialoguePanel.WasDialogueTapped());

        introDialoguePanel.HideDialogue();
        npcImage.rectTransform.localScale = new Vector3(1, 1, 1);
        yield return StartCoroutine(SlideTo(offScreenRight));

        gameplayManager.OnNpcLeft(success, loseLife);
        customerCount++;
        GameplayManager.Instance.UpdateCustomersCount(customerCount);
    }

    private IEnumerator SlideTo(Vector2 target)
    {
        while (Vector2.Distance(npcPanel.anchoredPosition, target) > 1f)
        {
            npcPanel.anchoredPosition = Vector2.MoveTowards(
                npcPanel.anchoredPosition,
                target,
                slideSpeed * Time.deltaTime
            );
            yield return null;
        }
        npcPanel.anchoredPosition = target;
    }

    private void PlayAudio(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}