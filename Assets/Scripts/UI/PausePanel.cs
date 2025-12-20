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
        GameManager.Instance.Resume();
    }
    
    // 새로시작 (확인 팝업)
    public void OnClickRestart()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
        BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
        popup.Setup(
            "정말 새로 시작하시겠습니까?\n현재 진행 상황이 사라집니다.",
            () => {
                GameManager.Instance.RestartLevel();
            },
            null 
        );
    }

    public void OnClickMainHome()
    {
        GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseConfirmationPopup");
        BaseConfirmationPopup popup = popupObj.GetComponent<BaseConfirmationPopup>();
        popup.Setup(
            "메인 홈으로 이동하시겠습니까?\n현재 진행 상황이 저장됩니다.",
            () => {
                GameManager.Instance.GoToMainHome();
            },
            null
        );
    }
}