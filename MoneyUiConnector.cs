using UnityEngine;
using TMPro;

public class MoneyUIConnector : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI moneyChangeText;
    [SerializeField] private GameObject moneyChangePanel;

    private void Start()
    {
        PlayerManager.Instance.SetMoneyText(moneyText);
        PlayerManager.Instance.SetMoneyChangeUI(moneyChangeText, moneyChangePanel);
    }
}