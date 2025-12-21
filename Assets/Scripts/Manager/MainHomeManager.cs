using UnityEngine;
using UnityEngine.SceneManagement;
using Scripts.UI;

public class MainHomeManager : MonoBehaviour
{
    [Header("UI Controllers")]
    [SerializeField] private MainHomePanel mainHomeUI;
    [SerializeField] private LevelSelectPanel levelSelectUI;
    [SerializeField] private HowToPlayPanel howToPlayUI;
    [SerializeField] private HallOfFamePanel hallOfFameUI;

    [SerializeField] private CanvasGroup mainCanvasGroup;

    private ProgressData currentProgress;
    private const string SaveKey = "ProgressData";

    void Start()
    {
        // 1. 데이터 로드
        currentProgress = SaveManager.LoadData<ProgressData>(SaveKey);
        
        // 2. UI 버튼들에 기능 연결
        SetupButtons();

        // 3. 초기 화면 설정
        mainHomeUI.ShowMain();

        // ===== 엔딩 완료 후 랭킹 자동 열기 =====
        if(PlayerPrefs.GetInt("ShowRankingOnStart", 0) == 1)
        {
            PlayerPrefs.SetInt("ShowRankingOnStart", 0); // 초기화
            OnClickHallOfFame(); // 명예의 전당 자동 열기
        }
        else
        {
            // 4. 레벨 4 클리어 시 엔딩 표시
            CheckAndRunEndingSequence();
        }
    }

    private void SetupButtons()
    {
        mainHomeUI.startButton.onClick.AddListener(OnClickGameStart);
        mainHomeUI.howToPlayButton.onClick.AddListener(OnClickHowToPlay);
        mainHomeUI.hallOfFameButton.onClick.AddListener(OnClickHallOfFame);

        levelSelectUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
        howToPlayUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
        hallOfFameUI.closeButton.onClick.AddListener(() =>
        {
            hallOfFameUI.gameObject.SetActive(false);
            mainHomeUI.ShowMain();
        });
    }

    private void CheckAndRunEndingSequence()
    {
        // 레벨 4 클리어했지만 엔딩 미시청
        if(currentProgress.LastClearedLevel >= 4 && !currentProgress.EndingCompleted)
        {
            if(NetworkManager.Instance != null && NetworkManager.Instance.IsNetworkAvailable())
            {
                if(mainCanvasGroup != null) mainCanvasGroup.interactable = false;
                
                Debug.Log("엔딩 시퀀스로 진입합니다.");
                SceneManager.LoadScene("EndingScene"); 
            }
            else
            {
                Debug.LogWarning("네트워크 연결 후 재접속 시 엔딩을 감상할 수 있습니다.");
            }
        }
    }

    public void OnClickGameStart()
    {
        mainHomeUI.OpenPanel(mainHomeUI.LevelSelectPanel);
        levelSelectUI.RefreshLevelNodes(currentProgress.LastClearedLevel);
    }

    public void OnClickHowToPlay()
    {
        mainHomeUI.OpenPanel(mainHomeUI.HowToPlayPanel);
        howToPlayUI.Init();
    }

    public void OnClickHallOfFame()
    {
        if(NetworkManager.Instance != null && NetworkManager.Instance.IsNetworkAvailable())
        {
            mainHomeUI.OpenPanel(mainHomeUI.HallOfFamePanel);
            hallOfFameUI.Open(); // Open() 메서드 호출
        }
        else
        {
            Debug.LogWarning("명예의 전당은 온라인 상태에서만 확인 가능합니다.");
            
            GameObject popupObj = PopupParentSetHelper.Instance.CreatePopup("Prefabs/BaseWarningPopup");
            BaseWarningPopup popup = popupObj.GetComponent<BaseWarningPopup>();
            popup.Setup("네트워크 연결이 필요합니다카피!", null);
        }
    }
}