using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DenominationSelectorPanel : MonoBehaviour
{
    [System.Serializable]
    public class DenominationRow
    {
        public Button subtractButton;
        public TextMeshProUGUI quantityText;
        public Button addButton;
        public Sprite denominationSprite;
    }
    [Header("Panel")]
    [SerializeField] private GameObject denominationSelectorPanelObject;

    [Header("References")]
    [SerializeField] private TransactionPanel transactionPanel;

    [Header("Denomination Rows")]
    [SerializeField] private DenominationRow[] rows;

    [Header("Confirm")]
    [SerializeField] private Button denominationConfirmButton;

    private int[] quantities;
    private readonly float[] denominationValues = Denomination.Values;

    private void Start()
    {
        quantities = new int[denominationValues.Length];

        for (int i = 0; i < rows.Length; i++)
        {
            int index = i;

            rows[index].quantityText.text = "0";
            rows[index].subtractButton.interactable = false;

            rows[index].subtractButton.onClick.AddListener(() => OnSubtract(index));
            rows[index].addButton.onClick.AddListener(() => OnAdd(index));
        }
        denominationConfirmButton.interactable = false;
        RefreshAllAddButtons(); // ✅ correct state on open
    }


    private void OnAdd(int index)
    {
        quantities[index]++;
        RefreshRow(index);
    }

    private void OnSubtract(int index)
    {
        if (quantities[index] <= 0) return;
        quantities[index]--;
        RefreshRow(index);
    }

    private void RefreshRow(int index)
    {
        int qty = quantities[index];

        rows[index].quantityText.text = qty.ToString();
        rows[index].subtractButton.interactable = qty > 0;

        // Refresh ALL add buttons since remaining money changes on every click
        RefreshAllAddButtons();

        // Recalculate grand total across ALL denomination rows
        float grandTotal = 0f;
        for (int i = 0; i < quantities.Length; i++)
            grandTotal += quantities[i] * denominationValues[i];

    }

    public void RefreshAllAddButtons()
    {
        float allocated = 0f;
        for (int i = 0; i < quantities.Length; i++)
            allocated += quantities[i] * denominationValues[i];

        // ✅ Only limit by what the player physically has
        float remaining = PlayerManager.Instance.CurrentMoney - allocated;

        for (int i = 0; i < rows.Length; i++)
            rows[i].addButton.interactable = denominationValues[i] <= remaining;

        denominationConfirmButton.interactable = allocated > 0f;
    }
    public void ResetPanel()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            quantities[i] = 0;
            rows[i].quantityText.text = "0";        // ← could be null
            rows[i].subtractButton.interactable = false; // ← could be null
        }

        denominationConfirmButton.interactable = false;
        RefreshAllAddButtons();
    }

    /// <summary>
    /// Collects all Denomination entries where quantity > 0 and passes them forward.
    /// </summary>
    public void OnDenominationConfirm()
    {
        List<(Denomination denomination, int quantity)> selected =
            new List<(Denomination, int)>();

        float grandTotal = 0f;

        for (int i = 0; i < quantities.Length; i++)
        {
            if (quantities[i] > 0)
            {
                Denomination denom = new Denomination(denominationValues[i], rows[i].denominationSprite);
                selected.Add((denom, quantities[i]));
                grandTotal += quantities[i] * denominationValues[i];
            }
        }

        transactionPanel.ReceiveDenominations(selected, grandTotal);
        // Reset all quantities and UI back to default
        ResetPanel();

        // Close the panel
        denominationSelectorPanelObject.SetActive(false);
    }
}