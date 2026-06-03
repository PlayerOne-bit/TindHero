using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NpcRowPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image npcImage;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI priceRangeText;
    [SerializeField] private Button deleteButton;

    public NpcData npcData;
    private NpcPanel parentPanel;

    public void Initialize(NpcData data, NpcPanel panel)
    {
        npcData = data;
        parentPanel = panel;

        npcImage.sprite = npcData.sprite;
        npcNameText.text = npcData.npcName;
        priceRangeText.text = npcData.PriceRangeDisplay;

        // ✅ SetActive false for defaults, true for customs
        deleteButton.gameObject.SetActive(npcData.isCustom);

        if (npcData.isCustom)
            deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    private void OnDeleteClicked()
    {
        if (NpcManager.Instance.DeleteCustomNpc(npcData))
        {
            parentPanel.RefreshPanel();
            Destroy(gameObject);
        }
    }
}