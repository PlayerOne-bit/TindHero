using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TutorialScript : MonoBehaviour
{
    [System.Serializable]
    public class TutorialStep
    {
        public GameObject dialogueBox;
        public Image highlightTarget;
        public bool required;
        public bool isGameplayTransition;
    }

    // ── Inspector ──────────────────────────────────────
    [Header("Steps")]
    [SerializeField] private List<TutorialStep> steps;

    [Header("Scene Panels")]
    [SerializeField] private GameObject startScenePanel;
    [SerializeField] private GameObject gameplayScenePanel;

    [Header("Shop")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button shopOpenButton;
    [SerializeField] private Button shopCloseButton;

    [Header("Stock Row")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button refundButton;
    [SerializeField] private TextMeshProUGUI stockQuantityText;

    [Header("Start Button")]
    [SerializeField] private Button startButton;

    [Header("Gameplay — Inventory")]
    [SerializeField] private Button inventoryButton;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button inventoryAddButton;
    [SerializeField] private Button inventorySubtractButton;
    [SerializeField] private TextMeshProUGUI productQuantityText;
    [SerializeField] private TextMeshProUGUI productStockText;
    [SerializeField] private Button inventoryConfirmButton;

    [Header("Gameplay — Denomination")]
    [SerializeField] private Button denominationButton;
    [SerializeField] private GameObject denominationPanel;
    [SerializeField] private Button denomAddButton;
    [SerializeField] private Button denomSubtractButton;
    [SerializeField] private TextMeshProUGUI denomQuantityText;
    [SerializeField] private Button denominationConfirmButton;

    [Header("Transaction")]
    [SerializeField] private Button transactionConfirmButton;

    [Header("Draggable Area")]
    [SerializeField] private RectTransform draggableAreaPanel;
    [SerializeField] private RectTransform customerPanel;
    [SerializeField] private RectTransform returnPanel;

    [Header("Receipt Panel")]
    [SerializeField] private Transform receiptContent;
    [SerializeField] private GameObject receiptRowPrefab;

    [Header("Tutorial Product Draggable")]
    [SerializeField] private Sprite tutorialProductSprite;
    // Product used for receipt display (name + price)
    [SerializeField] private string tutorialProductName = "Sample Product";
    [SerializeField] private float tutorialProductPrice = 7.5f;

    [Header("Tutorial Denomination Draggable")]
    [SerializeField] private Sprite tutorialDenomSprite;
    [SerializeField] private float tutorialDenomValue = 50f;  // ₱50 bill player uses

    [Header("NPC")]
    [SerializeField] private RectTransform npcPanel;
    [SerializeField] private Image npcImage;
    [SerializeField] private Sprite npcSprite;
    [SerializeField] private float npcSlideSpeed = 800f;
    [SerializeField] private GameObject npcDialogueBox;

    [Header("NPC Payment")]
    [SerializeField] private Sprite npcPaymentSprite;   // ₱50 bill sprite
    [SerializeField] private float npcPaymentValue = 50f; // value shown in receipt

    [Header("Overlay")]
    [SerializeField] private TutorialOverlay tutorialOverlay;

    [Header("Navigation")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button skipButton;

    [Header("Money UI")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI moneyChangeText;
    [SerializeField] private GameObject moneyChangePanel;

    // ── Constants ──────────────────────────────────────
    private const float PRODUCT_COST = 7.5f;
    private const int STOCK_MAX = 99;
    private const int NPC_BUY_QTY = 3;
    private const int PRODUCT_STOCK_START = 10;

    // Money checkpoints for silent Prev snaps
    private const float MONEY_START = 200f;
    private const float MONEY_STEP7 = 192.5f;
    private const float MONEY_STEP8 = 117.5f;
    private const float MONEY_STEP9 = 119.38f;
    // Step 28 starts with this balance (after products sold, before final confirm)
    private const float MONEY_STEP28 = 119.38f;

    // ── State ──────────────────────────────────────────
    private int currentStep = 0;
    private bool stepComplete = false;
    private bool inGameplayScene = false;
    private bool npcSpawned = false;

    private int stockCount = 0;
    private int buyStep6Count = 0;
    private int buyStep7Count = 0;
    private int addPressCount = 0;
    private int denomAddCount = 0;
    private int productsDragged = 0;
    private int denomsDragged = 0;
    private int productStockRemaining = PRODUCT_STOCK_START;

    private float tutorialMoney = 200f;
    private float cumulativeChange = 0f;
    private Coroutine hideMoneyCoroutine;

    // Draggable product/denom objects — parented inside overlay clones
    private List<GameObject> spawnedDraggables = new List<GameObject>();

    // ── Persistent overlay panel clones (steps 19–28) ─────────────────────
    // These are created once in step 19 and reused through step 28.
    private RectTransform persistentOverlayDragArea = null;
    private RectTransform persistentOverlayCustomer = null;
    private RectTransform persistentOverlayReturn = null;   // created in step 26, reused in 27–28

    // ── Receipt state (mirrors TransactionPanel) ──────
    // Products dragged to customer in step 19
    private List<(string name, float price, int qty)> receiptProducts = new List<(string, float, int)>();
    // Denominations dragged to customer in step 26
    private List<(float value, int qty)> receiptDenoms = new List<(float, int)>();
    // NPC payment denomination rows shown after step 21
    private List<(float value, int qty)> receiptNpcPayment = new List<(float, int)>();
    // Instantiated receipt row GameObjects
    private List<GameObject> receiptRows = new List<GameObject>();

    // NPC payment sprite — separate lifetime from spawnedDraggables
    private GameObject spawnedNpcPayment = null;
    // Whether the NPC payment was dragged to the return panel (and thus should be destroyed)
    private bool npcPaymentReturnedToReturn = false;

    // ══════════════════════════════════════════════════
    // START
    // ══════════════════════════════════════════════════

    private void Start()
    {
        foreach (var s in steps)
            if (s.dialogueBox != null) s.dialogueBox.SetActive(false);

        gameplayScenePanel.SetActive(false);
        shopPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (denominationPanel != null) denominationPanel.SetActive(false);
        if (npcDialogueBox != null) npcDialogueBox.SetActive(false);
        moneyChangePanel.SetActive(false);

        if (npcPanel != null)
        {
            Canvas c = npcPanel.GetComponentInParent<Canvas>();
            float w = c.GetComponent<RectTransform>().rect.width;
            npcPanel.anchoredPosition = new Vector2(-w, 0f);
        }

        if (inventorySubtractButton != null) inventorySubtractButton.interactable = false;
        if (denomSubtractButton != null) denomSubtractButton.interactable = false;

        prevButton.onClick.AddListener(GoPrev);
        skipButton.onClick.AddListener(EndTutorial);
        prevButton.gameObject.SetActive(false);

        UpdateMoneyDisplay();
        UpdateStockText();
        UpdateProductQty();
        UpdateDenomQty();

        StartCoroutine(RunTutorial());
    }

    // ══════════════════════════════════════════════════
    // MAIN LOOP
    // ══════════════════════════════════════════════════

    private IEnumerator RunTutorial(Rect? fromHole = null)
    {
        bool first = fromHole.HasValue;
        while (currentStep < steps.Count)
        {
            Rect? holeForStep = first ? fromHole : (Rect?)null;
            first = false;
            yield return StartCoroutine(RunStep(currentStep, holeForStep));
            currentStep++;
        }
        EndTutorial();
    }

    private IEnumerator RunStep(int index, Rect? fromHole = null)
    {
        TutorialStep step = steps[index];
        stepComplete = false;

        prevButton.gameObject.SetActive(index > 0);

        foreach (var s in steps)
            if (s.dialogueBox != null) s.dialogueBox.SetActive(false);
        if (step.dialogueBox != null)
            step.dialogueBox.SetActive(true);

        if (step.isGameplayTransition && !inGameplayScene)
        {
            startScenePanel.SetActive(false);
            gameplayScenePanel.SetActive(true);
            inGameplayScene = true;
            yield return null;
        }

        ShowOverlayForStep(step, fromHole);

        yield return StartCoroutine(SetupStep(index));

        yield return new WaitUntil(() => stepComplete);

        tutorialOverlay.onScreenTap = null;
    }

    // ══════════════════════════════════════════════════
    // OVERLAY
    // ══════════════════════════════════════════════════

    private void ShowOverlayForStep(TutorialStep step, Rect? fromHole = null)
    {
        System.Action ghostAction = step.required ? BuildGhostAction(step) : null;

        if (step.highlightTarget != null)
        {
            tutorialOverlay.onScreenTap = step.required ? null : (System.Action)(() => stepComplete = true);

            if (fromHole.HasValue)
                tutorialOverlay.ShowFrom(fromHole.Value, step.highlightTarget, ghostAction);
            else
                tutorialOverlay.Show(step.highlightTarget, ghostAction);
        }
        else
        {
            tutorialOverlay.onScreenTap = () => stepComplete = true;
            if (fromHole.HasValue)
                tutorialOverlay.ShowFullHoleFrom(fromHole.Value);
            else
                tutorialOverlay.ShowFullHole();
        }
    }

    private System.Action BuildGhostAction(TutorialStep step)
    {
        int idx = steps.IndexOf(step);
        switch (idx)
        {
            case 1: return () => { shopPanel.SetActive(true); shopOpenButton.onClick.RemoveListener(OnShopOpen); stepComplete = true; };
            case 5: return () => { OnBuyOnce(); };
            case 7: return () => { OnBuyMultiple(); };
            case 8: return () => { OnRefundOnce(); };
            case 9: return () => { shopPanel.SetActive(false); shopCloseButton.onClick.RemoveListener(OnShopClose); stepComplete = true; };
            case 10: return () => { OnStartPressed(); };
            case 14: return () => { if (inventoryPanel != null) inventoryPanel.SetActive(true); inventoryButton.onClick.RemoveListener(OnInventoryOpen); stepComplete = true; };
            case 17: return () => { OnAddProduct(); };
            case 18: return () => { if (inventoryPanel != null) inventoryPanel.SetActive(false); inventoryConfirmButton.onClick.RemoveListener(OnInventoryConfirm); stepComplete = true; };
            case 21: return () => { OnTransactionConfirm21(); };
            case 22: return () => { if (denominationPanel != null) denominationPanel.SetActive(true); denominationButton.onClick.RemoveListener(OnDenominationOpen); stepComplete = true; };
            case 24: return () => { OnDenomAdd(); };
            case 25: return () => { OnDenomConfirm(); };
            case 28: return () => { OnTransactionConfirm28(); };
            default: return () => stepComplete = true;
        }
    }

    // ══════════════════════════════════════════════════
    // PER-STEP SETUP
    // ══════════════════════════════════════════════════

    private IEnumerator SetupStep(int index)
    {
        switch (index)
        {
            case 0: break;

            case 1:
                shopOpenButton.onClick.RemoveListener(OnShopOpen);
                shopOpenButton.onClick.AddListener(OnShopOpen);
                break;

            case 2: case 3: case 4: break;

            case 5:
                buyStep6Count = 0;
                buyButton.onClick.RemoveListener(OnBuyOnce);
                buyButton.onClick.AddListener(OnBuyOnce);
                break;

            case 6:
                UpdateStockText();
                break;

            case 7:
                buyStep7Count = 0;
                buyButton.onClick.RemoveListener(OnBuyMultiple);
                buyButton.onClick.AddListener(OnBuyMultiple);
                break;

            case 8:
                refundButton.onClick.RemoveListener(OnRefundOnce);
                refundButton.onClick.AddListener(OnRefundOnce);
                break;

            case 9:
                shopCloseButton.onClick.RemoveListener(OnShopClose);
                shopCloseButton.onClick.AddListener(OnShopClose);
                break;

            case 10:
                startButton.onClick.RemoveListener(OnStartPressed);
                startButton.onClick.AddListener(OnStartPressed);
                break;

            case 11:
                if (!npcSpawned)
                {
                    yield return StartCoroutine(SlideNpcIn());
                    npcSpawned = true;
                }
                break;

            case 12: break;

            case 13:
                if (npcDialogueBox != null) npcDialogueBox.SetActive(true);
                tutorialOverlay.onScreenTap = () =>
                {
                    if (npcDialogueBox != null) npcDialogueBox.SetActive(false);
                    stepComplete = true;
                };
                break;

            case 14:
                inventoryButton.onClick.RemoveListener(OnInventoryOpen);
                inventoryButton.onClick.AddListener(OnInventoryOpen);
                break;

            case 15: case 16: break;

            case 17:
                addPressCount = 0;
                productStockRemaining = PRODUCT_STOCK_START;
                UpdateProductQty();
                UpdateProductStockText();
                if (inventorySubtractButton != null) inventorySubtractButton.interactable = false;
                inventoryAddButton.onClick.RemoveListener(OnAddProduct);
                inventoryAddButton.onClick.AddListener(OnAddProduct);
                break;

            case 18:
                inventoryConfirmButton.onClick.RemoveListener(OnInventoryConfirm);
                inventoryConfirmButton.onClick.AddListener(OnInventoryConfirm);
                break;

            // ── [19] Drag 3 products → customer panel ──────────────
            case 19:
                productsDragged = 0;
                receiptProducts.Clear();
                CleanupDraggables();
                ClearReceiptRows();
                yield return StartCoroutine(SpawnProductDragScene());
                tutorialOverlay.onScreenTap = null;
                break;

            // ── [20] Tap to confirm seeing receipt ─────────────────
            case 20:
                // Ensure persistent panels are visible for step 20
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(true);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(true);
                // Receipt already shows x3 from step 19; rebuild if needed
                if (receiptRows.Count == 0)
                {
                    receiptProducts.Clear();
                    receiptProducts.Add((tutorialProductName, tutorialProductPrice, NPC_BUY_QTY));
                    RebuildReceipt();
                }
                tutorialOverlay.onScreenTap = () => stepComplete = true;
                break;

            // ── [21] Confirm transaction → spawns NPC payment ──────
            case 21:
                // Ensure persistent panels are visible for step 21
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(true);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(true);
                transactionConfirmButton.onClick.RemoveListener(OnTransactionConfirm21);
                transactionConfirmButton.onClick.AddListener(OnTransactionConfirm21);
                break;

            // ── [22] Tap screen; NPC payment visible ───────────────
            case 22:
                // Ensure persistent panels are visible for step 22
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(true);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(true);
                break;

            // ── [23] Open denomination panel ───────────────────────
            case 23:
                // Hide the drag/customer panels while the denomination panel UI is active
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(false);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(false);
                denominationButton.onClick.RemoveListener(OnDenominationOpen);
                denominationButton.onClick.AddListener(OnDenominationOpen);
                break;

            // ── [24] Add denomination ×1 ────────────────────────────
            case 24:
                // Keep drag/customer panels hidden during denomination panel interaction
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(false);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(false);
                denomAddCount = 0;
                UpdateDenomQty();
                if (denomSubtractButton != null) denomSubtractButton.interactable = false;
                denomAddButton.onClick.RemoveListener(OnDenomAdd);
                denomAddButton.onClick.AddListener(OnDenomAdd);
                break;

            // ── [25] Confirm denomination → close panel ─────────────
            case 25:
                // Keep drag/customer panels hidden during denomination panel interaction
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(false);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(false);
                denominationConfirmButton.onClick.RemoveListener(OnDenomConfirm);
                denominationConfirmButton.onClick.AddListener(OnDenomConfirm);
                break;

            // ── [26] Drag denom → customer; NPC payment → return ────
            case 26:
                if (denominationPanel != null) denominationPanel.SetActive(false);
                // Re-show drag/customer panels now that denomination panel is closed
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(true);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(true);
                denomsDragged = 0;
                CleanupDraggablesOnly();
                yield return StartCoroutine(SpawnDenomDragScene());
                tutorialOverlay.onScreenTap = null;
                break;

            // ── [27] Tap screen — return panel visible ─────────────
            case 27:
                // Ensure all panels are visible
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(true);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(true);
                if (persistentOverlayReturn != null)
                    persistentOverlayReturn.gameObject.SetActive(true);
                break;

            // ── [28] Final confirm → +₱30 ──────────────────────────
            case 28:
                // Ensure all panels are visible
                if (persistentOverlayDragArea != null)
                    persistentOverlayDragArea.gameObject.SetActive(true);
                if (persistentOverlayCustomer != null)
                    persistentOverlayCustomer.gameObject.SetActive(true);
                if (persistentOverlayReturn != null)
                    persistentOverlayReturn.gameObject.SetActive(true);
                // Silently snap money to 119.38 before the confirm fires
                SnapMoney(MONEY_STEP28);
                // ✅ NPC payment sprite must NOT appear at step 28 — destroy it if somehow still present
                DestroyNpcPayment();
                transactionConfirmButton.onClick.RemoveListener(OnTransactionConfirm28);
                transactionConfirmButton.onClick.AddListener(OnTransactionConfirm28);
                break;

            case 29: break;
        }
        yield return null;
    }

    // ══════════════════════════════════════════════════
    // DRAG SCENES
    // ══════════════════════════════════════════════════

    private IEnumerator SpawnProductDragScene()
    {
        yield return null;  // let layout settle

        // Create persistent overlay clones at original source size (step 19 — first creation)
        if (persistentOverlayDragArea == null)
            persistentOverlayDragArea = ClonePanelIntoOverlayPersistent(draggableAreaPanel);
        if (persistentOverlayCustomer == null)
            persistentOverlayCustomer = ClonePanelIntoOverlayPersistent(customerPanel);

        // Ensure they are visible
        persistentOverlayDragArea.gameObject.SetActive(true);
        persistentOverlayCustomer.gameObject.SetActive(true);

        int dropped = 0;

        for (int i = 0; i < NPC_BUY_QTY; i++)
        {
            GameObject obj = new GameObject($"DragProduct_{i}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            Image img = obj.GetComponent<Image>();
            img.sprite = tutorialProductSprite;
            img.raycastTarget = true;

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.SetParent(persistentOverlayDragArea, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 200f);
            rt.anchoredPosition = new Vector2(
                Random.Range(-80f, 80f),
                Random.Range(-40f, 40f));

            TutorialDraggable drag = obj.AddComponent<TutorialDraggable>();
            drag.Initialize(
                customer: persistentOverlayCustomer,
                returnPnl: null,
                dragArea: persistentOverlayDragArea,
                onDropped: () =>
                {
                    dropped++;
                    // Update receipt: add or increment the product row
                    UpdateReceiptProduct(tutorialProductName, tutorialProductPrice, dropped);
                    if (dropped >= NPC_BUY_QTY)
                        stepComplete = true;
                },
                onReturned: () => { });

            spawnedDraggables.Add(obj);
        }
    }

    private IEnumerator SpawnDenomDragScene()
    {
        yield return null;  // let layout settle

        // Persistent drag area and customer panel already exist from step 19.
        // Create return panel if it doesn't exist yet (first time in step 26).
        if (persistentOverlayReturn == null)
            persistentOverlayReturn = ClonePanelIntoOverlayPersistent(returnPanel);

        persistentOverlayDragArea.gameObject.SetActive(true);
        persistentOverlayCustomer.gameObject.SetActive(true);
        persistentOverlayReturn.gameObject.SetActive(true);

        // ── Player denomination sprite ──────────────────────────
        GameObject denomObj = new GameObject("DragDenom",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        Image denomImg = denomObj.GetComponent<Image>();
        denomImg.sprite = tutorialDenomSprite;
        denomImg.raycastTarget = true;
        denomImg.SetNativeSize();
        RectTransform denomRt = denomObj.GetComponent<RectTransform>();
        denomRt.SetParent(persistentOverlayDragArea, false);
        denomRt.anchorMin = denomRt.anchorMax = new Vector2(0.5f, 0.5f);
        denomRt.pivot = new Vector2(0.5f, 0.5f);
        denomRt.anchoredPosition = new Vector2(
            Random.Range(-60f, 60f),
            Random.Range(-30f, 30f));

        TutorialDraggable denomDrag = denomObj.AddComponent<TutorialDraggable>();
        denomDrag.Initialize(
            customer: persistentOverlayCustomer,
            returnPnl: null,
            dragArea: persistentOverlayDragArea,
            onDropped: () =>
            {
                denomsDragged++;
                // Add denom row to receipt
                UpdateReceiptDenom(tutorialDenomValue, 1);
                if (denomsDragged >= 1)
                    stepComplete = true;
            },
            onReturned: () => { });

        spawnedDraggables.Add(denomObj);

        // ── NPC payment sprite ──────────────────────────────────
        // Spawn or re-attach NPC payment; it goes to return panel, NOT customer panel.
        // Dragging it to customer panel does NOT destroy it — only return panel destroys it.
        if (spawnedNpcPayment == null)
            SpawnNpcPayment(persistentOverlayDragArea, persistentOverlayReturn);
        else
            ReparentNpcPaymentIntoOverlay(persistentOverlayDragArea, persistentOverlayReturn);
    }

    // ══════════════════════════════════════════════════
    // NPC PAYMENT
    // ══════════════════════════════════════════════════

    private void SpawnNpcPayment(RectTransform overlayDragArea, RectTransform overlayReturn)
    {
        if (spawnedNpcPayment != null) return;

        GameObject obj = new GameObject("NpcPayment",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        Image img = obj.GetComponent<Image>();
        img.sprite = npcPaymentSprite;
        img.raycastTarget = true;
        img.SetNativeSize();
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(overlayDragArea, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(60f, 50f);

        npcPaymentReturnedToReturn = false;

        TutorialDraggable drag = obj.AddComponent<TutorialDraggable>();
        drag.Initialize(
            customer: null,           // dragging to customer does nothing (no destroy)
            returnPnl: overlayReturn, // only return panel triggers the callback
            dragArea: overlayDragArea,
            onDropped: () => { },     // dropped on customer — no action, no destroy
            onReturned: () =>
            {
                // Dragged to return panel — mark as returned and destroy
                npcPaymentReturnedToReturn = true;
                DestroyNpcPayment();
            });

        spawnedNpcPayment = obj;
    }

    private void ReparentNpcPaymentIntoOverlay(RectTransform overlayDragArea, RectTransform overlayReturn)
    {
        spawnedNpcPayment.transform.SetParent(overlayDragArea, false);
        spawnedNpcPayment.transform.SetAsLastSibling();

        // Enable raycasts — NpcPayment was spawned with raycastTarget=false in step 21,
        // but now in step 26 the player must be able to drag it to the return panel.
        Image img = spawnedNpcPayment.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;

        npcPaymentReturnedToReturn = false;

        TutorialDraggable drag = spawnedNpcPayment.GetComponent<TutorialDraggable>();
        if (drag != null) Destroy(drag);

        TutorialDraggable newDrag = spawnedNpcPayment.AddComponent<TutorialDraggable>();
        newDrag.Initialize(
            customer: null,           // dragging to customer does nothing (no destroy)
            returnPnl: overlayReturn,
            dragArea: overlayDragArea,
            onDropped: () => { },     // dropped on customer — no action, no destroy
            onReturned: () =>
            {
                // Dragged to return panel — mark as returned and destroy
                npcPaymentReturnedToReturn = true;
                DestroyNpcPayment();
            });
    }

    private void DestroyNpcPayment()
    {
        if (spawnedNpcPayment != null)
        {
            Destroy(spawnedNpcPayment);
            spawnedNpcPayment = null;
        }
    }

    /// <summary>
    /// Called at the start of step 28 to ensure the NPC payment sprite exists in the overlay.
    /// If it was destroyed because the player dragged it to the return panel (or on Prev from 29),
    /// we respawn it so the player can interact with it again.
    /// </summary>
    private void EnsureNpcPaymentForStep28()
    {
        if (spawnedNpcPayment == null && persistentOverlayDragArea != null && persistentOverlayReturn != null)
        {
            npcPaymentReturnedToReturn = false;
            SpawnNpcPayment(persistentOverlayDragArea, persistentOverlayReturn);
        }
        else if (spawnedNpcPayment != null && persistentOverlayDragArea != null && persistentOverlayReturn != null)
        {
            // Reattach draggable logic with correct panels in case they were rebuilt
            ReparentNpcPaymentIntoOverlay(persistentOverlayDragArea, persistentOverlayReturn);
        }
    }

    private void SpawnNpcPaymentDirect()
    {
        if (spawnedNpcPayment != null) return;

        // Ensure the persistent drag area panel exists so we can parent into it.
        // (It was created in step 19 and persists through step 28.)
        if (persistentOverlayDragArea == null)
            persistentOverlayDragArea = ClonePanelIntoOverlayPersistent(draggableAreaPanel);

        GameObject obj = new GameObject("NpcPayment",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        Image img = obj.GetComponent<Image>();
        img.sprite = npcPaymentSprite;
        img.raycastTarget = false;   // not draggable yet — becomes draggable in [26]
        img.SetNativeSize();
        RectTransform rt = obj.GetComponent<RectTransform>();
        // Parent directly under the drag area clone (same as SpawnNpcPayment does in step 26)
        rt.SetParent(persistentOverlayDragArea, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(60f, 50f);

        npcPaymentReturnedToReturn = false;
        spawnedNpcPayment = obj;
    }

    // ══════════════════════════════════════════════════
    // PANEL CLONE HELPERS
    // ══════════════════════════════════════════════════

    /// <summary>
    /// Creates a persistent overlay clone positioned and sized to exactly match the source panel
    /// in screen space. Uses world corners so it works regardless of canvas scale or anchoring.
    /// The clone is tracked via AddOverlayChild for proper cleanup.
    /// </summary>
    private RectTransform ClonePanelIntoOverlayPersistent(RectTransform source)
    {
        GameObject clone = new GameObject(source.name + "_OverlayCopy",
            typeof(RectTransform), typeof(CanvasRenderer));

        Image srcImg = source.GetComponent<Image>();
        if (srcImg != null)
        {
            Image dstImg = clone.AddComponent<Image>();
            dstImg.sprite = srcImg.sprite;
            dstImg.color = srcImg.color;
            dstImg.type = srcImg.type;
            dstImg.raycastTarget = false;
        }

        RectTransform rt = clone.GetComponent<RectTransform>();

        // ── Parent with worldPositionStays=FALSE ─────────────────────────
        // AddOverlayChild uses worldPositionStays=true which lets Unity overwrite
        // anchoredPosition to try to preserve the clone's world pos (0,0,0).
        // We bypass that by parenting directly with false, then tracking manually.
        rt.SetParent(tutorialOverlay.rectTransform, false);
        rt.SetAsLastSibling();
        tutorialOverlay.AddOverlayChild(clone);   // just tracks for cleanup

        // ── Copy source panel's exact position and size ──────────────────
        // Because both the source and the overlay are children of the same
        // Canvas, we read the source's world corners and convert them into
        // the overlay's local rect space using ScreenPointToLocalPoint, which
        // correctly accounts for any Canvas Scaler scaling.
        RectTransform overlayRT = tutorialOverlay.rectTransform;
        Canvas canvas = overlayRT.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                        ? canvas.worldCamera : null;

        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);   // [0]=BL [1]=TL [2]=TR [3]=BR

        Vector2 localBL, localTR;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRT, corners[0], uiCam, out localBL);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRT, corners[2], uiCam, out localTR);

        Vector2 size = new Vector2(localTR.x - localBL.x, localTR.y - localBL.y);
        Vector2 center = (localBL + localTR) * 0.5f;

        // Anchor + pivot at centre so anchoredPosition == offset from overlay centre.
        // Must be set AFTER parenting (with false) so Unity doesn't recalculate.
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = center;

        return rt;
    }

    /// <summary>
    /// Original non-persistent clone used for steps outside the 19–28 range.
    /// </summary>
    private RectTransform ClonePanelIntoOverlay(RectTransform source)
    {
        return ClonePanelIntoOverlayPersistent(source);
    }

    // ══════════════════════════════════════════════════
    // CALLBACKS
    // ══════════════════════════════════════════════════

    private void OnShopOpen()
    {
        shopPanel.SetActive(true);
        shopOpenButton.onClick.RemoveListener(OnShopOpen);
        stepComplete = true;
    }

    private void OnShopClose()
    {
        shopPanel.SetActive(false);
        shopCloseButton.onClick.RemoveListener(OnShopClose);
        stepComplete = true;
    }

    private void OnBuyOnce()
    {
        stockCount++;
        SubtractMoney(PRODUCT_COST);
        UpdateStockText();
        buyButton.onClick.RemoveListener(OnBuyOnce);
        buyStep6Count++;
        stepComplete = true;
    }

    private void OnBuyMultiple()
    {
        stockCount++;
        buyStep7Count++;
        SubtractMoney(PRODUCT_COST);
        UpdateStockText();
        if (buyStep7Count >= 10)
        {
            buyButton.onClick.RemoveListener(OnBuyMultiple);
            stepComplete = true;
        }
    }

    private void OnRefundOnce()
    {
        stockCount = Mathf.Max(0, stockCount - 1);
        AddMoney(PRODUCT_COST * 0.25f);
        UpdateStockText();
        refundButton.onClick.RemoveListener(OnRefundOnce);
        stepComplete = true;
    }

    private void OnStartPressed()
    {
        startButton.onClick.RemoveListener(OnStartPressed);
        stepComplete = true;
    }

    private void OnInventoryOpen()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        inventoryButton.onClick.RemoveListener(OnInventoryOpen);
        stepComplete = true;
    }

    private void OnAddProduct()
    {
        addPressCount++;
        productStockRemaining = PRODUCT_STOCK_START - addPressCount;
        UpdateProductQty();
        UpdateProductStockText();

        if (inventorySubtractButton != null && addPressCount > 0)
            inventorySubtractButton.interactable = true;

        if (addPressCount >= NPC_BUY_QTY)
        {
            inventoryAddButton.onClick.RemoveListener(OnAddProduct);
            stepComplete = true;
        }
    }

    private void OnInventoryConfirm()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        inventoryConfirmButton.onClick.RemoveListener(OnInventoryConfirm);
        stepComplete = true;
    }

    private void OnDenominationOpen()
    {
        if (denominationPanel != null) denominationPanel.SetActive(true);
        denominationButton.onClick.RemoveListener(OnDenominationOpen);
        stepComplete = true;
    }

    private void OnDenomAdd()
    {
        denomAddCount = Mathf.Min(1, denomAddCount + 1);
        UpdateDenomQty();

        if (denomSubtractButton != null)
            denomSubtractButton.interactable = denomAddCount > 0;

        if (denomAddCount >= 1)
        {
            denomAddButton.onClick.RemoveListener(OnDenomAdd);
            stepComplete = true;
        }
    }

    private void OnDenomConfirm()
    {
        if (denominationPanel != null) denominationPanel.SetActive(false);
        denominationConfirmButton.onClick.RemoveListener(OnDenomConfirm);
        stepComplete = true;
    }

    /// <summary>[21] Spawns NPC payment sprite. Receipt stays showing products only.</summary>
    private void OnTransactionConfirm21()
    {
        ClearReceiptRows();
        receiptDenoms.Clear();
        receiptNpcPayment.Clear(); // NPC payment never appears as a receipt row
        // Keep receiptProducts so the product rows stay visible
        RebuildReceipt();

        transactionConfirmButton.onClick.RemoveListener(OnTransactionConfirm21);
        SpawnNpcPaymentDirect();
        stepComplete = true;
    }

    /// <summary>[28] Adds ₱30 revenue after final transaction confirm.</summary>
    private void OnTransactionConfirm28()
    {
        transactionConfirmButton.onClick.RemoveListener(OnTransactionConfirm28);
        AddMoney(30f);
        stepComplete = true;
    }

    // ══════════════════════════════════════════════════
    // NPC SLIDE
    // ══════════════════════════════════════════════════

    private IEnumerator SlideNpcIn()
    {
        if (npcSprite != null) npcImage.sprite = npcSprite;
        Canvas c = npcPanel.GetComponentInParent<Canvas>();
        float w = c.GetComponent<RectTransform>().rect.width;
        npcPanel.anchoredPosition = new Vector2(-w, 0f);
        npcImage.rectTransform.localScale = new Vector3(-1, 1, 1);

        while (Vector2.Distance(npcPanel.anchoredPosition, Vector2.zero) > 1f)
        {
            npcPanel.anchoredPosition = Vector2.MoveTowards(
                npcPanel.anchoredPosition, Vector2.zero, npcSlideSpeed * Time.deltaTime);
            yield return null;
        }
        npcPanel.anchoredPosition = Vector2.zero;
    }

    // ══════════════════════════════════════════════════
    // RECEIPT  (mirrors TransactionPanel.UpdateReceipt)
    // ══════════════════════════════════════════════════

    /// <summary>
    /// Update or add a product entry and rebuild the receipt immediately.
    /// Called live as each product is dragged to the customer panel (step 19).
    /// </summary>
    private void UpdateReceiptProduct(string name, float price, int totalDropped)
    {
        receiptProducts.Clear();
        receiptProducts.Add((name, price, totalDropped));
        RebuildReceipt();
    }

    /// <summary>
    /// Add or increment a denomination entry and rebuild the receipt.
    /// Called live when the player's denom is dropped in step 26.
    /// </summary>
    private void UpdateReceiptDenom(float value, int qty)
    {
        bool found = false;
        for (int i = 0; i < receiptDenoms.Count; i++)
        {
            if (Mathf.Approximately(receiptDenoms[i].value, value))
            {
                receiptDenoms[i] = (value, receiptDenoms[i].qty + qty);
                found = true;
                break;
            }
        }
        if (!found) receiptDenoms.Add((value, qty));
        RebuildReceipt();
    }

    /// <summary>
    /// Rebuild the receipt UI from scratch.
    /// Only shows items actually dragged by the player:
    ///   Products (step 19): "Name x{qty}"
    ///   Player denominations (step 26): "₱ {value:N2} x{qty}"
    /// NPC payment is NOT shown in the receipt — it only goes to the return panel.
    /// </summary>
    private void RebuildReceipt()
    {
        ClearReceiptRows();

        if (receiptContent == null || receiptRowPrefab == null) return;

        // 1) Products dragged to customer (step 19 and beyond)
        foreach (var (name, price, qty) in receiptProducts)
        {
            if (qty <= 0) continue;
            GameObject row = Instantiate(receiptRowPrefab, receiptContent);
            receiptRows.Add(row);
            TextMeshProUGUI tmp = row.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"{name} x{qty} (₱ {price:N2} each)";
        }

        // 2) Player denomination(s) dragged to customer (step 26)
        //    NPC payment rows are intentionally excluded — NPC payment only goes to return panel.
        foreach (var (value, qty) in receiptDenoms)
        {
            if (qty <= 0) continue;
            GameObject row = Instantiate(receiptRowPrefab, receiptContent);
            receiptRows.Add(row);
            TextMeshProUGUI tmp = row.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"₱ {value:N2} x{qty}";
        }
    }

    private void ClearReceiptRows()
    {
        foreach (var row in receiptRows)
            if (row != null) Destroy(row);
        receiptRows.Clear();
    }

    // ══════════════════════════════════════════════════
    // DRAGGABLES CLEANUP
    // ══════════════════════════════════════════════════

    /// <summary>
    /// Destroys only the spawned product/denom draggables (spawnedDraggables list).
    /// Does NOT destroy the persistent overlay panels or the NPC payment sprite.
    /// Used between steps 19–28 where panels must persist.
    /// </summary>
    private void CleanupDraggablesOnly()
    {
        foreach (var obj in spawnedDraggables)
            if (obj != null) Destroy(obj);
        spawnedDraggables.Clear();
        productsDragged = 0;
        denomsDragged = 0;
    }

    /// <summary>
    /// Full cleanup: destroys all draggables AND all overlay children except the NPC payment.
    /// Used when leaving the 19–28 range entirely (e.g., Prev back to step 18 or earlier,
    /// or EndTutorial).
    /// </summary>
    private void CleanupDraggables()
    {
        foreach (var obj in spawnedDraggables)
            if (obj != null) Destroy(obj);
        spawnedDraggables.Clear();
        // Destroy persistent panel references so they get recreated fresh
        persistentOverlayDragArea = null;
        persistentOverlayCustomer = null;
        persistentOverlayReturn = null;
        productsDragged = 0;
        denomsDragged = 0;
    }

    // ══════════════════════════════════════════════════
    // PREV
    // ══════════════════════════════════════════════════

    private void GoPrev()
    {
        if (currentStep <= 0) return;

        Rect outgoing = tutorialOverlay.CurrentHoleRect;
        int targetStep = currentStep - 1;

        // ── Scene ──────────────────────────────────────────────
        if (targetStep < 2) shopPanel.SetActive(false);
        if (targetStep >= 9 && targetStep < 10) shopPanel.SetActive(true);

        if (inGameplayScene && targetStep < 11)
        {
            gameplayScenePanel.SetActive(false);
            startScenePanel.SetActive(true);
            inGameplayScene = false;
        }

        // ── Inventory panel ────────────────────────────────────
        if (targetStep <= 14 && inventoryPanel != null) inventoryPanel.SetActive(false);
        if (targetStep >= 15 && targetStep < 19 && inventoryPanel != null) inventoryPanel.SetActive(true);

        // ── Denomination panel ─────────────────────────────────
        if (denominationPanel != null)
        {
            if (targetStep >= 23 && targetStep <= 26) denominationPanel.SetActive(true);
            else denominationPanel.SetActive(false);
        }

        // ── NPC dialogue ───────────────────────────────────────
        if (npcDialogueBox != null)
            npcDialogueBox.SetActive(targetStep == 13);

        // ── NPC payment lifecycle ──────────────────────────────
        if (targetStep < 21 || targetStep >= 28)
        {
            // Steps 0–20: NPC payment hasn't appeared yet
            // Step 28+: player has already dragged the denom; NPC payment no longer shown
            DestroyNpcPayment();
        }

        // ── Persistent overlay panels: destroy when leaving range 19–28 ──
        if (targetStep < 19)
        {
            // Fully leaving the drag scene range — destroy persistent clones
            CleanupDraggables();
        }
        else
        {
            // Staying within 19–28: only clean up spawned draggables, not panels
            CleanupDraggablesOnly();
        }

        // ── Receipt rows ───────────────────────────────────────
        receiptProducts.Clear();
        receiptDenoms.Clear();
        receiptNpcPayment.Clear();
        ClearReceiptRows();

        if (targetStep >= 19 && targetStep <= 20)
        {
            receiptProducts.Add((tutorialProductName, tutorialProductPrice, NPC_BUY_QTY));
            RebuildReceipt();
        }
        else if (targetStep >= 21 && targetStep <= 26)
        {
            // Products only — NPC payment is never shown as a receipt row
            receiptProducts.Add((tutorialProductName, tutorialProductPrice, NPC_BUY_QTY));
            RebuildReceipt();
        }
        else if (targetStep == 28)
        {
            // Products + player denomination — NPC payment never appears in receipt
            receiptProducts.Add((tutorialProductName, tutorialProductPrice, NPC_BUY_QTY));
            receiptDenoms.Add((tutorialDenomValue, 1));
            RebuildReceipt();
        }

        // ── Money snap ─────────────────────────────────────────
        if (targetStep <= 6)
            SnapMoney(MONEY_START);
        else if (targetStep <= 7)
            SnapMoney(MONEY_STEP7);
        else if (targetStep <= 8)
            SnapMoney(MONEY_STEP8);
        else if (targetStep <= 9)
            SnapMoney(MONEY_STEP9);
        else if (targetStep == 28)
            SnapMoney(MONEY_STEP28);

        // ── Stock counts ───────────────────────────────────────
        if (targetStep <= 5)
        {
            stockCount = 0;
            buyStep6Count = 0;
            buyStep7Count = 0;
            UpdateStockText();
        }
        else if (targetStep <= 6)
        {
            stockCount = Mathf.Max(0, stockCount - buyStep7Count);
            buyStep7Count = 0;
            UpdateStockText();
        }
        else if (targetStep <= 7)
        {
            stockCount = Mathf.Max(0, stockCount - buyStep7Count);
            buyStep7Count = 0;
            UpdateStockText();
        }
        else if (targetStep <= 8)
        {
            stockCount = Mathf.Min(STOCK_MAX, stockCount + 1);
            UpdateStockText();
        }

        // ── Inventory / denom counters ─────────────────────────
        if (targetStep <= 17)
        {
            addPressCount = 0;
            productStockRemaining = PRODUCT_STOCK_START;
            UpdateProductQty();
            UpdateProductStockText();
            if (inventorySubtractButton != null) inventorySubtractButton.interactable = false;
        }
        if (targetStep <= 23)
        {
            denomAddCount = 0;
            UpdateDenomQty();
            if (denomSubtractButton != null) denomSubtractButton.interactable = false;
        }

        currentStep = targetStep;

        tutorialOverlay.onScreenTap = null;
        tutorialOverlay.ClearGhostOnly();
        StopAllCoroutines();

        foreach (var s in steps)
            if (s.dialogueBox != null) s.dialogueBox.SetActive(false);

        StartCoroutine(RunTutorial(outgoing));
    }

    // ══════════════════════════════════════════════════
    // MONEY
    // ══════════════════════════════════════════════════

    private void SnapMoney(float value)
    {
        tutorialMoney = value;
        UpdateMoneyDisplay();
    }

    private void AddMoney(float amount)
    {
        tutorialMoney = Mathf.Max(0f, tutorialMoney + amount);
        UpdateMoneyDisplay();
        ShowMoneyChange(amount);
    }

    private void SubtractMoney(float amount)
    {
        tutorialMoney = Mathf.Max(0f, tutorialMoney - amount);
        UpdateMoneyDisplay();
        ShowMoneyChange(-amount);
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyText != null) moneyText.text = $"₱ {tutorialMoney:N2}";
    }

    private void UpdateStockText()
    {
        if (stockQuantityText != null)
            stockQuantityText.text = $"{stockCount}/{STOCK_MAX}";
    }

    private void UpdateProductQty()
    {
        if (productQuantityText != null)
            productQuantityText.text = $"{addPressCount}/10";
    }

    private void UpdateProductStockText()
    {
        if (productStockText != null)
            productStockText.text = $"Stocks: {productStockRemaining}";
    }

    private void UpdateDenomQty()
    {
        if (denomQuantityText != null)
            denomQuantityText.text = denomAddCount.ToString();
    }

    private void ShowMoneyChange(float change)
    {
        if (moneyChangePanel == null || moneyChangeText == null) return;
        cumulativeChange += change;
        moneyChangeText.text = cumulativeChange >= 0
            ? $"+ ₱ {cumulativeChange:N2}"
            : $"- ₱ {Mathf.Abs(cumulativeChange):N2}";
        moneyChangeText.color = cumulativeChange >= 0 ? Color.green : Color.red;
        moneyChangePanel.SetActive(true);
        if (hideMoneyCoroutine != null) StopCoroutine(hideMoneyCoroutine);
        hideMoneyCoroutine = StartCoroutine(HideMoneyAfterDelay());
    }

    private IEnumerator HideMoneyAfterDelay()
    {
        yield return new WaitForSeconds(5f);
        if (moneyChangePanel != null) moneyChangePanel.SetActive(false);
        cumulativeChange = 0f;
    }

    // ══════════════════════════════════════════════════
    // END
    // ══════════════════════════════════════════════════

    private void EndTutorial()
    {
        tutorialOverlay.onScreenTap = null;
        tutorialOverlay.HideInstant();
        DestroyNpcPayment();
        CleanupDraggables();
        ClearReceiptRows();
        SceneManager.LoadScene("START");
    }

    private void OnDestroy()
    {
        tutorialOverlay.onScreenTap = null;
        DestroyNpcPayment();
        CleanupDraggables();
        ClearReceiptRows();
    }
}