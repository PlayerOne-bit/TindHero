using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameAudioSetup : MonoBehaviour
{
    [Header("Scene Music")]
    [SerializeField] private AudioClip startSceneMusic;
    [SerializeField] private AudioClip gameplaySceneMusic;
    [SerializeField] private AudioClip tutorialSceneMusic;

    [Header("Optional Button Click SFX")]
    [SerializeField] private AudioClip buttonClickSFX;

    [Header("Buttons That Should Play Click Sound")]
    [SerializeField] private Button[] buttons;

    private void Awake()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.onClick.AddListener(PlayButtonClick);
            }
        }
    }

    private void Start()
    {
        PlayMusicForCurrentScene();
    }

    private void PlayButtonClick()
    {
        if (buttonClickSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSFX);
        }
    }

    private void PlayMusicForCurrentScene()
    {
        if (AudioManager.Instance == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;
        AudioClip clipToPlay = null;

        switch (sceneName)
        {
            case "START":
                clipToPlay = startSceneMusic;
                break;

            case "GAMEPLAY":
                clipToPlay = gameplaySceneMusic;
                break;

            case "TUTORIAL":
                clipToPlay = tutorialSceneMusic;
                break;
        }

        if (clipToPlay != null)
        {
            AudioManager.Instance.PlayMusic(clipToPlay);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForCurrentScene();
    }
}