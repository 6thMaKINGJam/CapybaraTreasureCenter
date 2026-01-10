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
    
    // ✅ 레이어용 AudioSource (크로스페이드용 2개)
    AudioSource bgmLayerSource1;
    AudioSource bgmLayerSource2;
    AudioSource currentBGMLayer;
    AudioSource nextBGMLayer;
    
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
        
        // ✅ 레이어용 AudioSource 2개 생성
        bgmLayerSource1 = gameObject.AddComponent<AudioSource>();
        bgmLayerSource1.loop = true;
        bgmLayerSource1.playOnAwake = false;
        
        bgmLayerSource2 = gameObject.AddComponent<AudioSource>();
        bgmLayerSource2.loop = true;
        bgmLayerSource2.playOnAwake = false;
        
        currentBGMLayer = bgmLayerSource1;
        nextBGMLayer = bgmLayerSource2;
        
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
        // Game Scene이 아니면 기존 방식
        if(scene.name != "Game")
        {
            PlayBGMForScene(scene.name);
        }
        // Game Scene은 GameManager가 직접 PlayBGMWithLayer 호출
    }
    
    // ==================== BGM (기존 방식) ====================
    
    public void PlayBGMForScene(string sceneName) {
        if (soundData == null) {
            Debug.LogError("[SoundManager] SoundData is null!");
            return;
        }
        
        AudioClip newClip = soundData.GetBGMClip(sceneName);
        if (newClip == null) return;
        
        if (currentBGMSource.clip == newClip && currentBGMSource.isPlaying) {
            return;
        }
        
        PlayBGMWithFade(newClip);
    }
    
    void PlayBGMWithFade(AudioClip newClip) {
        if (currentBGMSource.isPlaying) {
            currentBGMSource.DOFade(0f, bgmFadeDuration).OnComplete(() => {
                currentBGMSource.Stop();
                currentBGMSource.clip = null;
            });
        }
        
        AudioSource temp = currentBGMSource;
        currentBGMSource = nextBGMSource;
        nextBGMSource = temp;
        
        currentBGMSource.clip = newClip;
        currentBGMSource.volume = 0f;
        currentBGMSource.Play();
        currentBGMSource.DOFade(BGMVolume, bgmFadeDuration);
    }
    
    // ✅ BGM + 레이어 동시 재생 (Game Scene 전용)
    public void PlayBGMWithLayer(string sceneName, AudioClip layerClip)
    {
        if (soundData == null) {
            Debug.LogError("[SoundManager] SoundData is null!");
            return;
        }
        
        AudioClip baseBGM = soundData.GetBGMClip(sceneName);
        if(baseBGM == null) return;
        
        // 기존 BGM 페이드아웃
        if(currentBGMSource.isPlaying)
        {
            currentBGMSource.DOFade(0f, bgmFadeDuration).OnComplete(() => {
                currentBGMSource.Stop();
                currentBGMSource.clip = null;
            });
        }
        
        // 기존 레이어 페이드아웃
        if(currentBGMLayer.isPlaying)
        {
            currentBGMLayer.DOFade(0f, bgmFadeDuration).OnComplete(() => {
                currentBGMLayer.Stop();
                currentBGMLayer.clip = null;
            });
        }
        
        // 소스 스왑
        AudioSource tempBase = currentBGMSource;
        currentBGMSource = nextBGMSource;
        nextBGMSource = tempBase;
        
        AudioSource tempLayer = currentBGMLayer;
        currentBGMLayer = nextBGMLayer;
        nextBGMLayer = tempLayer;
        
        // Base BGM 페이드인
        currentBGMSource.clip = baseBGM;
        currentBGMSource.volume = 0f;
        currentBGMSource.Play();
        currentBGMSource.DOFade(BGMVolume, bgmFadeDuration);
        
        // 레이어 페이드인
        if(layerClip != null)
        {
            currentBGMLayer.clip = layerClip;
            currentBGMLayer.volume = 0f;
            currentBGMLayer.Play();
            currentBGMLayer.DOFade(BGMVolume, bgmFadeDuration);
            
            Debug.Log($"[SoundManager] BGM + Layer 재생: {sceneName} + {layerClip.name}");
        }
        else
        {
            Debug.Log($"[SoundManager] BGM만 재생: {sceneName}");
        }
    }
    
    void UpdateBGMVolume() {
        if (currentBGMSource != null) {
            currentBGMSource.volume = BGMVolume;
        }
        if (nextBGMSource != null && nextBGMSource.isPlaying) {
            nextBGMSource.volume = BGMVolume;
        }
        
        // ✅ 레이어 볼륨도 동기화
        if (currentBGMLayer != null) {
            currentBGMLayer.volume = BGMVolume;
        }
        if (nextBGMLayer != null && nextBGMLayer.isPlaying) {
            nextBGMLayer.volume = BGMVolume;
        }
    }
    
    // ==================== BGM 제어 메서드 ====================
    
    public void PauseBGM() {
        if (currentBGMSource != null && currentBGMSource.isPlaying) {
            currentBGMSource.Pause();
        }
        if (currentBGMLayer != null && currentBGMLayer.isPlaying) {
            currentBGMLayer.Pause();
        }
        Debug.Log("[SoundManager] BGM 일시정지");
    }
    
    public void ResumeBGM() {
        if (currentBGMSource != null && !currentBGMSource.isPlaying) {
            currentBGMSource.UnPause();
        }
        if (currentBGMLayer != null && !currentBGMLayer.isPlaying) {
            currentBGMLayer.UnPause();
        }
        Debug.Log("[SoundManager] BGM 재개");
    }
    
    public void StopBGM() {
        if (currentBGMSource != null) {
            currentBGMSource.Stop();
            currentBGMSource.time = 0f;
        }
        
        if (nextBGMSource != null && nextBGMSource.isPlaying) {
            nextBGMSource.Stop();
            nextBGMSource.time = 0f;
        }
        
        if (currentBGMLayer != null) {
            currentBGMLayer.Stop();
            currentBGMLayer.time = 0f;
        }
        
        if (nextBGMLayer != null && nextBGMLayer.isPlaying) {
            nextBGMLayer.Stop();
            nextBGMLayer.time = 0f;
        }
        
        Debug.Log("[SoundManager] BGM 정지");
    }
    
    // ✅ 모든 BGM 즉시 정리 (씬 전환 시)
    public void StopAllBGM()
    {
        currentBGMSource.DOKill();
        currentBGMSource.Stop();
        currentBGMSource.clip = null;
        
        nextBGMSource.DOKill();
        nextBGMSource.Stop();
        nextBGMSource.clip = null;
        
        currentBGMLayer.DOKill();
        currentBGMLayer.Stop();
        currentBGMLayer.clip = null;
        
        nextBGMLayer.DOKill();
        nextBGMLayer.Stop();
        nextBGMLayer.clip = null;
        
        Debug.Log("[SoundManager] 모든 BGM 정리 완료");
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
        foreach (var source in fxSourcePool) {
            if (!source.isPlaying) {
                return source;
            }
        }
        
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.loop = false;
        fxSourcePool.Add(newSource);
        
        Debug.Log($"[SoundManager] FX pool expanded to {fxSourcePool.Count}");
        return newSource;
    }
    
    private void OnDestroy()
    {
        StopAllBGM();
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
        if (bgmLayerSource1 != null) bgmLayerSource1.Stop();
        if (bgmLayerSource2 != null) bgmLayerSource2.Stop();
        
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