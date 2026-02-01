using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PausePanel : MonoBehaviour
{
    [Header("버튼")]
    public Button ResumeButton;    // 이어하기
    public Button RestartButton;   // 새로시작
    public Button MainHomeButton;  // 메인홈으로
    
    private void Awake()
    {
        SetupButtons();
    }
    
    private void SetupButtons()
    {
        ResumeButton.onClick.AddListener(ResumeGame);
        RestartButton.onClick.AddListener(OnClickRestart);
        MainHomeButton.onClick.AddListener(OnClickMainHome);
    }
    
    // 이어하기
    public void ResumeGame()
    {
        if (LevelModeManager.Instance != null)
        LevelModeManager.Instance.Resume();

        if (ChallengeModeManager.Instance != null)
        ChallengeModeManager.Instance.Resume();
    }
    
    // 새로시작 (확인 팝업)
    public void OnClickRestart()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
        BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
        popup.Setup(
            "정말 새로 시작하시겠습니까?",
            () => {
                RestartCurrentLevel();
            },
            null 
        );
    }

    public void RestartCurrentLevel()
    {
        Time.timeScale = 1f; // 시간 흐름 복구
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("[ChallengeModeManager] 현재 레벨 재시작");  
    }

    public void GoToMainHome()
    {
        Time.timeScale = 1f; // 시간 흐름 복구
        SceneManager.LoadScene("MainHome");
    }

    public void OnClickMainHome()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
        BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
        popup.Setup(
            "메인 홈으로 이동하시겠습니까?",
            () => {
                GoToMainHome();
            },
            null
        );
    }
}