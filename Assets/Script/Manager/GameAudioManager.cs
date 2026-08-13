using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameAudioManager : MonoBehaviour
{
    private const string SettingsPath = "GameAudioSettings";
    private const string BgmVolumeKey = "Audio.BgmVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";

    private static GameAudioManager instance;

    private GameAudioSettings settings;
    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource mistakeSource;

    public static float BgmVolume =>
        PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
    public static float SfxVolume =>
        PlayerPrefs.GetFloat(SfxVolumeKey, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static GameAudioManager EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject root = new GameObject(nameof(GameAudioManager));
        instance = root.AddComponent<GameAudioManager>();
        DontDestroyOnLoad(root);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        settings = Resources.Load<GameAudioSettings>(SettingsPath);
        bgmSource = CreateSource("BGM", true);
        sfxSource = CreateSource("SFX", false);
        mistakeSource = CreateSource("MistakeSFX", false);
        ApplyVolumes();
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        instance = null;
    }

    public static void SetDay(int day)
    {
        GameAudioManager manager = EnsureInstance();
        AudioClip clip = day >= 6 && day != 10
            ? manager.settings?.daySixToNineBgm
            : manager.settings?.dayOneToFiveBgm;
        manager.PlayBgm(clip);
    }

    public static void StopBgm()
    {
        GameAudioManager manager = EnsureInstance();
        manager.bgmSource.Stop();
        manager.bgmSource.clip = null;
    }

    public static void PlayDayStart() =>
        PlaySfx(EnsureInstance().settings?.dayStart);
    public static void PlayUiButton() =>
        PlaySfx(EnsureInstance().settings?.uiButtonClick);
    public static void PlayMouseClick() =>
        PlaySfx(EnsureInstance().settings?.mouseClick);
    public static void PlayMistake()
    {
        GameAudioManager manager = EnsureInstance();
        AudioClip clip = manager.settings?.miniGameMistake;

        if (clip == null)
            return;

        manager.mistakeSource.Stop();
        manager.mistakeSource.clip = clip;
        manager.mistakeSource.time = Mathf.Min(
            manager.settings.miniGameMistakeStartOffset,
            Mathf.Max(0f, clip.length - 0.01f));
        manager.mistakeSource.Play();
    }
    public static void PlayTyping() =>
        PlaySfx(EnsureInstance().settings?.typing);
    public static void PlaySortingCorrect() =>
        PlaySfx(EnsureInstance().settings?.sortingCorrect);

    public static void PlayFailure()
    {
        GameAudioManager manager = EnsureInstance();
        manager.mistakeSource.Stop();
        manager.sfxSource.Stop();
        manager.PlaySfxInternal(manager.settings?.miniGameFailure);
    }

    public static void SetBgmVolume(float volume)
    {
        PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        EnsureInstance().ApplyVolumes();
    }

    public static void SetSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        EnsureInstance().ApplyVolumes();
    }

    private static void PlaySfx(AudioClip clip)
    {
        EnsureInstance().PlaySfxInternal(clip);
    }

    private void PlaySfxInternal(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    private void PlayBgm(AudioClip clip)
    {
        if (clip == null || bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        return source;
    }

    private void ApplyVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = BgmVolume;
        if (sfxSource != null)
            sfxSource.volume = SfxVolume;
        if (mistakeSource != null)
            mistakeSource.volume = SfxVolume;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "TitleScene")
            StopBgm();
    }
}
