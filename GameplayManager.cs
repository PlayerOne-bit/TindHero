using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private NpcController npcController;
    [SerializeField] private TransactionPanel transactionPanel;
    [SerializeField] private HeartUI heartUI;
    [SerializeField] private TimerBar timerBar;
    [SerializeField] private StageResultPanel stageResultPanel;
    [SerializeField] private IntroDialoguePanel introDialoguePanel;
    [SerializeField] private TextMeshProUGUI customersServed;

    [Header("Settings")]
    [SerializeField] private int maxLives = 5;
    private int npcsPerStage;

    private int currentLives;
    private int npcSpawnedCount = 0;
    private float stageStartTime;
    private float incomeRevenue = 0f;
    private bool roundActive = false;
    private bool stageEnded = false;
    private NpcData currentNpc;
    public int satisfiedCustomers = 0;
    public bool RoundActive => roundActive;

    private void Awake()
    {
        npcsPerStage = Random.Range(20, 31);
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        npcsPerStage = Random.Range(20, 31);
        currentLives = maxLives;
        heartUI.Initialize(maxLives);
        stageStartTime = Time.time;
        npcSpawnedCount = 0;
        incomeRevenue = 0f;
        satisfiedCustomers = 0;
        stageEnded = false;

        UpdateCustomersCount(0);
        if (NpcManager.Instance != null && NpcManager.Instance.AllNpcs.Count > 0)
        {
            StartNextRound();
        }
        else
        {
            Debug.Log("Waiting for NpcManager...");
        }
    }

    public void UpdateCustomersCount(int count)
    {
        customersServed.text = $"Customers Count: {count}/{npcsPerStage}";
    }

    public void OnManagersReady()
    {
        if (stageEnded) return;
        if (npcSpawnedCount > 0) return;

        StartNextRound();
    }

    public void StartNextRound()
    {
        if (stageEnded) return;
        roundActive = false;
        introDialoguePanel.ResetForNextRound();

        if (npcSpawnedCount >= npcsPerStage)
        {
            EndStage(success: true);
            return;
        }

        List<NpcData> allNpcs = NpcManager.Instance.AllNpcs;
        if (allNpcs.Count == 0) return;

        NpcData randomNpc = allNpcs[Random.Range(0, allNpcs.Count)];

        List<Product> affordableProducts = StockManager.Instance.Products.FindAll(
            p => p.HasStock && p.sellingPrice <= randomNpc.maxPreferredPrice);

        if (affordableProducts.Count == 0)
        {
            npcSpawnedCount++;
            StartNextRound();
            return;
        }

        int productTypeCount = Mathf.Min(
            Random.Range(1, 6),
            affordableProducts.Count
        );

        for (int i = affordableProducts.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Product temp = affordableProducts[i];
            affordableProducts[i] = affordableProducts[j];
            affordableProducts[j] = temp;
        }

        List<int> quantities = new List<int>();
        List<string> productNames = new List<string>();
        List<float> productPrices = new List<float>();

        for (int i = 0; i < productTypeCount; i++)
        {
            Product p = affordableProducts[i];
            int qty = Random.Range(1, Mathf.Min(8, p.quantity + 1));
            quantities.Add(qty);
            productNames.Add(p.productName);
            productPrices.Add(p.sellingPrice);
        }

        npcSpawnedCount++;
        npcController.SpawnNextNpc(
            allNpcs,
            quantities.ToArray(),
            productNames.ToArray(),
            productPrices.ToArray()
        );
    }

    public void OnNpcReady(NpcData npc)
    {
        currentNpc = npc;
    }

    public void StartRound()
    {
        if (stageEnded) return;
        roundActive = true;
        transactionPanel.StartProductPhase();

        float payment = npcController.TotalProductCost;
        float extraTime = Mathf.Clamp(payment / 10f, 0f, 60f);
        float roundTime = Random.Range(60f, 90f) + extraTime;
        timerBar.StartTimer(roundTime);
    }

    public void CheckStockEmpty()
    {
        bool allEmpty = true;
        foreach (Product p in StockManager.Instance.Products)
        {
            if (p.quantity > 0)
            {
                allEmpty = false;
                break;
            }
        }

        if (allEmpty)
            EndStage(success: incomeRevenue > 0f);
    }

    public void OnTimerExpired()
    {
        if (!roundActive || stageEnded) return;
        roundActive = false;

        transactionPanel.CancelTransaction();

        StartCoroutine(DelayedNpcLeave());
    }

    private IEnumerator DelayedNpcLeave()
    {
        yield return null;
        npcController.OnTransactionFailed();
    }

    public void OnTransactionConfirmed(float revenue)
    {
        if (!roundActive || stageEnded) return;
        roundActive = false;
        timerBar.StopTimer();
        incomeRevenue += revenue;
        satisfiedCustomers++;
        introDialoguePanel.ShowPostSuccessButtons();

        npcController.OnTransactionSuccess();
    }

    public void RestorePhaseButtons()
    {
        if (transactionPanel.CurrentPhase == TransactionPanel.TransactionPhase.ChangePhase)
            introDialoguePanel.ShowPhase2Buttons();
        else
            introDialoguePanel.ShowPhase1Buttons();
    }

    public void OnTransactionWrong(string errorMessage)
    {
        if (!roundActive || stageEnded) return;
        roundActive = false;
        timerBar.StopTimer();

        transactionPanel.CancelTransaction();

        StartCoroutine(DelayedNpcLeave(errorMessage));
    }

    private IEnumerator DelayedNpcLeave(string errorMessage = "")
    {
        yield return null;
        npcController.OnTransactionFailed(errorMessage);
    }

    public void OnNpcLeft(bool success, bool loseLife)
    {
        if (stageEnded) return;

        if (loseLife)
        {
            currentLives--;
            currentLives = Mathf.Max(0, currentLives);
            heartUI.TakeDamage(1);

            if (currentLives <= 0)
            {
                EndStage(success: false);
                return;
            }
        }

        if (npcSpawnedCount >= npcsPerStage)
        {
            EndStage(success: true);
            return;
        }

        // ✅ Only start next round if CheckStockEmpty didn't already end the stage
        CheckStockEmpty();
        if (!stageEnded)
            StartNextRound();
    }

    public bool ValidateChange(float changeGiven)
    {
        float totalPaid = npcController.TotalProductCost;
        float changeRequired = transactionPanel.PendingGrandTotal - totalPaid;
        return NpcPaymentHandler.IsChangeAcceptable(changeGiven, changeRequired);
    }

    private void EndStage(bool success)
    {
        if (stageEnded) return;
        stageEnded = true;
        roundActive = false;
        timerBar.StopTimer();
        float totalTime = Time.time - stageStartTime;
        stageResultPanel.ShowResult(success, currentLives, totalTime, incomeRevenue);
    }
}