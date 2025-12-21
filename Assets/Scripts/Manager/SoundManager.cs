using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using DG.Tweening;

public class SoundManager : MonoBehaviour {
    public static SoundManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] SoundData soundData;
    
    [Header("Settings")]
    [SerializeField] int fxSourcePoolSize = 5;
    [SerializeField] float bgmFadeDuration = 1.5f;
    
    // BGM용 AudioSource (크로스페이드용 2개)
    AudioSource bgmSource1;
    AudioSource bgmSource2;
    AudioSource currentBGMSource;
    AudioSource nextBGMSource;
    
    // FX용 AudioSource 풀
    List<AudioSource> fxSourcePool = new List<AudioSource>();
    
    // 볼륨 설정
    const string BGM_VOLUME_KEY = "BGMVolume";
    const string FX_VOLUME_KEY = "FXVolume";
    
    public float BGMVolume {
        get => PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
        set {
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            UpdateBGMVolume();
        }
    }
    
    public float FXVolume {
        get => PlayerPrefs.GetFloat(FX_VOLUME_KEY, 1f);
        set {
            PlayerPrefs.SetFloat(FX_VOLUME_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }
    
    void Awake() {
        // 싱글톤 설정
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeAudioSources();
    }
    
    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void InitializeAudioSources() {
        // BGM용 AudioSource 2개 생성
        bgmSource1 = gameObject.AddComponent<AudioSource>();
        bgmSource1.loop = true;
        bgmSource1.playOnAwake = false;
        
        bgmSource2 = gameObject.AddComponent<AudioSource>();
        bgmSource2.loop = true;
        bgmSource2.playOnAwake = false;
        
        currentBGMSource = bgmSource1;
        nextBGMSource = bgmSource2;
        
        // FX용 AudioSource 풀 생성
        for (int i = 0; i < fxSourcePoolSize; i++) {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            fxSourcePool.Add(source);
        }
        
        UpdateBGMVolume();
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        PlayBGMForScene(scene.name);
    }
    
    // ==================== BGM ====================
    
    public void PlayBGMForScene(string sceneName) {
        if (soundData == null) {
            Debug.LogError("[SoundManager] SoundData is null!");
            return;
        }
        
        AudioClip newClip = soundData.GetBGMClip(sceneName);
        if (newClip == null) return;
        
        // 이미 같은 BGM이 재생 중이면 무시
        if (currentBGMSource.clip == newClip && currentBGMSource.isPlaying) {
            return;
        }
        
        PlayBGMWithFade(newClip);
    }
    
    void PlayBGMWithFade(AudioClip newClip) {
        // 현재 재생 중인 BGM 페이드아웃
        if (currentBGMSource.isPlaying) {
            currentBGMSource.DOFade(0f, bgmFadeDuration).OnComplete(() => {
                currentBGMSource.Stop();
                currentBGMSource.clip = null;
            });
        }
        
        // 소스 스왑
        AudioSource temp = currentBGMSource;
        currentBGMSource = nextBGMSource;
        nextBGMSource = temp;
        
        // 새 BGM 페이드인
        currentBGMSource.clip = newClip;
        currentBGMSource.volume = 0f;
        currentBGMSource.Play();
        currentBGMSource.DOFade(BGMVolume, bgmFadeDuration);
    }
    
    void UpdateBGMVolume() {
        if (currentBGMSource != null) {
            currentBGMSource.volume = BGMVolume;
        }
        if (nextBGMSource != null && nextBGMSource.isPlaying) {
            nextBGMSource.volume = BGMVolume;
        }
    }
    
    // ==================== FX ====================
    
    public void PlayFX(SoundType type) {
        if (soundData == null) {
            Debug.LogError("[SoundManager] SoundData is null!");
            return;
        }
        
        AudioClip clip = soundData.GetFXClip(type);
        if (clip == null) return;
        
        AudioSource source = GetAvailableFXSource();
        source.PlayOneShot(clip, FXVolume);
    }
    
    AudioSource GetAvailableFXSource() {
        // 재생 중이지 않은 AudioSource 찾기
        foreach (var source in fxSourcePool) {
            if (!source.isPlaying) {
                return source;
            }
        }
        
        // 모두 사용 중이면 새로 생성 (동적 확장)
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.loop = false;
        fxSourcePool.Add(newSource);
        
        Debug.Log($"[SoundManager] FX pool expanded to {fxSourcePool.Count}");
        return newSource;
    }
    
    // ==================== 에디터 테스트용 ====================
    
#if UNITY_EDITOR
    public void EditorPlayBGM(string sceneName) {
        if (!Application.isPlaying) {
            EnsureEditorAudioSources();
        }
        PlayBGMForScene(sceneName);
    }
    
    public void EditorPlayFX(SoundType type) {
        if (!Application.isPlaying) {
            EnsureEditorAudioSources();
        }
        PlayFX(type);
    }
    
    public void EditorStopAll() {
        if (bgmSource1 != null) bgmSource1.Stop();
        if (bgmSource2 != null) bgmSource2.Stop();
        
        foreach (var source in fxSourcePool) {
            if (source != null) source.Stop();
        }
    }
    
    void EnsureEditorAudioSources() {
        if (bgmSource1 == null || bgmSource2 == null || fxSourcePool.Count == 0) {
            InitializeAudioSources();
        }
    }
#endif
}