using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreateProductPanel : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private TMP_InputField sellingPriceField;

    [Header("Sprite")]
    [SerializeField] private Button uploadButton;
    [SerializeField] private Image spritePreview;
    [SerializeField] private Sprite defaultPreviewSprite;

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI costDisplay;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Buttons")]
    [SerializeField] private Button createButton;

    [Header("References")]
    [SerializeField] private StocksPanel stocksPanel;

    private Sprite uploadedSprite;
    private float sellingPrice;
    private bool hasValidPrice;
    private bool hasValidName;
    private bool hasSprite;

    private void Start()
    {
        createButton.interactable = false;
        errorText.gameObject.SetActive(false);
        costDisplay.text = "Cost: ₱ 0.00";

        nameField.onValueChanged.AddListener(OnNameChanged);
        sellingPriceField.onValueChanged.AddListener(OnPriceChanged);
        uploadButton.onClick.AddListener(OnUploadClicked);
        createButton.onClick.AddListener(OnCreateClicked);
    }

    private void OnNameChanged(string value)
    {
        hasValidName = !string.IsNullOrWhiteSpace(value);
        errorText.gameObject.SetActive(false);
        RefreshCreateButton();
    }

    private void OnPriceChanged(string value)
    {
        errorText.gameObject.SetActive(false);

        // ✅ Check if value is a valid decimal number
        if (string.IsNullOrWhiteSpace(value))
        {
            hasValidPrice = false;
            sellingPrice = 0f;
            costDisplay.text = "Cost: ₱ 0.00";
            RefreshCreateButton();
            return;
        }

        if (!float.TryParse(value, out sellingPrice))
        {
            // ✅ Error — non-numeric input
            hasValidPrice = false;
            errorText.text = "Selling price must be a number.";
            errorText.gameObject.SetActive(true);
            costDisplay.text = "Cost: ₱ 0.00";
            RefreshCreateButton();
            return;
        }

        if (sellingPrice <= 0f)
        {
            hasValidPrice = false;
            errorText.text = "Selling price must be greater than 0.";
            errorText.gameObject.SetActive(true);
            costDisplay.text = "Cost: ₱ 0.00";
            RefreshCreateButton();
            return;
        }

        // ✅ Valid price — update cost display live
        hasValidPrice = true;
        float cost = sellingPrice * 0.5f;
        costDisplay.text = $"Cost: ₱ {cost:N2}";
        RefreshCreateButton();
    }

    private void OnUploadClicked()
    {
#if UNITY_ANDROID
        // ✅ Remove the permission variable assignment
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
                spritePreview.preserveAspect = true;
                hasSprite = true;
                RefreshCreateButton();
            },
            "Select Product Image"
        );
#endif
    }

    private void RefreshCreateButton()
    {
        if (!hasValidName || !hasValidPrice || !hasSprite)
        {
            createButton.interactable = false;
            return;
        }

        float cost = sellingPrice * 0.75f;

        // ✅ Disable if can't afford
        createButton.interactable = PlayerManager.Instance.CurrentMoney >= cost;

        // ✅ Show error if can't afford
        if (PlayerManager.Instance.CurrentMoney < cost)
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
        if (!hasValidName || !hasValidPrice || !hasSprite) return;

        bool success = StockManager.Instance.AddCustomProduct(
            nameField.text.Trim(),
            uploadedSprite,
            sellingPrice
        );

        if (success)
        {
            // ✅ Add new row to StocksPanel
            Product newProduct = StockManager.Instance.Products[^1];
            stocksPanel.SpawnRow(newProduct);

            // ✅ Reset form
            ResetForm();
        }
        else
        {
            errorText.text = "Not enough money to create this product.";
            errorText.gameObject.SetActive(true);
        }
    }

    private void ResetForm()
    {
        nameField.text = "";
        sellingPriceField.text = "";
        spritePreview.sprite = defaultPreviewSprite;
        uploadedSprite = null;
        costDisplay.text = "Cost: ₱ 0.00";
        errorText.gameObject.SetActive(false);
        hasValidName = false;
        hasValidPrice = false;
        hasSprite = false;
        sellingPrice = 0f;
        createButton.interactable = false;
    }
}