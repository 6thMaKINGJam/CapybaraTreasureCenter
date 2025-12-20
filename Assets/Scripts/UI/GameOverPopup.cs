using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GameOverPopup : MonoBehaviour
{
    [Header("UI 요소")]
    public TMP_Text MessageText;
    public Button RestartButton;   // 다시하기
    public Button UndoButton;      // 되돌리기
    public Button MainHomeButton;  // 메인으로
    
    private Action onRestartAction;
    private Action onUndoAction;
    private Action onMainHomeAction;
    
    public void Setup(string message, Action restartCallback, Action undoCallback, Action mainHomeCallback)
    {
        MessageText.text = message;
        
        onRestartAction = restartCallback;
        onUndoAction = undoCallback;
        onMainHomeAction = mainHomeCallback;
        
        RestartButton.onClick.RemoveAllListeners();
        RestartButton.onClick.AddListener(OnClickRestart);
        
        UndoButton.onClick.RemoveAllListeners();
        UndoButton.onClick.AddListener(OnClickUndo);
        
        MainHomeButton.onClick.RemoveAllListeners();
        MainHomeButton.onClick.AddListener(OnClickMainHome);
    }
    
    private void OnClickRestart()
    {
        onRestartAction?.Invoke();
        Destroy(gameObject);
    }
    
    private void OnClickUndo()
    {
        onUndoAction?.Invoke();
        Destroy(gameObject);
    }
    
    private void OnClickMainHome()
    {
        onMainHomeAction?.Invoke();
        Destroy(gameObject);
    }
}