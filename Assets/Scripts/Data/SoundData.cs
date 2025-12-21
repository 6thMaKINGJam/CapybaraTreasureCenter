using UnityEngine;

[System.Serializable]
public class BGMClip {
    public string sceneName; // "MainHome", "Tutorial", "Game", "Ending"
    public AudioClip clip;
}

[System.Serializable]
public class FXClip {
    public SoundType type;
    public AudioClip clip;
}

[CreateAssetMenu(fileName = "SoundData", menuName = "Game/Sound Data")]
public class SoundData : ScriptableObject {
    [Header("배경음악 (씬별)")]
    public BGMClip[] bgmClips;
    
    [Header("효과음 (타입별)")]
    public FXClip[] fxClips;
    
    // BGM 클립 찾기
    public AudioClip GetBGMClip(string sceneName) {
        foreach (var bgm in bgmClips) {
            if (bgm.sceneName == sceneName) {
                return bgm.clip;
            }
        }
        Debug.LogWarning($"[SoundData] BGM not found for scene: {sceneName}");
        return null;
    }
    
    // FX 클립 찾기
    public AudioClip GetFXClip(SoundType type) {
        foreach (var fx in fxClips) {
            if (fx.type == type) {
                return fx.clip;
            }
        }
        Debug.LogWarning($"[SoundData] FX not found for type: {type}");
        return null;
    }
}