using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button openShopButton;
    [SerializeField] private Button closeShopButton;
    [SerializeField] private Button openCreditsButton;
    [SerializeField] private Button closeCreditsButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button exitButton;

    [Header("Game Over Popup")]
    [SerializeField] private GameObject gameOverPopup;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button gameOverRestartButton;

    [Header("No Stock Popup")]
    [SerializeField] private GameObject noStockPopup;
    [SerializeField] private Button noStockOkButton;

    [Header("Price Mismatch Popup")]
    [SerializeField] private GameObject priceMismatchPopup;
    [SerializeField] private TextMeshProUGUI priceMismatchText;
    [SerializeField] private Button priceMismatchOkButton;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Panels")]
    [SerializeField] GameObject shopPanel;
    [SerializeField] GameObject creditsPanel;

    private const float GAME_OVER_BUDGET = 200f;
    private const float MIN_AFFORDABLE_PRICE = 1f; // ✅ cheapest stock cost

    private void Start()
    {
        // ✅ First ever launch — redirect immediately to tutorial before any UI setup
        if (!PlayerPrefs.HasKey("HasPlayedBefore"))
        {
            PlayerPrefs.SetInt("HasPlayedBefore", 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("TUTORIAL");
            return;  // ← stops the rest of Start() from running
        }
        gameOverPopup.SetActive(false);
        noStockPopup.SetActive(false);
        priceMismatchPopup.SetActive(false); // ✅
        openShopButton.onClick.AddListener(() => OpenShopPanel());
        closeShopButton.onClick.AddListener(() => CloseShopPanel());
        StartCoroutine(FadeIn());
        shopPanel.SetActive(false);
        creditsPanel.SetActive(false);
        openCreditsButton.onClick.AddListener(() => OpenCreditsPanel());
        closeCreditsButton.onClick.AddListener(() => CloseCreditsPanel());
        tutorialButton.onClick.AddListener(() => Tutorial());
        startButton.onClick.AddListener(OnStartClicked);
        exitButton.onClick.AddListener(OnExitClicked);
        gameOverRestartButton.onClick.AddListener(OnGameOverRestart);
        noStockOkButton.onClick.AddListener(() => noStockPopup.SetActive(false));
        priceMismatchOkButton.onClick.AddListener(() => priceMismatchPopup.SetActive(false)); // ✅
    }
    public void OpenItchIo()
    {
        Application.OpenURL("https://level-origin.itch.io/tindhero");
    }
    private void Tutorial()
    {
        StartCoroutine(FadeIn());
        SceneManager.LoadScene("TUTORIAL");
    }
    public void OpenCreditsPanel()
    {
        creditsPanel.SetActive(true);
        
    }
    public void CloseCreditsPanel()
    {
        creditsPanel.SetActive(false);
    }
    public void OpenShopPanel()
    {
        shopPanel.SetActive(true);
    }
    public void CloseShopPanel()
    {
        shopPanel.SetActive(false);
    }
    public void StartGame()
    {
        SceneManager.LoadScene("GAMEPLAY");
    }
    public void ExitGame()
    {
        Application.Quit();
    }
    private void OnStartClicked()
    {
        if (IsGameOver())
        {
            ShowGameOverPopup();
            return;
        }

        if (HasNoStocks())
        {
            noStockPopup.SetActive(true);
            return;
        }

        // ✅ Check if all stocked products are too expensive for all NPCs
        if (AllProductsTooExpensive())
        {
            ShowPriceMismatchPopup();
            return;
        }

        StartCoroutine(FadeToGameplay());
    }
    private void ShowPriceMismatchPopup()
    {
        // ✅ Build list of mismatched products
        float highestNpcPreference = 0f;
        foreach (NpcData npc in NpcManager.Instance.AllNpcs)
            if (npc.maxPreferredPrice > highestNpcPreference)
                highestNpcPreference = npc.maxPreferredPrice;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"All your stocked products are above the max NPC preferred price of ₱{highestNpcPreference:N2}!\n");
        sb.AppendLine("Overpriced products:");

        foreach (Product p in StockManager.Instance.Products)
            if (p.quantity > 0 && p.sellingPrice > highestNpcPreference)
                sb.AppendLine($"• {p.productName} — ₱{p.sellingPrice:N2}");

        sb.AppendLine("\nLower your selling prices or add Customers with higher preferred prices.");
        priceMismatchText.text = sb.ToString();
        priceMismatchPopup.SetActive(true);
    }
    private bool AllProductsTooExpensive()
    {
        // ✅ Get max preferred price across all NPCs
        float highestNpcPreference = 0f;
        foreach (NpcData npc in NpcManager.Instance.AllNpcs)
            if (npc.maxPreferredPrice > highestNpcPreference)
                highestNpcPreference = npc.maxPreferredPrice;

        // ✅ Check if ANY stocked product is within ANY NPC's range
        foreach (Product p in StockManager.Instance.Products)
        {
            if (p.quantity > 0 && p.sellingPrice <= highestNpcPreference)
                return false; // ✅ at least one product is affordable — good to go
        }

        return true; // ✅ all stocked products are too expensive for all NPCs
    }

    private bool IsGameOver()
    {
        float money = PlayerManager.Instance.CurrentMoney;

        // ✅ Check if player can afford ANY product
        bool canAffordAnything = false;
        foreach (Product p in StockManager.Instance.Products)
        {
            if (money >= p.baseCost)
            {
                canAffordAnything = true;
                break;
            }
        }

        // ✅ Game over if broke AND has no stocks
        bool hasStocks = false;
        foreach (Product p in StockManager.Instance.Products)
        {
            if (p.quantity > 0)
            {
                hasStocks = true;
                break;
            }
        }

        return !canAffordAnything && !hasStocks;
    }

    private bool HasNoStocks()
    {
        foreach (Product p in StockManager.Instance.Products)
            if (p.quantity > 0)
                return false;
        return true;
    }

    private void ShowGameOverPopup()
    {
        gameOverText.text = $"You've run out of money and stocks!\nRestarting with ₱ {GAME_OVER_BUDGET:N2}";
        gameOverPopup.SetActive(true);
    }

    private void OnGameOverRestart()
    {
        // ✅ Reset money to 200
        PlayerManager.Instance.SetMoney(GAME_OVER_BUDGET);

        // ✅ Reset all stock quantities to 0
        foreach (Product p in StockManager.Instance.Products)
            p.quantity = 0;

        StockManager.Instance.Save();

        gameOverPopup.SetActive(false);
    }

    private void OnExitClicked()
    {
        Application.Quit();
    }
    

    private IEnumerator FadeToGameplay()
    {
        // ✅ Fade to black
        yield return StartCoroutine(FadeOut());

        SceneManager.LoadScene("GAMEPLAY");
    }

    private IEnumerator FadeIn()
    {
        // ✅ Start black, fade to clear
        fadeImage.gameObject.SetActive(true);
        Color c = Color.black;
        c.a = 1f;
        fadeImage.color = c;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - (elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        fadeImage.gameObject.SetActive(true);
        Color c = Color.black;
        c.a = 0f;
        fadeImage.color = c;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = elapsed / fadeDuration;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }
}