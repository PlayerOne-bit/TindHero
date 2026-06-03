using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeletePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject deletePanel;      // ← the popup panel
    [SerializeField] private Button deleteButton;         // ← button OUTSIDE panel
    [SerializeField] private Button confirmButton;        // ← button INSIDE panel
    [SerializeField] private Button cancelButton;         // ← button INSIDE panel
    [SerializeField] private TMP_InputField confirmInput; // ← input INSIDE panel

    private const string ConfirmText = "CONFIRM";

    private void Start()
    {
        // ✅ Hide panel on start
        deletePanel.SetActive(false);
        confirmButton.interactable = false;

        deleteButton.onClick.AddListener(OpenDeletePanel);
        cancelButton.onClick.AddListener(CloseDeletePanel);
        confirmButton.onClick.AddListener(ConfirmDelete);
        confirmInput.onValueChanged.AddListener(OnInputChanged);
    }

    private void OpenDeletePanel()
    {
        confirmInput.text = "";              // ✅ clear input on open
        confirmButton.interactable = false;  // ✅ reset button on open
        deletePanel.SetActive(true);
    }

    private void CloseDeletePanel()
    {
        confirmInput.text = "";
        deletePanel.SetActive(false);
    }

    private void OnInputChanged(string input)
    {
        confirmButton.interactable = input == ConfirmText;
    }

    private void ConfirmDelete()
    {
        ClearSave();
        deletePanel.SetActive(false);
    }

    public static void ClearSave()
    {
        string path = Path.Combine(Application.persistentDataPath, "tindhero.dat");
        if (File.Exists(path))
            File.Delete(path);

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SetMoney(200f);

        if (StockManager.Instance != null)
            StockManager.Instance.ResetToDefaults();

        SceneManager.LoadScene("TUTORIAL");
    }
}