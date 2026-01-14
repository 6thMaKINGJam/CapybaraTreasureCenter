using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameOverPopup : MonoBehaviour
{
    [Header("UI 요소")]
    public TMP_Text MessageText;
    public TMP_Text LevelText;
    public Button RestartButton;
    public Button MainHomeButton;
    public Button TimeAddButton; // ✅ 추가
    
    private Action onRestartAction;
    private Action onMainHomeAction;
    private Action onTimeAddAction; // ✅ 추가
    
    public void Setup(int levelIndex, string message, Action restartCallback, Action mainHomeCallback, Action timeAddCallback = null)
    {
        if (LevelText != null) 
<<<<<<< Updated upstream
        {
            // 챌린지 모드일 때는 텍스트를 비활성화하고, 아닐 때만 레벨 표시
            LevelText.gameObject.SetActive(ChallengeModeManager.Instance == null); 
            if (ChallengeModeManager.Instance == null)
                LevelText.text = $"LEVEL {levelIndex}"; 
            }// ✅ 레벨 표시
=======
            LevelText.text = $"LV.{levelIndex}"; // ✅ 레벨 표시
>>>>>>> Stashed changes
        MessageText.text = message;
        
        onRestartAction = restartCallback;
        onMainHomeAction = mainHomeCallback;
        onTimeAddAction = timeAddCallback;
        
        RestartButton.onClick.RemoveAllListeners();
        RestartButton.onClick.AddListener(OnClickRestart);
        
        MainHomeButton.onClick.RemoveAllListeners();
        MainHomeButton.onClick.AddListener(OnClickMainHome);
        
        // ✅ 시간추가 버튼
        if(TimeAddButton != null && timeAddCallback != null)
        {
            TimeAddButton.gameObject.SetActive(true);
            TimeAddButton.onClick.RemoveAllListeners();
            TimeAddButton.onClick.AddListener(OnClickTimeAdd);
        }
        else if(TimeAddButton != null)
        {
            TimeAddButton.gameObject.SetActive(false);
        }
    }
    
    private void OnClickRestart()
    {
        onRestartAction?.Invoke();
        Debug.Log("GameOverPopup: Restart button clicked.");
        Destroy(gameObject);
    }
    
    private void OnClickMainHome()
    {
        onMainHomeAction?.Invoke();
        Destroy(gameObject);
    }
    
    private void OnClickTimeAdd()
    {
        onTimeAddAction?.Invoke();
        Destroy(gameObject); // ✅ 팝업 닫기 (게임 재개)
    }
}