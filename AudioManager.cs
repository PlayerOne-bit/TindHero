using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private const string MUSIC_VOLUME = "MusicVolume";
    private const string SFX_VOLUME = "SFXVolume";

    private const string MUSIC_MUTE_PREF = "MusicMutePref";
    private const string SFX_MUTE_PREF = "SFXMutePref";

    private bool musicMuted;
    private bool sfxMuted;

    // ==========================================================
    // INIT
    // ==========================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplySettings();
    }

    // ==========================================================
    // AUDIO PLAYBACK
    // ==========================================================

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip);
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    // ==========================================================
    // MUTE CONTROLS ONLY
    // ==========================================================

    public void ToggleMusicMute()
    {
        musicMuted = !musicMuted;
        PlayerPrefs.SetInt(MUSIC_MUTE_PREF, musicMuted ? 1 : 0);
        ApplySettings();
    }

    public void ToggleSFXMute()
    {
        sfxMuted = !sfxMuted;
        PlayerPrefs.SetInt(SFX_MUTE_PREF, sfxMuted ? 1 : 0);
        ApplySettings();
    }

    // ==========================================================
    // APPLY TO MIXER
    // ==========================================================

    private void ApplySettings()
    {
        SetMixerMute(MUSIC_VOLUME, musicMuted);
        SetMixerMute(SFX_VOLUME, sfxMuted);
    }

    private void SetMixerMute(string parameter, bool muted)
    {
        mixer.SetFloat(parameter, muted ? -80f : 0f);
    }

    // ==========================================================
    // LOAD SETTINGS
    // ==========================================================

    private void LoadSettings()
    {
        musicMuted = PlayerPrefs.GetInt(MUSIC_MUTE_PREF, 0) == 1;
        sfxMuted = PlayerPrefs.GetInt(SFX_MUTE_PREF, 0) == 1;
    }

    // ==========================================================
    // GETTERS
    // ==========================================================

    public bool IsMusicMuted() => musicMuted;
    public bool IsSFXMuted() => sfxMuted;

    public bool IsMusicPlaying()
    {
        return musicSource != null && musicSource.clip != null;
    }
}