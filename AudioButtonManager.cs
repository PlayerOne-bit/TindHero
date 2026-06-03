using UnityEngine;
using UnityEngine.UI;

public class AudioButtonManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private Button[] buttons;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        RegisterButtons();
    }

    private void RegisterButtons()
    {
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            btn.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (clickClip == null || audioSource == null) return;

        audioSource.PlayOneShot(clickClip);
    }
}