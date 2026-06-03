using UnityEngine;
using TMPro;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Money Settings")]
    [SerializeField] private float startingMoney = 200f; // Tutorial starting fund for buying first stocks

    private float _currentMoney;
    private Coroutine hideChangeCoroutine;

    // UI references — reconnected per scene via SceneUIConnector
    private TextMeshProUGUI moneyText;
    private TextMeshProUGUI moneyChangeText;
    private GameObject moneyChangeObject;
    private float cumulativeChange = 0f;
    private Coroutine hideCumulativeCoroutine;
    private const float CUMULATIVE_HIDE_DELAY = 5f;
    public float CurrentMoney => _currentMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_currentMoney == 0f)
            _currentMoney = startingMoney;
    }

    // ──────────────────────────────────────────
    // Called by SceneUIConnector on every scene load
    // ──────────────────────────────────────────
    public void SetMoneyText(TextMeshProUGUI tmp)
    {
        moneyText = tmp;
        UpdateMoneyDisplay();
    }


    // ──────────────────────────────────────────
    // Money operations
    // ──────────────────────────────────────────
    public void SetMoney(float amount)
    {
        _currentMoney = Mathf.Max(0f, amount);
        UpdateMoneyDisplay();
    }

    public void AddMoney(float amount)
    {
        _currentMoney = Mathf.Max(0f, _currentMoney + amount);
        UpdateMoneyDisplay();
        AccumulateChange(amount);
    }

    public void SubtractMoney(float amount)
    {
        _currentMoney = Mathf.Max(0f, _currentMoney - amount);
        UpdateMoneyDisplay();
        AccumulateChange(-amount);
    }
    private void AccumulateChange(float change)
    {
        if (moneyChangeObject == null || moneyChangeText == null) return;
        if (Mathf.Approximately(change, 0f)) return;

        // ✅ Accumulate change
        cumulativeChange += change;

        // ✅ Update display with cumulative total
        if (cumulativeChange > 0f)
        {
            moneyChangeText.text = $"+ ₱ {cumulativeChange:N2}";
            moneyChangeText.color = Color.green;
        }
        else
        {
            moneyChangeText.text = $"- ₱ {Mathf.Abs(cumulativeChange):N2}";
            moneyChangeText.color = Color.red;
        }

        moneyChangeObject.SetActive(true);

        // ✅ Reset hide timer on every change
        if (hideCumulativeCoroutine != null)
            StopCoroutine(hideCumulativeCoroutine);

        hideCumulativeCoroutine = StartCoroutine(HideCumulativeAfterDelay());
    }
    private IEnumerator HideCumulativeAfterDelay()
    {
        yield return new WaitForSeconds(CUMULATIVE_HIDE_DELAY); // ✅ 5 seconds

        // ✅ Hide and reset cumulative
        if (moneyChangeObject != null)
            moneyChangeObject.SetActive(false);

        cumulativeChange = 0f;
        hideCumulativeCoroutine = null;
    }
    // ──────────────────────────────────────────
    // UI
    // ──────────────────────────────────────────
    private void UpdateMoneyDisplay()
    {
        if (moneyText != null)
            moneyText.text = $"₱ {_currentMoney:N2}";
    }

    public void SetMoneyChangeUI(TextMeshProUGUI changeTmp, GameObject changeObject)
    {
        moneyChangeText = changeTmp;
        moneyChangeObject = changeObject;

        // ✅ Reset cumulative on scene load
        cumulativeChange = 0f;

        if (moneyChangeObject != null)
            moneyChangeObject.SetActive(false);
    }


}