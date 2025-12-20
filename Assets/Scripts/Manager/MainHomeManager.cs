using UnityEngine;
using Scripts.UI;
using Scripts.Managers; // NetworkManager 접근을 위해 추가
using UnityEngine.SceneManagement;

public class MainHomeManager : MonoBehaviour
{
    [Header("UI Controllers")]
    [SerializeField] private MainHomePanel mainHomeUI;
    [SerializeField] private LevelSelectPanel levelSelectUI;
    [SerializeField] private HowToPlayPanel howToPlayUI;
    [SerializeField] private HallOfFamePanel hallOfFameUI;

    private ProgressData currentProgress;
    private const string SaveKey = "ProgressData";

    void Start()
    {
        // 1. 데이터 로드
        currentProgress = SaveManager.LoadData<ProgressData>(SaveKey);
        
        // 2. UI 버튼들에 기능(함수) 연결
        SetupButtons();

        // 3. 초기 화면 설정 (패널 중복 오픈 방지 포함)
        mainHomeUI.ShowMain();

        // 4. 레벨 4 클리어 시 엔딩 표시 분기 처리
        CheckAndRunEndingSequence();
    }

    private void SetupButtons()
    {
        // 메인 패널 버튼들 연결
        mainHomeUI.startButton.onClick.AddListener(OnClickGameStart);
        mainHomeUI.howToPlayButton.onClick.AddListener(OnClickHowToPlay);
        mainHomeUI.hallOfFameButton.onClick.AddListener(OnClickHallOfFame);

        // 각 패널의 X(닫기) 버튼들 연결
        levelSelectUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
        howToPlayUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
        hallOfFameUI.closeButton.onClick.AddListener(mainHomeUI.ShowMain);
    }

    // [PHASE 7 연동] 엔딩 시퀀스 진입 로직
    private void CheckAndRunEndingSequence()
    {
        // 조건: 레벨 4를 클리어했으나 아직 엔딩 시퀀스를 보지 않은 경우
        // ProgressData의 변수명은 제공해주신 1-C 정의를 따릅니다.
        if (currentProgress.LastClearedLevel >= 4 && !currentProgress.hasSeenLevel4Ending)
        {
            // 네트워크 상태 확인
            if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected())
            {
                Debug.Log("엔딩 시퀀스로 진입합니다.");
                // 엔딩 시퀀스(7-D) 프리팹을 열거나 엔딩 씬으로 이동
                SceneManager.LoadScene("EndingScene"); 
            }
            else
            {
                // 네트워크 없으면 경고 팝업 표시 후 메인 홈 유지
                Debug.LogWarning("네트워크 연결이 없어 엔딩을 재생할 수 없습니다.");
                // TODO: BaseWarningPopup.Instance.Show("네트워크 연결이 필요합니다.");
            }
        }
    }

    public void OnClickGameStart()
    {
        // 패널 중복 오픈 방지를 위해 mainHomeUI 내부에서 SetAllPanelsInactive 호출
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
        // 명예의 전당 진입 전 네트워크 체크 (권장)
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected())
        {
            mainHomeUI.OpenPanel(mainHomeUI.HallOfFamePanel);
            // hallOfFameUI.SetupRecords(currentProgress.level4ClearTimes);
        }
        else
        {
            Debug.LogWarning("명예의 전당은 온라인 상태에서만 확인 가능합니다.");
        }
    }
}