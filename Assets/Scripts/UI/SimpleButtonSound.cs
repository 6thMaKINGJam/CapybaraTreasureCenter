using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SimpleButtonSound : MonoBehaviour {
    [SerializeField] bool useDefaultSound = true;
    
    [SerializeField] 
    [Tooltip("useDefaultSound가 false일 때 재생할 커스텀 사운드")]
    SoundType customSoundType = SoundType.ButtonClick;
    
    void Start() {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(PlaySound);
    }
    
    void PlaySound() {
        if (SoundManager.Instance == null) {
            Debug.LogWarning("[SimpleButtonSound] SoundManager not found!");
            return;
        }
        
        if (useDefaultSound) {
            SoundManager.Instance.PlayFX(SoundType.ButtonClick);
        } else {
            SoundManager.Instance.PlayFX(customSoundType);
        }
    }
}