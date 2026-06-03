using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameButtons : MonoBehaviour
{
    [SerializeField] GameObject denominationPanel;
    [SerializeField] GameObject productPanel;


    public GameObject mainPanel;

    public Image buttonImage;

    public Sprite showNormal;
    public Sprite showPressed;

    public Sprite hideNormal;
    public Sprite hidePressed;
    public Button toggleButton;
    private bool isOpen = true;

    public void TogglePanel()
    {
        isOpen = !isOpen;

        mainPanel.SetActive(isOpen);

        SpriteState state = toggleButton.spriteState;

        if (!isOpen)
        {
            buttonImage.sprite = hideNormal;
            state.pressedSprite = hidePressed;
        }
        else
        {
            buttonImage.sprite = showNormal;
            state.pressedSprite = showPressed;
        }

        toggleButton.spriteState = state;
    }
    private void Start()
    {
        Time.timeScale = 1;

        SpriteState state = toggleButton.spriteState;
        state.pressedSprite = showPressed;
        toggleButton.spriteState = state;
        mainPanel.SetActive(isOpen);
        denominationPanel.SetActive(false);
        productPanel.SetActive(false);
    }

    public void OnProductButtonPressed()
    {
        productPanel.SetActive(true);
    }
    public void OnCloseProductPanelPressed()
    {
        productPanel.SetActive(false);
    }
    public void OnDenominationButtonPressed()
    {
        denominationPanel.SetActive(true);
    }
    public void OnCloseDenominationPanelPressed()
    {
        denominationPanel.SetActive(false);
    }

    public void OnMainMenuPressed()
    {
        SceneManager.LoadScene("START");
    }
}
